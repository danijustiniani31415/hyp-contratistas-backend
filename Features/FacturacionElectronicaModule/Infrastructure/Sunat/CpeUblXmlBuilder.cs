using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Abril_Backend.Features.FacturacionElectronicaModule.Application.Services;
using Abril_Backend.Features.FacturacionElectronicaModule.Infrastructure.Models;
using Abril_Backend.Features.GuiasRemisionModule.Infrastructure.Sunat;
using Microsoft.Extensions.Options;

namespace Abril_Backend.Features.FacturacionElectronicaModule.Infrastructure.Sunat
{
    public interface ICpeXmlBuilder
    {
        /// <summary>XML UBL 2.1 sin firmar de una factura (01), nota de crédito (07) o nota de débito (08).</summary>
        string Build(Cpe cpe);
        /// <summary>XML de la comunicación de baja (RA) de un comprobante ya aceptado.</summary>
        string BuildBaja(Cpe cpe, string identificador, DateOnly hoy);
    }

    /// <summary>
    /// Genera el XML UBL 2.1 sin firmar. La estructura sigue la guía de elaboración de XML de SUNAT; el orden de los
    /// elementos importa porque SUNAT valida contra el XSD. [PENDIENTE VALIDAR en Beta] Es esperable que el primer
    /// envío devuelva algún error puntual de validación: se corrige con el código del CDR, no hace falta adivinar de antemano.
    /// </summary>
    public class CpeUblXmlBuilder : ICpeXmlBuilder
    {
        private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
        private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
        private static readonly XNamespace Ext = "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2";
        private static readonly XNamespace Ds = "http://www.w3.org/2000/09/xmldsig#";
        private static readonly XNamespace Sac = "urn:sunat:names:specification:ubl:peru:schema:xsd:SunatAggregateComponents-1";

        private readonly SunatCpeSettings _s;

        public CpeUblXmlBuilder(IOptions<SunatCpeSettings> settings)
        {
            _s = settings.Value;
        }

        private static string M(decimal v) => v.ToString("0.00", CultureInfo.InvariantCulture);
        private static string Q(decimal v) => v.ToString("0.##########", CultureInfo.InvariantCulture);
        private static string D(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        private static XElement Amt(XName n, string moneda, decimal v) => new(n, new XAttribute("currencyID", moneda), M(v));
        private static XElement Cdata(XName n, string texto, params XAttribute[] attrs) => new(n, attrs, new XCData(texto ?? ""));

        public string Build(Cpe c)
        {
            if (c.Numero == null) throw new InvalidOperationException("El comprobante debe tener número antes de generar el XML.");
            var tipo = c.TipoDocumento;
            var raiz = tipo switch { "01" => "Invoice", "07" => "CreditNote", "08" => "DebitNote", _ => throw new InvalidOperationException($"Tipo {tipo} no soportado.") };
            XNamespace ns = $"urn:oasis:names:specification:ubl:schema:xsd:{raiz}-2";
            var id = $"{c.Serie}-{c.Numero}";
            var mon = c.Moneda;
            var hora = c.HoraEmision ?? new TimeOnly(12, 0, 0);

            var doc = new XElement(ns + raiz,
                new XAttribute(XNamespace.Xmlns + "cac", Cac),
                new XAttribute(XNamespace.Xmlns + "cbc", Cbc),
                new XAttribute(XNamespace.Xmlns + "ds", Ds),
                new XAttribute(XNamespace.Xmlns + "ext", Ext),
                new XElement(Ext + "UBLExtensions", new XElement(Ext + "UBLExtension", new XElement(Ext + "ExtensionContent"))),
                new XElement(Cbc + "UBLVersionID", "2.1"),
                new XElement(Cbc + "CustomizationID", "2.0"),
                new XElement(Cbc + "ID", id),
                new XElement(Cbc + "IssueDate", D(c.FechaEmision)),
                new XElement(Cbc + "IssueTime", hora.ToString("HH:mm:ss", CultureInfo.InvariantCulture)));

            if (tipo == "01")
            {
                if (c.FechaVencimiento.HasValue) doc.Add(new XElement(Cbc + "DueDate", D(c.FechaVencimiento.Value)));
                doc.Add(new XElement(Cbc + "InvoiceTypeCode", new XAttribute("listID", !string.IsNullOrWhiteSpace(c.DetraccionCodigo) && c.TipoOperacion == "0101" ? "1001" : c.TipoOperacion), "01"));
            }

            doc.Add(Cdata(Cbc + "Note", NumeroALetras.Monto(c.Total, mon), new XAttribute("languageLocaleID", "1000")));
            if (!string.IsNullOrWhiteSpace(c.DetraccionCodigo))
                doc.Add(Cdata(Cbc + "Note", "Operación sujeta al Sistema de Pago de Obligaciones Tributarias", new XAttribute("languageLocaleID", "2006")));

            doc.Add(new XElement(Cbc + "DocumentCurrencyCode", mon));
            doc.Add(new XElement(Cbc + "LineCountNumeric", c.Items.Count));

            if (tipo is "07" or "08")
            {
                doc.Add(new XElement(Cac + "DiscrepancyResponse",
                    new XElement(Cbc + "ReferenceID", $"{c.RefSerie}-{c.RefNumero}"),
                    new XElement(Cbc + "ResponseCode", c.MotivoCodigo),
                    new XElement(Cbc + "Description", c.MotivoDescripcion)));
                doc.Add(new XElement(Cac + "BillingReference",
                    new XElement(Cac + "InvoiceDocumentReference",
                        new XElement(Cbc + "ID", $"{c.RefSerie}-{c.RefNumero}"),
                        new XElement(Cbc + "DocumentTypeCode", c.RefTipoDocumento))));
            }

            if (!string.IsNullOrWhiteSpace(c.OrdenCompraRef))
                doc.Add(new XElement(Cac + "OrderReference", new XElement(Cbc + "ID", c.OrdenCompraRef!.Trim())));
            if (!string.IsNullOrWhiteSpace(c.GuiaRef))
                doc.Add(new XElement(Cac + "DespatchDocumentReference",
                    new XElement(Cbc + "ID", c.GuiaRef!.Trim()),
                    new XElement(Cbc + "DocumentTypeCode", "09")));

            doc.Add(FirmaRef(id));
            doc.Add(Proveedor());
            doc.Add(Cliente(c));

            if (tipo == "01") AgregarFormaDePago(doc, c);

            doc.Add(TaxTotalCabecera(c));
            doc.Add(tipo == "08"
                ? new XElement(Cac + "RequestedMonetaryTotal", Totales(c, incluirLinea: false))
                : new XElement(Cac + "LegalMonetaryTotal", Totales(c, incluirLinea: true)));

            foreach (var (it, i) in c.Items.OrderBy(x => x.Orden).Select((x, i) => (x, i + 1)))
                doc.Add(Linea(c, it, i, ns, raiz));

            return Serializar(doc);
        }

        // ── Cabecera ────────────────────────────────────────────────────────────

        private XElement FirmaRef(string id) =>
            new(Cac + "Signature",
                new XElement(Cbc + "ID", id),
                new XElement(Cac + "SignatoryParty",
                    new XElement(Cac + "PartyIdentification", new XElement(Cbc + "ID", _s.RucEmisor)),
                    new XElement(Cac + "PartyName", Cdata(Cbc + "Name", _s.RazonSocialEmisor))),
                new XElement(Cac + "DigitalSignatureAttachment",
                    new XElement(Cac + "ExternalReference", new XElement(Cbc + "URI", "#SignatureSP"))));

        private XElement Proveedor()
        {
            var legal = new XElement(Cac + "PartyLegalEntity",
                Cdata(Cbc + "RegistrationName", _s.RazonSocialEmisor),
                new XElement(Cac + "RegistrationAddress",
                    new XElement(Cbc + "ID", string.IsNullOrWhiteSpace(_s.Ubigeo) ? "150101" : _s.Ubigeo),
                    new XElement(Cbc + "AddressTypeCode", string.IsNullOrWhiteSpace(_s.CodigoEstablecimiento) ? "0000" : _s.CodigoEstablecimiento),
                    new XElement(Cbc + "CityName", _s.Provincia),
                    new XElement(Cbc + "CountrySubentity", _s.Departamento),
                    new XElement(Cbc + "District", _s.Distrito),
                    new XElement(Cac + "AddressLine", Cdata(Cbc + "Line", string.IsNullOrWhiteSpace(_s.Direccion) ? "-" : _s.Direccion)),
                    new XElement(Cac + "Country", new XElement(Cbc + "IdentificationCode", "PE"))));

            return new XElement(Cac + "AccountingSupplierParty",
                new XElement(Cac + "Party",
                    new XElement(Cac + "PartyIdentification", new XElement(Cbc + "ID", new XAttribute("schemeID", "6"), _s.RucEmisor)),
                    new XElement(Cac + "PartyName", Cdata(Cbc + "Name", string.IsNullOrWhiteSpace(_s.NombreComercial) ? _s.RazonSocialEmisor : _s.NombreComercial)),
                    legal));
        }

        private static XElement Cliente(Cpe c)
        {
            var legal = new XElement(Cac + "PartyLegalEntity", Cdata(Cbc + "RegistrationName", c.ClienteRazon));
            if (!string.IsNullOrWhiteSpace(c.ClienteDireccion))
                legal.Add(new XElement(Cac + "RegistrationAddress",
                    new XElement(Cac + "AddressLine", Cdata(Cbc + "Line", c.ClienteDireccion!)),
                    new XElement(Cac + "Country", new XElement(Cbc + "IdentificationCode", c.TipoOperacion == "0200" ? (c.ClientePais ?? "XX") : "PE"))));

            return new XElement(Cac + "AccountingCustomerParty",
                new XElement(Cac + "Party",
                    new XElement(Cac + "PartyIdentification", new XElement(Cbc + "ID", new XAttribute("schemeID", c.ClienteTipoDoc), c.ClienteNumeroDoc)),
                    legal));
        }

        private static void AgregarFormaDePago(XElement doc, Cpe c)
        {
            var mon = c.Moneda;

            if (!string.IsNullOrWhiteSpace(c.DetraccionCodigo))
            {
                doc.Add(new XElement(Cac + "PaymentMeans",
                    new XElement(Cbc + "ID", "Detraccion"),
                    new XElement(Cbc + "PaymentMeansCode", "001"),
                    new XElement(Cac + "PayeeFinancialAccount", new XElement(Cbc + "ID", c.DetraccionCuenta))));
                doc.Add(new XElement(Cac + "PaymentTerms",
                    new XElement(Cbc + "ID", "Detraccion"),
                    new XElement(Cbc + "PaymentMeansID", c.DetraccionCodigo),
                    new XElement(Cbc + "PaymentPercent", M(c.DetraccionPorcentaje ?? 0)),
                    Amt(Cbc + "Amount", "PEN", c.DetraccionMonto ?? 0)));
            }

            if (c.FormaPago == "CREDITO")
            {
                var cuotas = CpeValidador.LeerCuotas(c.CuotasJson);
                var neto = c.Total - (mon == "PEN" ? c.DetraccionMonto ?? 0 : 0);
                doc.Add(new XElement(Cac + "PaymentTerms",
                    new XElement(Cbc + "ID", "FormaPago"),
                    new XElement(Cbc + "PaymentMeansID", "Credito"),
                    Amt(Cbc + "Amount", mon, neto)));
                foreach (var (q, i) in cuotas.Select((q, i) => (q, i + 1)))
                    doc.Add(new XElement(Cac + "PaymentTerms",
                        new XElement(Cbc + "ID", "FormaPago"),
                        new XElement(Cbc + "PaymentMeansID", $"Cuota{i:000}"),
                        Amt(Cbc + "Amount", mon, q.Monto),
                        new XElement(Cbc + "PaymentDueDate", D(q.Fecha))));
            }
            else
            {
                doc.Add(new XElement(Cac + "PaymentTerms",
                    new XElement(Cbc + "ID", "FormaPago"),
                    new XElement(Cbc + "PaymentMeansID", "Contado")));
            }
        }

        // ── Totales ─────────────────────────────────────────────────────────────

        private static XElement TaxScheme(string codigo, string nombre, string tipo) =>
            new(Cac + "TaxScheme",
                new XElement(Cbc + "ID", codigo),
                new XElement(Cbc + "Name", nombre),
                new XElement(Cbc + "TaxTypeCode", tipo));

        private static XElement Subtotal(string moneda, decimal baseImponible, decimal impuesto, string categoria, string codigo, string nombre, string tipo) =>
            new(Cac + "TaxSubtotal",
                Amt(Cbc + "TaxableAmount", moneda, baseImponible),
                Amt(Cbc + "TaxAmount", moneda, impuesto),
                new XElement(Cac + "TaxCategory",
                    new XElement(Cbc + "ID", categoria),
                    TaxScheme(codigo, nombre, tipo)));

        private static XElement TaxTotalCabecera(Cpe c)
        {
            var t = new XElement(Cac + "TaxTotal", Amt(Cbc + "TaxAmount", c.Moneda, c.Igv));
            // El bloque de IGV (1000) va si hay base gravada, o si no hay ningún otro grupo. En una exportación SUNAT lo rechaza (3107).
            var hayOtros = c.TotalExonerado > 0 || c.TotalInafecto > 0 || c.TotalExportacion > 0;
            if (c.TotalGravado > 0 || !hayOtros)
                t.Add(Subtotal(c.Moneda, c.TotalGravado, c.Igv, "S", "1000", "IGV", "VAT"));
            if (c.TotalExonerado > 0) t.Add(Subtotal(c.Moneda, c.TotalExonerado, 0, "E", "9997", "EXO", "VAT"));
            if (c.TotalInafecto > 0) t.Add(Subtotal(c.Moneda, c.TotalInafecto, 0, "O", "9998", "INA", "FRE"));
            if (c.TotalExportacion > 0) t.Add(Subtotal(c.Moneda, c.TotalExportacion, 0, "G", "9995", "EXP", "FRE"));
            return t;
        }

        private static IEnumerable<XElement> Totales(Cpe c, bool incluirLinea)
        {
            var valorVenta = c.TotalGravado + c.TotalExonerado + c.TotalInafecto + c.TotalExportacion;
            if (incluirLinea)
            {
                yield return Amt(Cbc + "LineExtensionAmount", c.Moneda, valorVenta);
                yield return Amt(Cbc + "TaxInclusiveAmount", c.Moneda, c.Total);
            }
            yield return Amt(Cbc + "PayableAmount", c.Moneda, c.Total);
        }

        // ── Líneas ──────────────────────────────────────────────────────────────

        private static XElement Linea(Cpe c, CpeItem it, int n, XNamespace ns, string raiz)
        {
            var mon = c.Moneda;
            var (nombreLinea, nombreCantidad) = raiz switch
            {
                "Invoice" => ("InvoiceLine", "InvoicedQuantity"),
                "CreditNote" => ("CreditNoteLine", "CreditedQuantity"),
                _ => ("DebitNoteLine", "DebitedQuantity"),
            };
            var gravada = it.AfectacionIgv.StartsWith('1');

            var linea = new XElement(Cac + nombreLinea,
                new XElement(Cbc + "ID", n),
                new XElement(Cbc + nombreCantidad, new XAttribute("unitCode", it.UnidadMedida), Q(it.Cantidad)),
                Amt(Cbc + "LineExtensionAmount", mon, it.ValorVenta),
                new XElement(Cac + "PricingReference",
                    new XElement(Cac + "AlternativeConditionPrice",
                        new XElement(Cbc + "PriceAmount", new XAttribute("currencyID", mon), CpeCalculo.PrecioConIgv(it).ToString("0.0000", CultureInfo.InvariantCulture)),
                        new XElement(Cbc + "PriceTypeCode", "01"))));

            if (it.Descuento > 0)
            {
                var baseDescuento = CpeCalculo.R2(it.Cantidad * it.ValorUnitario);
                var factor = baseDescuento > 0 ? Math.Round(it.Descuento / baseDescuento, 5) : 0;
                linea.Add(new XElement(Cac + "AllowanceCharge",
                    new XElement(Cbc + "ChargeIndicator", "false"),
                    new XElement(Cbc + "AllowanceChargeReasonCode", "00"),
                    new XElement(Cbc + "MultiplierFactorNumeric", factor.ToString("0.#####", CultureInfo.InvariantCulture)),
                    Amt(Cbc + "Amount", mon, it.Descuento),
                    Amt(Cbc + "BaseAmount", mon, baseDescuento)));
            }

            var (categoria, esquema, nombreEsq, tipoEsq) = it.AfectacionIgv[0] switch
            {
                '1' => ("S", "1000", "IGV", "VAT"),
                '2' => ("E", "9997", "EXO", "VAT"),
                '3' => ("O", "9998", "INA", "FRE"),
                _ => ("G", "9995", "EXP", "FRE"),
            };
            linea.Add(new XElement(Cac + "TaxTotal",
                Amt(Cbc + "TaxAmount", mon, it.Igv),
                new XElement(Cac + "TaxSubtotal",
                    Amt(Cbc + "TaxableAmount", mon, it.ValorVenta),
                    Amt(Cbc + "TaxAmount", mon, it.Igv),
                    new XElement(Cac + "TaxCategory",
                        new XElement(Cbc + "ID", categoria),
                        new XElement(Cbc + "Percent", gravada ? "18.00" : "0.00"),
                        new XElement(Cbc + "TaxExemptionReasonCode", it.AfectacionIgv),
                        TaxScheme(esquema, nombreEsq, tipoEsq)))));

            var item = new XElement(Cac + "Item", Cdata(Cbc + "Description", it.Descripcion));
            if (!string.IsNullOrWhiteSpace(it.Codigo))
                item.Add(new XElement(Cac + "SellersItemIdentification", new XElement(Cbc + "ID", it.Codigo)));
            linea.Add(item);

            linea.Add(new XElement(Cac + "Price",
                new XElement(Cbc + "PriceAmount", new XAttribute("currencyID", mon), it.ValorUnitario.ToString("0.0000", CultureInfo.InvariantCulture))));
            return linea;
        }

        // ── Comunicación de baja ────────────────────────────────────────────────

        public string BuildBaja(Cpe c, string identificador, DateOnly hoy)
        {
            XNamespace ns = "urn:sunat:names:specification:ubl:peru:schema:xsd:VoidedDocuments-1";
            var doc = new XElement(ns + "VoidedDocuments",
                new XAttribute(XNamespace.Xmlns + "cac", Cac),
                new XAttribute(XNamespace.Xmlns + "cbc", Cbc),
                new XAttribute(XNamespace.Xmlns + "ds", Ds),
                new XAttribute(XNamespace.Xmlns + "ext", Ext),
                new XAttribute(XNamespace.Xmlns + "sac", Sac),
                new XElement(Ext + "UBLExtensions", new XElement(Ext + "UBLExtension", new XElement(Ext + "ExtensionContent"))),
                new XElement(Cbc + "UBLVersionID", "2.0"),
                new XElement(Cbc + "CustomizationID", "1.0"),
                new XElement(Cbc + "ID", identificador),
                new XElement(Cbc + "ReferenceDate", D(c.FechaEmision)),
                new XElement(Cbc + "IssueDate", D(hoy)),
                FirmaRef(identificador),
                new XElement(Cac + "AccountingSupplierParty",
                    new XElement(Cbc + "CustomerAssignedAccountID", _s.RucEmisor),
                    new XElement(Cbc + "AdditionalAccountID", "6"),
                    new XElement(Cac + "Party",
                        new XElement(Cac + "PartyLegalEntity", Cdata(Cbc + "RegistrationName", _s.RazonSocialEmisor)))),
                new XElement(Sac + "VoidedDocumentsLine",
                    new XElement(Cbc + "LineID", 1),
                    new XElement(Cbc + "DocumentTypeCode", c.TipoDocumento),
                    new XElement(Sac + "DocumentSerialID", c.Serie),
                    new XElement(Sac + "DocumentNumberID", c.Numero),
                    new XElement(Sac + "VoidReasonDescription", c.BajaMotivo)));
            return Serializar(doc);
        }

        private static string Serializar(XElement raiz)
        {
            using var writer = new Utf8StringWriter();
            using (var xw = XmlWriter.Create(writer, new XmlWriterSettings { OmitXmlDeclaration = false, Indent = false }))
            {
                new XDocument(new XDeclaration("1.0", "UTF-8", null), raiz).Save(xw);
            }
            return writer.ToString();
        }
    }
}

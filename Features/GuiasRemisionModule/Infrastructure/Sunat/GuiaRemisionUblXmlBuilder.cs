using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Abril_Backend.Features.GuiasRemisionModule.Infrastructure.Models;
using Microsoft.Extensions.Options;

namespace Abril_Backend.Features.GuiasRemisionModule.Infrastructure.Sunat
{
    /// <summary>
    /// Genera el XML UBL 2.1 (DespatchAdvice, tipo 09 = Guía de Remisión Remitente) sin firmar.
    ///
    /// [PENDIENTE VALIDAR] La estructura sigue el modelo oficial de SUNAT (Guía de Elaboración de
    /// Documentos XML - Guía de Remisión Remitente) y el mapeo usado por implementaciones ya
    /// probadas en producción en Perú (ej. Greenter), pero SUNAT valida contra un XSD estricto con
    /// varias reglas condicionales (ej. campos obligatorios solo si modalidad es pública/privada).
    /// La primera guía real que se envíe al Beta casi seguro devuelve algún error de validación
    /// puntual (campo faltante/formato) — es normal, se ajusta este builder según el CDR/fault
    /// que SUNAT responda, no hace falta adivinar cada regla de antemano.
    /// </summary>
    public class GuiaRemisionUblXmlBuilder : IGuiaRemisionXmlBuilder
    {
        private static readonly XNamespace Ns = "urn:oasis:names:specification:ubl:schema:xsd:DespatchAdvice-2";
        private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
        private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
        private static readonly XNamespace Ext = "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2";

        private readonly SunatGreSettings _settings;

        public GuiaRemisionUblXmlBuilder(IOptions<SunatGreSettings> settings)
        {
            _settings = settings.Value;
        }

        public string Build(GuiaRemision guia)
        {
            if (guia.AlmacenOrigen == null)
                throw new InvalidOperationException("GuiaRemision.AlmacenOrigen debe estar cargado antes de construir el XML.");

            // Hora de Perú (UTC-5): SUNAT rechaza fechas de emisión futuras y exige que el inicio
            // del traslado no sea anterior a la fecha de emisión.
            var emision = DateTime.UtcNow.AddHours(-5);
            var hoy = DateOnly.FromDateTime(emision);
            var inicioTraslado = (guia.FechaTraslado < hoy ? hoy : guia.FechaTraslado)
                .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var esTrasladoPropio = guia.AlmacenDestinoId.HasValue;

            var destinatarioRuc = esTrasladoPropio ? _settings.Ruc : guia.DestinatarioRuc ?? _settings.Ruc;
            var destinatarioNombre = esTrasladoPropio
                ? _settings.RazonSocial
                : guia.DestinatarioRazonSocial ?? _settings.RazonSocial;

            var direccionDestino = esTrasladoPropio ? guia.AlmacenDestino?.Direccion : null;
            var ubigeoDestino = esTrasladoPropio ? guia.AlmacenDestino?.Ubigeo : null;

            var doc = new XElement(Ns + "DespatchAdvice",
                new XAttribute(XNamespace.Xmlns + "cac", Cac),
                new XAttribute(XNamespace.Xmlns + "cbc", Cbc),
                new XAttribute(XNamespace.Xmlns + "ext", Ext),

                // Contenedor de la firma XMLDSig — se completa en el paso de firmado (XmlDsigSigner),
                // acá queda vacío para que el firmador lo encuentre por posición.
                new XElement(Ext + "UBLExtensions",
                    new XElement(Ext + "UBLExtension",
                        new XElement(Ext + "ExtensionContent"))),

                new XElement(Cbc + "UBLVersionID", "2.1"),
                new XElement(Cbc + "CustomizationID", "2.0"),
                new XElement(Cbc + "ID", $"{guia.Serie}-{guia.Numero}"),
                new XElement(Cbc + "IssueDate", emision.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                new XElement(Cbc + "IssueTime", emision.ToString("HH:mm:ss", CultureInfo.InvariantCulture)),
                new XElement(Cbc + "DespatchAdviceTypeCode", "09"),
                string.IsNullOrWhiteSpace(guia.Observacion) ? null : new XElement(Cbc + "Note", guia.Observacion),

                new XElement(Cac + "Signature",
                    new XElement(Cbc + "ID", _settings.Ruc),
                    new XElement(Cac + "SignatoryParty",
                        new XElement(Cac + "PartyIdentification", new XElement(Cbc + "ID", _settings.Ruc)),
                        new XElement(Cac + "PartyName", new XElement(Cbc + "Name", _settings.RazonSocial))),
                    new XElement(Cac + "DigitalSignatureAttachment",
                        new XElement(Cac + "ExternalReference", new XElement(Cbc + "URI", "#SignatureSP")))),

                new XElement(Cac + "DespatchSupplierParty",
                    new XElement(Cac + "Party",
                        new XElement(Cac + "PartyIdentification",
                            new XElement(Cbc + "ID", new XAttribute("schemeID", "6"), _settings.Ruc)),
                        new XElement(Cac + "PartyLegalEntity",
                            new XElement(Cbc + "RegistrationName", _settings.RazonSocial)))),

                new XElement(Cac + "DeliveryCustomerParty",
                    new XElement(Cac + "Party",
                        new XElement(Cac + "PartyIdentification",
                            new XElement(Cbc + "ID", new XAttribute("schemeID", "6"), destinatarioRuc)),
                        new XElement(Cac + "PartyLegalEntity",
                            new XElement(Cbc + "RegistrationName", destinatarioNombre)))),

                BuildShipment(guia, direccionDestino, ubigeoDestino, inicioTraslado),

                guia.Items.Select((item, i) => BuildDespatchLine(item, i + 1))
            );

            // XDocument.ToString() nunca emite la declaración <?xml ...?> (limitación conocida de
            // LINQ-to-XML) — SUNAT sí la exige, así que se serializa con XmlWriter en su lugar.
            var xDoc = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), doc);
            using var writer = new StringWriter(new StringBuilder());
            using (var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Encoding = Encoding.UTF8, OmitXmlDeclaration = false }))
            {
                xDoc.Save(xmlWriter);
            }
            return writer.ToString();
        }

        private XElement BuildShipment(GuiaRemision guia, string? direccionDestino, string? ubigeoDestino, string inicioTraslado)
        {
            // Orden exigido por el XSD UBL: ID, HandlingCode (motivo), peso, bultos, ShipmentStage,
            // Delivery, TransportHandlingUnit (vehículo). Un elemento fuera de lugar rechaza todo el XML.
            var shipment = new XElement(Cac + "Shipment",
                new XElement(Cbc + "ID", "SUNAT_Envio"),
                new XElement(Cbc + "HandlingCode", guia.MotivoTraslado),
                new XElement(Cbc + "GrossWeightMeasure",
                    new XAttribute("unitCode", guia.PesoBrutoUnidad),
                    guia.PesoBrutoTotal.ToString(CultureInfo.InvariantCulture)),
                guia.NumBultos.HasValue
                    ? new XElement(Cbc + "TotalTransportHandlingUnitQuantity", guia.NumBultos.Value)
                    : null);

            // Modalidad '01' = transporte público -> transportista (empresa de transporte).
            // Modalidad '02' = transporte privado -> vehículo propio + conductor.
            var stage = new XElement(Cac + "ShipmentStage",
                new XElement(Cbc + "TransportModeCode", guia.ModalidadTraslado),
                new XElement(Cac + "TransitPeriod", new XElement(Cbc + "StartDate", inicioTraslado)));

            if (guia.ModalidadTraslado == "01" && guia.TransportistaRuc != null)
            {
                stage.Add(new XElement(Cac + "CarrierParty",
                    new XElement(Cac + "PartyIdentification",
                        new XElement(Cbc + "ID", new XAttribute("schemeID", "6"), guia.TransportistaRuc)),
                    new XElement(Cac + "PartyLegalEntity",
                        new XElement(Cbc + "RegistrationName", guia.TransportistaRazonSocial))));
            }
            else if (guia.ModalidadTraslado == "02" && guia.ConductorNombres != null)
            {
                // El nombre se guarda como "Apellidos Nombres" (ej. FLORES VILCA MELVIN NEBER): los dos
                // primeros términos son apellidos y el resto nombres; con menos de 3 términos, 1 apellido.
                string apellido, nombre;
                if (!string.IsNullOrWhiteSpace(guia.ConductorApellidos))
                {
                    // Guías nuevas: apellidos y nombres vienen separados (de los datos maestros o del formulario).
                    apellido = guia.ConductorApellidos.Trim();
                    nombre = guia.ConductorNombres.Trim();
                }
                else
                {
                    var partes = guia.ConductorNombres.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var nApellidos = partes.Length >= 3 ? 2 : 1;
                    apellido = string.Join(' ', partes.Take(nApellidos));
                    nombre = partes.Length > nApellidos ? string.Join(' ', partes.Skip(nApellidos)) : apellido;
                }
                stage.Add(new XElement(Cac + "DriverPerson",
                    new XElement(Cbc + "ID", new XAttribute("schemeID", "1"), guia.ConductorDni),
                    new XElement(Cbc + "FirstName", nombre),
                    new XElement(Cbc + "FamilyName", apellido),
                    new XElement(Cbc + "JobTitle", "Principal"),
                    new XElement(Cac + "IdentityDocumentReference",
                        new XElement(Cbc + "ID", guia.ConductorLicencia))));

                // Conductores adicionales: mismo bloque con cargo "Secundario".
                foreach (var sec in GuiaSecundarios.Leer<ConductorSecundario>(guia.ConductoresSecundariosJson))
                {
                    stage.Add(new XElement(Cac + "DriverPerson",
                        new XElement(Cbc + "ID", new XAttribute("schemeID", "1"), sec.NumDoc),
                        new XElement(Cbc + "FirstName", sec.Nombres),
                        new XElement(Cbc + "FamilyName", sec.Apellidos),
                        new XElement(Cbc + "JobTitle", "Secundario"),
                        new XElement(Cac + "IdentityDocumentReference",
                            new XElement(Cbc + "ID", sec.Licencia))));
                }
            }
            shipment.Add(stage);

            // Motivo 04 (traslado entre establecimientos de la misma empresa): SUNAT pide el código
            // del establecimiento anexo de cada punto ("0000" = domicilio fiscal); sale de cada almacén.
            var esMismaEmpresa = guia.MotivoTraslado == "04";
            XElement? CodigoEstablecimiento(string? codigo) => esMismaEmpresa
                ? new XElement(Cbc + "AddressTypeCode",
                    new XAttribute("listID", _settings.Ruc),
                    new XAttribute("listAgencyName", "PE:SUNAT"),
                    new XAttribute("listName", "Establecimientos anexos"),
                    string.IsNullOrWhiteSpace(codigo) ? "0000" : codigo)
                : null;
            XElement UbigeoId(string v) => new(Cbc + "ID",
                new XAttribute("schemeAgencyName", "PE:INEI"),
                new XAttribute("schemeName", "Ubigeos"), v);

            shipment.Add(new XElement(Cac + "Delivery",
                new XElement(Cac + "DeliveryAddress",
                    ubigeoDestino != null ? UbigeoId(ubigeoDestino) : null,
                    CodigoEstablecimiento(guia.AlmacenDestino?.CodigoEstablecimientoSunat),
                    direccionDestino != null ? new XElement(Cac + "AddressLine", new XElement(Cbc + "Line", direccionDestino)) : null),
                new XElement(Cac + "Despatch",
                    new XElement(Cac + "DespatchAddress",
                        guia.AlmacenOrigen!.Ubigeo != null ? UbigeoId(guia.AlmacenOrigen.Ubigeo) : null,
                        CodigoEstablecimiento(guia.AlmacenOrigen.CodigoEstablecimientoSunat),
                        guia.AlmacenOrigen.Direccion != null ? new XElement(Cac + "AddressLine", new XElement(Cbc + "Line", guia.AlmacenOrigen.Direccion)) : null))));

            if (guia.ModalidadTraslado == "02" && guia.VehiculoPlaca != null)
            {
                // Tarjeta de circulación (si se registró) dentro de ApplicableTransportMeans; los vehículos
                // adicionales (remolque/semirremolque) van como AttachedTransportEquipment del principal.
                static XElement? Circulacion(string? nro) => string.IsNullOrWhiteSpace(nro)
                    ? null
                    : new XElement(Cac + "ApplicableTransportMeans", new XElement(Cbc + "RegistrationNationalityID", nro));

                var equipo = new XElement(Cac + "TransportEquipment",
                    new XElement(Cbc + "ID", guia.VehiculoPlaca),
                    Circulacion(guia.VehiculoNroCirculacion));
                foreach (var sec in GuiaSecundarios.Leer<VehiculoSecundario>(guia.VehiculosSecundariosJson))
                {
                    equipo.Add(new XElement(Cac + "AttachedTransportEquipment",
                        new XElement(Cbc + "ID", sec.Placa),
                        Circulacion(sec.NroCirculacion)));
                }
                shipment.Add(new XElement(Cac + "TransportHandlingUnit", equipo));
            }

            return shipment;
        }

        /// <summary>Descripción detallada del bien: nombre + talla/color cuando aplican (como en la guía impresa de SUNAT).</summary>
        private static string DescripcionBien(GuiaRemisionItem item)
        {
            var partes = new List<string> { item.Producto?.Nombre ?? item.Descripcion ?? "" };
            if (!string.IsNullOrWhiteSpace(item.Talla) && item.Talla != "—") partes.Add($"TALLA {item.Talla}");
            if (!string.IsNullOrWhiteSpace(item.Color) && item.Color != "—") partes.Add($"COLOR {item.Color}");
            if (item.EsBienTercero) partes.Add("BIEN DE TERCERO");
            return string.Join(" - ", partes.Where(p => p.Length > 0));
        }

        private XElement BuildDespatchLine(GuiaRemisionItem item, int lineId)
        {
            return new XElement(Cac + "DespatchLine",
                new XElement(Cbc + "ID", lineId),
                new XElement(Cbc + "DeliveredQuantity",
                    new XAttribute("unitCode", item.UnidadMedida),
                    item.Cantidad.ToString(CultureInfo.InvariantCulture)),
                new XElement(Cac + "OrderLineReference", new XElement(Cbc + "LineID", lineId)),
                new XElement(Cac + "Item",
                    new XElement(Cbc + "Description", DescripcionBien(item)),
                    item.Producto?.Codigo != null
                        ? new XElement(Cac + "SellersItemIdentification", new XElement(Cbc + "ID", item.Producto.Codigo))
                        : null));
        }
    }
}

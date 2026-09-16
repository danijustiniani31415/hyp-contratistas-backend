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

            var fecha = guia.FechaTraslado.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
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
                new XElement(Cbc + "IssueDate", fecha),
                new XElement(Cbc + "DespatchAdviceTypeCode", "09"),

                new XElement(Cac + "DespatchSupplierParty",
                    new XElement(Cbc + "CustomerAssignedAccountID", _settings.Ruc),
                    new XElement(Cac + "Party",
                        new XElement(Cac + "PartyLegalEntity",
                            new XElement(Cbc + "RegistrationName", _settings.RazonSocial)))),

                new XElement(Cac + "DeliveryCustomerParty",
                    new XElement(Cbc + "CustomerAssignedAccountID", destinatarioRuc),
                    new XElement(Cac + "Party",
                        new XElement(Cac + "PartyLegalEntity",
                            new XElement(Cbc + "RegistrationName", destinatarioNombre)))),

                BuildShipment(guia, direccionDestino, ubigeoDestino, fecha),

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

        private XElement BuildShipment(GuiaRemision guia, string? direccionDestino, string? ubigeoDestino, string fecha)
        {
            var shipment = new XElement(Cac + "Shipment",
                new XElement(Cbc + "ID", "1"),
                new XElement(Cbc + "HandlingCode", guia.ModalidadTraslado),
                new XElement(Cbc + "GrossWeightMeasure",
                    new XAttribute("unitCode", guia.PesoBrutoUnidad),
                    guia.PesoBrutoTotal.ToString(CultureInfo.InvariantCulture)));

            if (guia.NumBultos.HasValue)
            {
                shipment.Add(new XElement(Cac + "TransportHandlingUnit",
                    new XElement(Cbc + "PackageQuantity", guia.NumBultos.Value)));
            }

            shipment.Add(new XElement(Cac + "Delivery",
                new XElement(Cac + "DeliveryAddress",
                    ubigeoDestino != null ? new XElement(Cbc + "ID", ubigeoDestino) : null,
                    direccionDestino != null ? new XElement(Cac + "AddressLine", new XElement(Cbc + "Line", direccionDestino)) : null),
                new XElement(Cac + "Despatch",
                    new XElement(Cbc + "ActualDespatchDate", fecha),
                    new XElement(Cac + "DespatchAddress",
                        guia.AlmacenOrigen!.Ubigeo != null ? new XElement(Cbc + "ID", guia.AlmacenOrigen.Ubigeo) : null,
                        guia.AlmacenOrigen.Direccion != null ? new XElement(Cac + "AddressLine", new XElement(Cbc + "Line", guia.AlmacenOrigen.Direccion)) : null))));

            // Modalidad '01' = transporte público -> va el transportista (empresa de transporte).
            // Modalidad '02' = transporte privado -> va vehículo propio + conductor.
            if (guia.ModalidadTraslado == "01" && guia.TransportistaRuc != null)
            {
                shipment.Add(new XElement(Cac + "CarrierParty",
                    new XElement(Cac + "PartyIdentification", new XElement(Cbc + "ID", guia.TransportistaRuc)),
                    new XElement(Cac + "PartyLegalEntity", new XElement(Cbc + "RegistrationName", guia.TransportistaRazonSocial))));
            }
            else if (guia.ModalidadTraslado == "02" && guia.VehiculoPlaca != null)
            {
                shipment.Add(new XElement(Cac + "TransportHandlingUnit",
                    new XElement(Cac + "TransportEquipment", new XElement(Cbc + "ID", guia.VehiculoPlaca))));

                if (guia.ConductorNombres != null)
                {
                    shipment.Add(new XElement(Cac + "DriverPerson",
                        new XElement(Cbc + "ID", guia.ConductorLicencia),
                        new XElement(Cbc + "FirstName", guia.ConductorNombres)));
                }
            }

            return shipment;
        }

        private XElement BuildDespatchLine(GuiaRemisionItem item, int lineId)
        {
            return new XElement(Ns + "DespatchLine",
                new XElement(Cbc + "ID", lineId),
                new XElement(Cbc + "DeliveredQuantity",
                    new XAttribute("unitCode", item.UnidadMedida),
                    item.Cantidad.ToString(CultureInfo.InvariantCulture)),
                new XElement(Cac + "OrderLineReference", new XElement(Cbc + "LineID", lineId)),
                new XElement(Cac + "Item",
                    new XElement(Cbc + "Description", item.Producto?.Nombre),
                    item.Producto?.Codigo != null
                        ? new XElement(Cac + "SellersItemIdentification", new XElement(Cbc + "ID", item.Producto.Codigo))
                        : null));
        }
    }
}

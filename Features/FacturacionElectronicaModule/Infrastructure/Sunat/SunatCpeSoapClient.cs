using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Options;

namespace Abril_Backend.Features.FacturacionElectronicaModule.Infrastructure.Sunat
{
    public class CpeEnvioResultado
    {
        /// <summary>true si SUNAT devolvió un CDR con código 0 o de aceptación con observaciones (4000+).</summary>
        public bool Aceptado { get; set; }
        /// <summary>true si el código es de aceptación con observaciones (el comprobante vale, pero hay algo que corregir a futuro).</summary>
        public bool ConObservaciones { get; set; }
        public string? Codigo { get; set; }
        public string? Descripcion { get; set; }
        public string? CdrXml { get; set; }
    }

    public class CpeTicketResultado
    {
        public string? Ticket { get; set; }
    }

    public class CpeConsultaTicketResultado
    {
        /// <summary>98 en SUNAT: el proceso sigue en curso, hay que volver a consultar.</summary>
        public bool EnProceso { get; set; }
        public bool Aceptado { get; set; }
        public string? Codigo { get; set; }
        public string? Descripcion { get; set; }
        public string? CdrXml { get; set; }
    }

    /// <summary>Error que SUNAT devolvió como SOAP Fault: el documento no quedó registrado.</summary>
    public class SunatFaultException : Exception
    {
        public string Codigo { get; }
        public SunatFaultException(string codigo, string mensaje) : base(mensaje) { Codigo = codigo; }
    }

    /// <summary>Resultado de preguntarle a SUNAT por un comprobante concreto.</summary>
    public class CpeExistenciaResultado
    {
        /// <summary>true si SUNAT ya tiene registrado ese número (aceptado, rechazado o dado de baja).</summary>
        public bool Existe { get; set; }
        public string? Codigo { get; set; }
        public string? Mensaje { get; set; }
    }

    public interface ISunatCpeClient
    {
        /// <summary>Consulta si SUNAT ya tiene un comprobante con ese tipo, serie y número (getStatusCdr).</summary>
        Task<CpeExistenciaResultado> ConsultarExistenciaAsync(string tipo, string serie, long numero);
        /// <summary>Envía una factura o nota (sendBill). SUNAT responde el CDR en la misma llamada.</summary>
        Task<CpeEnvioResultado> EnviarComprobanteAsync(string xmlFirmado, string nombreSinExtension);
        /// <summary>Envía una comunicación de baja (sendSummary). Devuelve un ticket que se consulta después.</summary>
        Task<CpeTicketResultado> EnviarBajaAsync(string xmlFirmado, string nombreSinExtension);
        Task<CpeConsultaTicketResultado> ConsultarTicketAsync(string ticket);
    }

    /// <summary>
    /// Cliente SOAP del servicio billService de SUNAT, con autenticación WS-Security (usuario = RUC + usuario SOL).
    /// [VERIFICAR] URL y nombres de operación contra la documentación vigente; son los de la plataforma SEE-SOL clásica.
    /// </summary>
    public class SunatCpeSoapClient : ISunatCpeClient
    {
        private static readonly XNamespace Soap = "http://schemas.xmlsoap.org/soap/envelope/";
        private static readonly XNamespace Ser = "http://service.sunat.gob.pe";
        private static readonly XNamespace Wsse = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

        private readonly HttpClient _http;
        private readonly SunatCpeSettings _s;

        public SunatCpeSoapClient(HttpClient http, IOptions<SunatCpeSettings> settings)
        {
            _http = http;
            _s = settings.Value;
        }

        public async Task<CpeEnvioResultado> EnviarComprobanteAsync(string xmlFirmado, string nombreSinExtension)
        {
            var respuesta = await LlamarAsync("sendBill", Cuerpo("sendBill", nombreSinExtension, xmlFirmado));
            var b64 = respuesta.Descendants().FirstOrDefault(e => e.Name.LocalName == "applicationResponse")?.Value
                ?? throw new InvalidOperationException("SUNAT respondió sin el CDR (applicationResponse).");
            var cdr = LeerCdr(Convert.FromBase64String(b64));
            var cod = cdr.Codigo ?? "";
            var ok = cod == "0" || (int.TryParse(cod, out var n) && n >= 4000);
            return new CpeEnvioResultado
            {
                Aceptado = ok,
                ConObservaciones = ok && cod != "0",
                Codigo = cdr.Codigo,
                Descripcion = cdr.Descripcion,
                CdrXml = cdr.Xml,
            };
        }

        public async Task<CpeTicketResultado> EnviarBajaAsync(string xmlFirmado, string nombreSinExtension)
        {
            var respuesta = await LlamarAsync("sendSummary", Cuerpo("sendSummary", nombreSinExtension, xmlFirmado));
            var ticket = respuesta.Descendants().FirstOrDefault(e => e.Name.LocalName == "ticket")?.Value
                ?? throw new InvalidOperationException("SUNAT respondió sin ticket para la baja.");
            return new CpeTicketResultado { Ticket = ticket };
        }

        public async Task<CpeConsultaTicketResultado> ConsultarTicketAsync(string ticket)
        {
            var cuerpo = new XElement(Ser + "getStatus", new XElement("ticket", ticket));
            var respuesta = await LlamarAsync("getStatus", cuerpo);
            var statusCode = respuesta.Descendants().FirstOrDefault(e => e.Name.LocalName == "statusCode")?.Value;
            if (statusCode == "98") return new CpeConsultaTicketResultado { EnProceso = true };

            var b64 = respuesta.Descendants().FirstOrDefault(e => e.Name.LocalName == "content")?.Value;
            if (string.IsNullOrEmpty(b64))
                return new CpeConsultaTicketResultado { Codigo = statusCode, Descripcion = "SUNAT no devolvió constancia para el ticket." };

            var cdr = LeerCdr(Convert.FromBase64String(b64));
            // statusCode 0 = procesó bien; 99 = terminó con errores (el detalle va en el CDR).
            return new CpeConsultaTicketResultado
            {
                Aceptado = statusCode == "0" && (cdr.Codigo == "0" || cdr.Codigo == null),
                Codigo = cdr.Codigo ?? statusCode,
                Descripcion = cdr.Descripcion,
                CdrXml = cdr.Xml,
            };
        }

        public async Task<CpeExistenciaResultado> ConsultarExistenciaAsync(string tipo, string serie, long numero)
        {
            var cuerpo = new XElement(Ser + "getStatusCdr",
                new XElement("rucComprobante", _s.RucEmisor),
                new XElement("tipoComprobante", tipo),
                new XElement("serieComprobante", serie),
                new XElement("numeroComprobante", numero));
            try
            {
                var doc = await LlamarAsync("getStatusCdr", cuerpo, _s.UrlConsultaActiva);
                var codigo = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "statusCode")?.Value;
                var mensaje = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "statusMessage")?.Value;
                // 0001 aceptado, 0002 rechazado, 0003 dado de baja: en los tres casos el número ya está usado en SUNAT.
                return new CpeExistenciaResultado { Existe = codigo is "0001" or "0002" or "0003", Codigo = codigo, Mensaje = mensaje };
            }
            catch (SunatFaultException ex)
            {
                // Un fault de "no existe" significa número libre; cualquier otro se informa y quien llama decide.
                return new CpeExistenciaResultado { Existe = false, Codigo = ex.Codigo, Mensaje = ex.Message };
            }
        }

        // ── SOAP ────────────────────────────────────────────────────────────────

        private static XElement Cuerpo(string operacion, string nombreSinExtension, string xml)
        {
            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                var entry = zip.CreateEntry(nombreSinExtension + ".xml");
                using var es = entry.Open();
                var bytes = new UTF8Encoding(false).GetBytes(xml);
                es.Write(bytes, 0, bytes.Length);
            }
            return new XElement(Ser + operacion,
                new XElement("fileName", nombreSinExtension + ".zip"),
                new XElement("contentFile", Convert.ToBase64String(ms.ToArray())));
        }

        private async Task<XDocument> LlamarAsync(string operacion, XElement cuerpo, string? url = null)
        {
            // Guardia de seguridad: Producción solo si el ambiente lo declara Y el candado está abierto.
            if (_s.EsProduccion && !_s.PermitirProduccion)
                throw new InvalidOperationException("SunatCpe: el ambiente Producción está bloqueado. Para emitir de verdad, pon SunatCpe:PermitirProduccion = true.");

            var sobre = new XDocument(new XElement(Soap + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soapenv", Soap),
                new XAttribute(XNamespace.Xmlns + "ser", Ser),
                new XAttribute(XNamespace.Xmlns + "wsse", Wsse),
                new XElement(Soap + "Header",
                    new XElement(Wsse + "Security",
                        new XElement(Wsse + "UsernameToken",
                            new XElement(Wsse + "Username", _s.UsuarioWs),
                            new XElement(Wsse + "Password", _s.ClaveWs)))),
                new XElement(Soap + "Body", cuerpo)));

            using var req = new HttpRequestMessage(HttpMethod.Post, url ?? _s.UrlActiva)
            {
                Content = new StringContent(sobre.ToString(SaveOptions.DisableFormatting), Encoding.UTF8, "text/xml"),
            };
            req.Headers.Add("SOAPAction", "");
            using var resp = await _http.SendAsync(req);
            var texto = await resp.Content.ReadAsStringAsync();

            XDocument? doc = null;
            try { doc = XDocument.Parse(texto); } catch { /* respuesta que no es XML: se trata abajo */ }

            var fault = doc?.Descendants().FirstOrDefault(e => e.Name.LocalName == "Fault");
            if (fault != null)
            {
                var codigo = fault.Descendants().FirstOrDefault(e => e.Name.LocalName == "faultcode")?.Value ?? "";
                var mensaje = fault.Descendants().FirstOrDefault(e => e.Name.LocalName == "faultstring")?.Value ?? "Error de SUNAT.";
                // faultcode típico: "soap-env:Client.0111" → se conserva solo el número.
                var numero = codigo.Contains('.') ? codigo[(codigo.LastIndexOf('.') + 1)..] : codigo;
                throw new SunatFaultException(numero, mensaje);
            }

            if ((int)resp.StatusCode == 401)
                throw new InvalidOperationException("SUNAT rechazó las credenciales (HTTP 401): revisa el RUC, el usuario SOL y la clave en SunatCpe.");
            if (!resp.IsSuccessStatusCode || doc == null)
                throw new InvalidOperationException($"SUNAT respondió HTTP {(int)resp.StatusCode} en {operacion}.");
            return doc;
        }

        /// <summary>Abre el ZIP del CDR y extrae código y descripción de la respuesta (ApplicationResponse).</summary>
        private static (string? Codigo, string? Descripcion, string Xml) LeerCdr(byte[] zipBytes)
        {
            using var zip = new ZipArchive(new MemoryStream(zipBytes), ZipArchiveMode.Read);
            var entry = zip.Entries.FirstOrDefault(e => e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("El CDR de SUNAT no contiene un XML.");
            using var sr = new StreamReader(entry.Open(), Encoding.UTF8);
            var xml = sr.ReadToEnd();
            var doc = XDocument.Parse(xml);
            var respuesta = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "DocumentResponse")
                ?.Descendants().FirstOrDefault(e => e.Name.LocalName == "Response");
            var codigo = respuesta?.Descendants().FirstOrDefault(e => e.Name.LocalName == "ResponseCode")?.Value;
            var descripcion = respuesta?.Descendants().FirstOrDefault(e => e.Name.LocalName == "Description")?.Value;
            return (codigo, descripcion, xml);
        }
    }
}

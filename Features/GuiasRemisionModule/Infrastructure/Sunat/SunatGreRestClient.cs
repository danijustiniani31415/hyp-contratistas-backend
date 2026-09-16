using System.IO.Compression;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Abril_Backend.Features.GuiasRemisionModule.Infrastructure.Sunat
{
    /// <summary>
    /// Cliente de la plataforma nueva de SUNAT para GRE: API REST + OAuth2 (credenciales API del
    /// portal SOL), ticket asíncrono — reemplaza al viejo billService SOAP. La firma XMLDSig del
    /// XML sigue siendo obligatoria (eso no cambia); lo que cambia es el transporte.
    ///
    /// [PENDIENTE CALIBRAR CONTRA BETA] Basado en la especificación OpenAPI pública de esta API
    /// (proyecto Greenter, ampliamente usado en Perú) — la primera prueba real contra
    /// api-cpe-test.sunat.gob.pe puede revelar algún detalle distinto (nombres de campo, formato
    /// de error) que se ajusta según la respuesta real.
    /// </summary>
    public class SunatGreRestClient : ISunatGreClient
    {
        private readonly HttpClient _http;
        private readonly SunatGreSettings _settings;

        public SunatGreRestClient(HttpClient http, IOptions<SunatGreSettings> settings)
        {
            _http = http;
            _settings = settings.Value;
        }

        private async Task<string> ObtenerTokenAsync()
        {
            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                // El scope tiene que coincidir con el host al que realmente se va a llamar
                // (api-cpe-test vs api-cpe) — un token pedido con el scope de producción sale
                // "invalid_token" al usarlo contra el host de pruebas, y viceversa.
                ["scope"] = _settings.Ambiente == "Produccion" ? "https://api-cpe.sunat.gob.pe" : "https://api-cpe-test.sunat.gob.pe",
                ["client_id"] = _settings.ClientId,
                ["client_secret"] = _settings.ClientSecret,
                ["username"] = $"{_settings.Ruc}{_settings.SolUsuario}",
                ["password"] = _settings.SolClave,
            };

            using var response = await _http.PostAsync(_settings.AuthUrl, new FormUrlEncodedContent(form));
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"SUNAT rechazó la autenticación OAuth2 ({(int)response.StatusCode}): {body}");

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("access_token").GetString()
                ?? throw new InvalidOperationException("La respuesta de autenticación de SUNAT no trajo access_token.");
        }

        public async Task<SunatGreEnvioResultado> EnviarAsync(string xmlFirmado, string nombreArchivoSinExtension)
        {
            var zipBytes = ZipearXml(xmlFirmado, nombreArchivoSinExtension);
            var hashZip = Convert.ToHexString(SHA256.HashData(zipBytes)).ToLowerInvariant();

            var token = await ObtenerTokenAsync();

            var payload = new
            {
                archivo = new
                {
                    nomArchivo = $"{nombreArchivoSinExtension}.zip",
                    arcGreZip = Convert.ToBase64String(zipBytes),
                    hashZip,
                },
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_settings.ApiBaseActivo}/contribuyente/gem/comprobantes/{nombreArchivoSinExtension}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(payload);

            using var response = await _http.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Los headers de la respuesta a veces traen más detalle que el body genérico
                // (ej. WWW-Authenticate con la razón exacta del rechazo) — se incluyen para diagnosticar.
                var headers = string.Join(" | ", response.Headers
                    .Concat(response.Content.Headers)
                    .Select(h => $"{h.Key}: {string.Join(",", h.Value)}"));
                return new SunatGreEnvioResultado
                {
                    Enviado = false,
                    ErrorMensaje = $"SUNAT rechazó el envío ({(int)response.StatusCode}): {body} — Headers: {headers}",
                };
            }

            using var doc = JsonDocument.Parse(body);
            var numTicket = doc.RootElement.TryGetProperty("numTicket", out var t) ? t.GetString() : null;
            return new SunatGreEnvioResultado { Enviado = true, NumTicket = numTicket };
        }

        public async Task<SunatGreConsultaResultado> ConsultarEstadoAsync(string numTicket)
        {
            var token = await ObtenerTokenAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{_settings.ApiBaseActivo}/contribuyente/gem/comprobantes/envios/{numTicket}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            using var response = await _http.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"SUNAT rechazó la consulta de estado ({(int)response.StatusCode}): {body}");

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var cdrGenerado = root.TryGetProperty("indCdrGenerado", out var ind) && ind.GetBoolean();
            if (!cdrGenerado)
                return new SunatGreConsultaResultado { CdrGenerado = false };

            var arcCdr = root.TryGetProperty("arcCdr", out var cdrProp) ? cdrProp.GetString() : null;
            if (string.IsNullOrWhiteSpace(arcCdr))
                return new SunatGreConsultaResultado { CdrGenerado = false };

            var (codigo, descripcion) = LeerCdr(Convert.FromBase64String(arcCdr));
            return new SunatGreConsultaResultado
            {
                CdrGenerado = true,
                Aceptado = codigo == "0",
                CodigoRespuesta = codigo,
                Descripcion = descripcion,
            };
        }

        private static byte[] ZipearXml(string xmlFirmado, string nombreArchivoSinExtension)
        {
            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = zip.CreateEntry($"{nombreArchivoSinExtension}.xml", CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream, new UTF8Encoding(false));
                writer.Write(xmlFirmado);
            }
            return ms.ToArray();
        }

        private static (string? codigo, string? descripcion) LeerCdr(byte[] cdrZipBytes)
        {
            using var ms = new MemoryStream(cdrZipBytes);
            using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
            var entry = zip.Entries.FirstOrDefault(e => e.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("El ZIP de respuesta (CDR) de SUNAT no contiene un XML.");

            using var reader = new StreamReader(entry.Open());
            var cdrXml = System.Xml.Linq.XDocument.Parse(reader.ReadToEnd());

            var codigo = cdrXml.Descendants().FirstOrDefault(e => e.Name.LocalName == "ResponseCode")?.Value;
            var descripcion = cdrXml.Descendants().FirstOrDefault(e => e.Name.LocalName == "Description")?.Value;
            return (codigo, descripcion);
        }
    }
}

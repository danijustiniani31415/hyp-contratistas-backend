using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.ComprasModule.Application.Dtos;
using Abril_Backend.Features.ComprasModule.Application.Interfaces;

namespace Abril_Backend.Features.ComprasModule.Application.Services
{
    /// <summary>
    /// Lectura de comprobantes con Gemini (API REST, salida JSON con esquema). Configuración:
    /// Gemini:ApiKey, Gemini:ModeloRapido (barato, PDFs limpios) y Gemini:ModeloPreciso (fotos y casos dudosos).
    /// </summary>
    public class GeminiComprobanteLectorService : IComprobanteLectorService
    {
        private static readonly JsonSerializerOptions Json = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly HttpClient _http;
        private readonly string? _apiKey;

        public GeminiComprobanteLectorService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _apiKey = config["Gemini:ApiKey"];
            Modelos = (
                config["Gemini:ModeloRapido"] ?? "gemini-2.5-flash-lite",
                config["Gemini:ModeloPreciso"] ?? "gemini-2.5-flash");
        }

        public (string Rapido, string Preciso) Modelos { get; }

        private const string Prompt =
            "Eres un asistente contable en Perú. Lee este comprobante de compra (factura, boleta, nota de crédito, " +
            "recibo, ticket o guía de remisión; puede ser PDF o foto) y devuelve SOLO los datos del documento.\n" +
            "Reglas:\n" +
            "- Copia los números tal como están impresos. Si algo es ilegible, no lo inventes: déjalo vacío o en 0 y baja la confianza.\n" +
            "- EMISOR es quien vende (su RUC aparece arriba, junto al número del comprobante). CLIENTE es quien compra.\n" +
            "- serie y numero van separados (ej. E001-196 → serie E001, numero 196; F283-0000413 → serie F283, numero 413).\n" +
            "- tipo: FACTURA, BOLETA, NOTA_CREDITO, NOTA_DEBITO, RECIBO_HONORARIOS, TICKET, GUIA_REMISION, INVOICE_EXTRANJERO u OTRO.\n" +
            "- moneda: PEN (soles) o USD (dólares).\n" +
            "- subtotal es el valor de venta SIN IGV; igv es el IGV; total es el importe total a pagar.\n" +
            "- Cada línea: descripcion completa, cantidad, unidad (UNIDAD, KG, PAR, CAJA…), precioUnitario SIN IGV si se distingue, total de la línea.\n" +
            "- categoria de cada línea: BIEN (material, herramienta o EPP que se guarda en almacén), SERVICIO (reparación, alquiler, mano de obra), " +
            "PASAJE_VIATICO (pasajes, hospedaje, alimentación, peajes), ACTIVO (equipo o maquinaria de alto valor) u OTRO.\n" +
            "- Si hay detracción, devuelve su monto en soles y su porcentaje.\n" +
            "- ordenCompraRef y guiaRef: solo si el documento los menciona.\n" +
            "- confianza: de 0 a 1, qué tan seguro estás de haber leído bien TODAS las cifras.";

        private static object Esquema()
        {
            static object S(bool nullable = true) => new { type = "STRING", nullable };
            static object N(bool nullable = false) => new { type = "NUMBER", nullable };
            return new
            {
                type = "OBJECT",
                properties = new Dictionary<string, object>
                {
                    ["tipo"] = new { type = "STRING" },
                    ["emisorRuc"] = S(),
                    ["emisorRazonSocial"] = S(),
                    ["clienteRuc"] = S(),
                    ["clienteRazonSocial"] = S(),
                    ["serie"] = S(),
                    ["numero"] = S(),
                    ["fechaEmision"] = S(),
                    ["fechaVencimiento"] = S(),
                    ["moneda"] = new { type = "STRING" },
                    ["formaPago"] = S(),
                    ["subtotal"] = N(),
                    ["igv"] = N(),
                    ["total"] = N(),
                    ["detraccionMonto"] = N(true),
                    ["detraccionPorcentaje"] = N(true),
                    ["ordenCompraRef"] = S(),
                    ["guiaRef"] = S(),
                    ["observacion"] = S(),
                    ["confianza"] = N(),
                    ["items"] = new
                    {
                        type = "ARRAY",
                        items = new
                        {
                            type = "OBJECT",
                            properties = new Dictionary<string, object>
                            {
                                ["descripcion"] = new { type = "STRING" },
                                ["cantidad"] = N(),
                                ["unidad"] = S(),
                                ["precioUnitario"] = N(),
                                ["total"] = N(),
                                ["categoria"] = new { type = "STRING" },
                            },
                            required = new[] { "descripcion", "cantidad", "total", "categoria" },
                        },
                    },
                },
                required = new[] { "tipo", "moneda", "subtotal", "igv", "total", "confianza", "items" },
            };
        }

        public async Task<ComprobanteLeidoDto> Leer(byte[] contenido, string mimeType, string modelo)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new AbrilException("La lectura con IA no está configurada: falta la clave de Gemini en el servidor.", 503);

            var cuerpo = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { inline_data = new { mime_type = mimeType, data = Convert.ToBase64String(contenido) } },
                            new { text = Prompt },
                        },
                    },
                },
                generationConfig = new
                {
                    temperature = 0,
                    responseMimeType = "application/json",
                    responseSchema = Esquema(),
                },
            };

            using var req = new HttpRequestMessage(HttpMethod.Post,
                $"https://generativelanguage.googleapis.com/v1beta/models/{modelo}:generateContent")
            {
                Content = JsonContent.Create(cuerpo),
            };
            req.Headers.Add("x-goog-api-key", _apiKey);

            using var resp = await _http.SendAsync(req);
            var texto = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new AbrilException(resp.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                    ? "El servicio de lectura con IA está saturado. Intenta de nuevo en un minuto."
                    : "No se pudo leer el documento con IA. Intenta de nuevo o ingrésalo a mano.", 502);

            var salida = JsonNode.Parse(texto)?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(salida))
                throw new AbrilException("La IA no pudo leer este documento. Prueba con una foto más nítida o un PDF.", 422);

            var lectura = JsonSerializer.Deserialize<ComprobanteLeidoDto>(salida, Json)
                ?? throw new AbrilException("La IA devolvió una lectura vacía.", 422);
            return Normalizar(lectura);
        }

        /// <summary>Deja lo leído en el formato que usa el sistema (RUC solo dígitos, serie en mayúsculas, número sin ceros a la izquierda).</summary>
        private static ComprobanteLeidoDto Normalizar(ComprobanteLeidoDto l)
        {
            static string? Digitos(string? s) => s == null ? null : new string(s.Where(char.IsDigit).ToArray());
            l.EmisorRuc = Digitos(l.EmisorRuc);
            l.ClienteRuc = Digitos(l.ClienteRuc);
            l.Serie = l.Serie?.Trim().ToUpperInvariant();
            l.Numero = l.Numero == null ? null : (l.Numero.Trim().TrimStart('0') is { Length: > 0 } n ? n : l.Numero.Trim());
            l.Moneda = l.Moneda?.ToUpperInvariant() == "USD" ? "USD" : "PEN";
            l.Tipo = (l.Tipo ?? "OTRO").Trim().ToUpperInvariant();
            l.Confianza = Math.Clamp(l.Confianza, 0, 1);
            foreach (var it in l.Items)
            {
                it.Descripcion = it.Descripcion.Trim();
                it.Categoria = (it.Categoria ?? "OTRO").Trim().ToUpperInvariant();
            }
            return l;
        }
    }
}

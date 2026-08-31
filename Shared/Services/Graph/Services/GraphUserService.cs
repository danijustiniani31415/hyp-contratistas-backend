using Abril_Backend.Shared.Services.Graph.Dtos;
using Abril_Backend.Shared.Services.Graph.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Abril_Backend.Shared.Services.Graph.Services
{
    public class GraphUserService : IGraphUserService, IEmailGroupResolver, IUserPhotoService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private const int ChunkSize = 15; // límite del operador 'in' en Graph API

        public GraphUserService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<Dictionary<string, GraphUserProfileDto>> GetUsersByEmailsAsync(List<string> emails)
        {
            if (emails == null || emails.Count == 0)
                return new Dictionary<string, GraphUserProfileDto>();

            var appToken = await GetAppTokenAsync();
            if (string.IsNullOrEmpty(appToken))
            {
                Console.WriteLine("[GraphUserService] No se pudo obtener el token de aplicación.");
                return new Dictionary<string, GraphUserProfileDto>();
            }

            var result = new Dictionary<string, GraphUserProfileDto>(StringComparer.OrdinalIgnoreCase);

            var client = BuildClient(appToken);

            foreach (var chunk in emails.Where(e => !string.IsNullOrWhiteSpace(e))
                                        .Distinct(StringComparer.OrdinalIgnoreCase)
                                        .Chunk(ChunkSize))
            {
                try
                {
                    foreach (var profile in await QueryUsersChunkAsync(client, chunk))
                        result[profile.Mail] = profile;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GraphUserService] EXCEPCIÓN en chunk: {ex.Message}");
                }
            }

            return result;
        }

        public async Task<List<GraphUserProfileDto>> GetResolvedProfilesAsync(List<string> emails)
        {
            if (emails == null || emails.Count == 0)
                return new List<GraphUserProfileDto>();

            var appToken = await GetAppTokenAsync();
            if (string.IsNullOrEmpty(appToken))
            {
                Console.WriteLine("[GraphUserService] No se pudo obtener el token de aplicación.");
                return new List<GraphUserProfileDto>();
            }

            var result    = new List<GraphUserProfileDto>();
            var foundMails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var client = BuildClient(appToken);

            var distinctEmails = emails
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Fase 1: consultar como usuarios individuales en chunks
            foreach (var chunk in distinctEmails.Chunk(ChunkSize))
            {
                try
                {
                    foreach (var profile in await QueryUsersChunkAsync(client, chunk))
                    {
                        if (foundMails.Add(profile.Mail))
                            result.Add(profile);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GraphUserService] EXCEPCIÓN en chunk users: {ex.Message}");
                }
            }

            // Fase 2: emails no resueltos → intentar como grupo y expandir miembros
            var unresolved = distinctEmails.Where(e => !foundMails.Contains(e)).ToList();
            foreach (var email in unresolved)
            {
                try
                {
                    var members = await TryExpandGroupAsync(client, email);
                    if (members is null) continue;   // no era un grupo

                    foreach (var profile in members)
                    {
                        if (foundMails.Add(profile.Mail))
                            result.Add(profile);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GraphUserService] EXCEPCIÓN expandiendo grupo '{email}': {ex.Message}");
                }
            }

            return result;
        }

        public async Task<List<string>> ExpandAsync(IEnumerable<string> emails)
        {
            var distinctEmails = (emails ?? Enumerable.Empty<string>())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (distinctEmails.Count == 0)
                return new List<string>();

            var appToken = await GetAppTokenAsync();
            if (string.IsNullOrEmpty(appToken))
            {
                // Sin token de app no podemos detectar grupos. Para no perder destinatarios,
                // se devuelven los correos tal cual (pass-through total). Si alguno era un grupo
                // que el proveedor de correo no sabe entregar, fallará solo ese envío.
                Console.WriteLine("[EmailGroupResolver] No se pudo obtener token de app; pass-through sin expandir.");
                return distinctEmails;
            }

            var client = BuildClient(appToken);
            var result = new List<string>();
            var seen   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var email in distinctEmails)
            {
                try
                {
                    var members = await TryExpandGroupAsync(client, email);

                    if (members is not null)
                    {
                        // Es un grupo → desglosar en los correos de sus miembros
                        foreach (var m in members)
                            if (!string.IsNullOrWhiteSpace(m.Mail) && seen.Add(m.Mail))
                                result.Add(m.Mail);
                    }
                    else if (seen.Add(email))
                    {
                        // No es un grupo → conservar el correo tal cual (pass-through)
                        result.Add(email);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EmailGroupResolver] Error resolviendo '{email}': {ex.Message}. Se conserva el correo tal cual.");
                    if (seen.Add(email))
                        result.Add(email);
                }
            }

            return result;
        }

        /// <summary>
        /// Si <paramref name="email"/> corresponde a un grupo (mail-enabled), devuelve la lista de
        /// perfiles de sus miembros usuario (excluye sub-grupos anidados). Devuelve <c>null</c> si el
        /// correo NO corresponde a ningún grupo (es usuario, externo o no existe). Una lista vacía
        /// significa que SÍ es un grupo pero no se pudieron leer/parsear sus miembros.
        /// </summary>
        private static async Task<List<GraphUserProfileDto>?> TryExpandGroupAsync(HttpClient client, string email)
        {
            // Buscar el grupo por email
            var groupUrl = $"https://graph.microsoft.com/v1.0/groups" +
                           $"?$filter=mail eq '{email}'" +
                           $"&$select=id,displayName" +
                           $"&$count=true";

            var groupResponse = await client.GetAsync(groupUrl);
            if (!groupResponse.IsSuccessStatusCode) return null;

            var groupJson = await groupResponse.Content.ReadAsStringAsync();
            using var groupDoc = JsonDocument.Parse(groupJson);

            if (!groupDoc.RootElement.TryGetProperty("value", out var groupArray)) return null;

            var firstGroup = groupArray.EnumerateArray().FirstOrDefault();
            if (firstGroup.ValueKind == JsonValueKind.Undefined) return null;
            if (!firstGroup.TryGetProperty("id", out var groupIdProp)) return null;

            var groupId = groupIdProp.GetString();
            if (string.IsNullOrEmpty(groupId)) return null;

            Console.WriteLine($"[GraphUserService] '{email}' es un grupo ({groupId}), expandiendo miembros...");

            var members = new List<GraphUserProfileDto>();

            // Obtener miembros del grupo (solo usuarios, no sub-grupos)
            var membersUrl = $"https://graph.microsoft.com/v1.0/groups/{groupId}/members" +
                             $"?$select=displayName,mail,jobTitle,mobilePhone,businessPhones";

            var membersResponse = await client.GetAsync(membersUrl);
            if (!membersResponse.IsSuccessStatusCode) return members;

            var membersJson = await membersResponse.Content.ReadAsStringAsync();
            using var membersDoc = JsonDocument.Parse(membersJson);

            if (!membersDoc.RootElement.TryGetProperty("value", out var membersArray)) return members;

            foreach (var member in membersArray.EnumerateArray())
            {
                // Saltar sub-grupos anidados (su @odata.type contiene "group")
                if (member.TryGetProperty("@odata.type", out var odataType) &&
                    odataType.GetString()?.Contains("group", StringComparison.OrdinalIgnoreCase) == true)
                    continue;

                var profile = ParseUserProfile(member);
                if (profile is null) continue;

                members.Add(profile);
            }

            return members;
        }

        public async Task<GraphUserProfileDto?> GetCurrentUserProfileAsync(string graphAccessToken)
        {
            if (string.IsNullOrEmpty(graphAccessToken))
                return null;

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", graphAccessToken);

                var response = await client.GetAsync(
                    "https://graph.microsoft.com/v1.0/me" +
                    "?$select=displayName,mail,jobTitle,mobilePhone,businessPhones");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[GraphUserService] GetCurrentUserProfileAsync error: {(int)response.StatusCode}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                return ParseUserProfile(doc.RootElement);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GraphUserService] EXCEPCIÓN en GetCurrentUserProfileAsync: {ex.Message}");
                return null;
            }
        }

        // Graph $batch admite como máximo 20 peticiones por lote.
        private const int BatchSize = 20;

        /// <summary>
        /// Descarga las fotos de perfil de varios usuarios por email (permiso de aplicación)
        /// usando Graph <c>$batch</c>: agrupa hasta 20 fotos por petición, reduciendo N round-trips
        /// a ceil(N/20). Los lotes se lanzan en paralelo (limitados por un semáforo). El binario
        /// llega ya en base64 dentro del batch, así que se arma el data URI directamente.
        /// </summary>
        public async Task<Dictionary<string, string?>> GetPhotosByEmailsAsync(List<string> emails)
        {
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            if (emails == null || emails.Count == 0)
                return result;

            var appToken = await GetAppTokenAsync();
            if (string.IsNullOrEmpty(appToken))
            {
                Console.WriteLine("[GraphUserService] No se pudo obtener el token de aplicación para fotos.");
                return result;
            }

            var distinct = emails.Where(e => !string.IsNullOrWhiteSpace(e))
                                 .Select(e => e.Trim())
                                 .Distinct(StringComparer.OrdinalIgnoreCase)
                                 .ToList();

            var client = BuildClient(appToken);
            using var gate = new SemaphoreSlim(4); // nº de lotes en paralelo

            var tasks = distinct.Chunk(BatchSize).Select(async chunk =>
            {
                await gate.WaitAsync();
                try { return await FetchPhotoBatchAsync(client, chunk); }
                finally { gate.Release(); }
            });

            foreach (var lote in await Task.WhenAll(tasks))
                foreach (var (email, foto) in lote)
                    result[email] = foto;

            return result;
        }

        /// <summary>
        /// Resuelve las fotos de un lote (≤20) con una sola llamada a <c>POST /$batch</c>.
        /// Devuelve cada email con su data URI base64 o <c>null</c> si no tiene foto.
        /// </summary>
        private static async Task<List<(string email, string? foto)>> FetchPhotoBatchAsync(HttpClient client, string[] chunk)
        {
            var salida = new List<(string, string?)>(chunk.Length);

            try
            {
                var requests = chunk.Select((email, i) => new
                {
                    id = i.ToString(),
                    method = "GET",
                    url = $"/users/{Uri.EscapeDataString(email)}/photo/$value",
                });

                var payload = JsonSerializer.Serialize(new { requests });
                using var content = new StringContent(payload, Encoding.UTF8, "application/json");

                var resp = await client.PostAsync("https://graph.microsoft.com/v1.0/$batch", content);
                var json = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[GraphUserService] $batch de fotos falló: {(int)resp.StatusCode}");
                    foreach (var email in chunk) salida.Add((email, null));
                    return salida;
                }

                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("responses", out var responses))
                {
                    foreach (var email in chunk) salida.Add((email, null));
                    return salida;
                }

                // Las respuestas pueden venir desordenadas → se mapean por 'id' (índice del chunk).
                var porEmail = new Dictionary<string, string?>();
                foreach (var r in responses.EnumerateArray())
                {
                    if (!r.TryGetProperty("id", out var idProp)) continue;
                    if (!int.TryParse(idProp.GetString(), out var idx) || idx < 0 || idx >= chunk.Length) continue;

                    var email = chunk[idx];
                    var status = r.TryGetProperty("status", out var stProp) ? stProp.GetInt32() : 0;

                    if (status != 200 ||
                        !r.TryGetProperty("body", out var bodyProp) ||
                        bodyProp.ValueKind != JsonValueKind.String)
                    {
                        porEmail[email] = null;
                        continue;
                    }

                    var base64 = bodyProp.GetString();
                    if (string.IsNullOrEmpty(base64))
                    {
                        porEmail[email] = null;
                        continue;
                    }

                    porEmail[email] = $"data:{ContentTypeDe(r)};base64,{base64}";
                }

                foreach (var email in chunk)
                    salida.Add((email, porEmail.TryGetValue(email, out var f) ? f : null));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GraphUserService] EXCEPCIÓN en $batch de fotos: {ex.Message}");
                salida.Clear();
                foreach (var email in chunk) salida.Add((email, null));
            }

            return salida;
        }

        /// <summary>Lee el Content-Type de una respuesta de $batch (headers, case-insensitive).</summary>
        private static string ContentTypeDe(JsonElement respuesta)
        {
            if (respuesta.TryGetProperty("headers", out var headers) &&
                headers.ValueKind == JsonValueKind.Object)
            {
                foreach (var h in headers.EnumerateObject())
                {
                    if (string.Equals(h.Name, "Content-Type", StringComparison.OrdinalIgnoreCase) &&
                        h.Value.ValueKind == JsonValueKind.String)
                    {
                        var ct = h.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(ct)) return ct!;
                    }
                }
            }
            return "image/jpeg";
        }

        // ── Helpers privados ─────────────────────────────────────────────────

        private HttpClient BuildClient(string appToken)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", appToken);
            client.DefaultRequestHeaders.Add("ConsistencyLevel", "eventual");
            return client;
        }

        private static async Task<List<GraphUserProfileDto>> QueryUsersChunkAsync(HttpClient client, string[] chunk)
        {
            var filterValues = string.Join(",", chunk.Select(e => $"'{e}'"));
            var url = $"https://graph.microsoft.com/v1.0/users" +
                      $"?$filter=mail in ({filterValues})" +
                      $"&$select=displayName,mail,jobTitle,mobilePhone,businessPhones" +
                      $"&$count=true";

            Console.WriteLine($"[GraphUserService] URL: {url}");

            var response = await client.GetAsync(url);
            var json     = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"[GraphUserService] Status: {(int)response.StatusCode} {response.StatusCode}");

            if (!response.IsSuccessStatusCode) return new List<GraphUserProfileDto>();

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("value", out var valueArray))
                return new List<GraphUserProfileDto>();

            var result = new List<GraphUserProfileDto>();
            foreach (var user in valueArray.EnumerateArray())
            {
                var profile = ParseUserProfile(user);
                if (profile is not null) result.Add(profile);
            }
            return result;
        }

        private static GraphUserProfileDto? ParseUserProfile(JsonElement user)
        {
            var mail = user.TryGetProperty("mail", out var mailProp) ? mailProp.GetString() : null;
            if (string.IsNullOrWhiteSpace(mail)) return null;

            var displayName = user.TryGetProperty("displayName", out var dnProp) ? dnProp.GetString() : null;
            var jobTitle    = user.TryGetProperty("jobTitle",    out var jtProp) ? jtProp.GetString() : null;

            string? phone = null;
            if (user.TryGetProperty("mobilePhone", out var mobileProp) && mobileProp.ValueKind != JsonValueKind.Null)
                phone = mobileProp.GetString();

            if (string.IsNullOrWhiteSpace(phone) &&
                user.TryGetProperty("businessPhones", out var bpProp) &&
                bpProp.ValueKind == JsonValueKind.Array)
            {
                var firstPhone = bpProp.EnumerateArray().FirstOrDefault();
                if (firstPhone.ValueKind == JsonValueKind.String)
                    phone = firstPhone.GetString();
            }

            return new GraphUserProfileDto
            {
                Mail        = mail,
                DisplayName = displayName,
                JobTitle    = jobTitle,
                Phone       = phone
            };
        }

        /// <summary>
        /// Obtiene un token de acceso usando el flujo client_credentials (permiso de aplicación).
        /// Requiere AzureAd:TenantId, AzureAd:ClientId y AzureAd:ClientSecret en la configuración.
        /// </summary>
        private async Task<string?> GetAppTokenAsync()
        {
            var tenantId = _configuration["AzureAd:TenantId"];
            var clientId = _configuration["AzureAd:ClientId"];
            var clientSecret = _configuration["AzureAd:ClientSecret"];

            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                Console.WriteLine("[GraphUserService] Faltan AzureAd:TenantId, AzureAd:ClientId o AzureAd:ClientSecret en la configuración.");
                return null;
            }

            var tokenUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

            var body = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("scope", "https://graph.microsoft.com/.default"),
            });

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsync(tokenUrl, body);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[GraphUserService] Error al obtener token de app: {response.StatusCode} - {json}");
                    return null;
                }

                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.TryGetProperty("access_token", out var tokenProp)
                    ? tokenProp.GetString()
                    : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GraphUserService] EXCEPCIÓN al obtener token de app: {ex.Message}");
                return null;
            }
        }
    }
}

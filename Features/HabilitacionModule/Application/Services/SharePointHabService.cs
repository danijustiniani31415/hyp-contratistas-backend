using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Abril_Backend.Features.Habilitacion.Application.Services
{
    public class SharePointHabService : ISharePointHabService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SharePointHabService> _logger;

        // keyed by libraryId (o "default" para el drive predeterminado del sitio)
        private readonly ConcurrentDictionary<string, string> _driveIdCache = new();
        private string? _cachedToken;
        private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;

        private readonly Lazy<HttpClient> _noRedirectClient = new(() =>
            new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }));

        public SharePointHabService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<SharePointHabService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string?> GetDownloadUrlAsync(string archivoUrl, string? libraryContexto = null)
        {
            var trimmed = archivoUrl?.Trim() ?? string.Empty;
            if (trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("GetDownloadUrlAsync: URL absoluta detectada, devolviendo como está.");
                return trimmed;
            }

            var siteId = ResolverSiteId(libraryContexto ?? archivoUrl ?? string.Empty);
            if (string.IsNullOrWhiteSpace(siteId)) return null;

            var token = await GetAccessTokenAsync();
            if (string.IsNullOrWhiteSpace(token)) return null;

            // Dossier paths: {contributorId}/{proyectoId}/Sem{n}_{yyyyMMdd}/archivo — stored in Documentos drive
            if (System.Text.RegularExpressions.Regex.IsMatch(archivoUrl ?? string.Empty,
                @"^\d+/\d+/Sem\d+_\d{8}/", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                const string driveIdDoc = "b!Bmji2TXVU0OWEBlZeOIDkC8Dt6ceUVNLiodQihkLPHxZH7QqINghTq0UWOH5DOFR";
                var encodedDoc = Uri.EscapeDataString(archivoUrl!).Replace("%2F", "/");
                var urlDoc = $"https://graph.microsoft.com/v1.0/sites/{siteId}/drives/{driveIdDoc}/root:/{encodedDoc}:/content";

                var clientDoc = _noRedirectClient.Value;
                clientDoc.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var responseDoc = await clientDoc.GetAsync(urlDoc);
                if (responseDoc.StatusCode == System.Net.HttpStatusCode.Found ||
                    responseDoc.StatusCode == System.Net.HttpStatusCode.MovedPermanently)
                {
                    var locationDoc = responseDoc.Headers.Location?.ToString();
                    if (!string.IsNullOrWhiteSpace(locationDoc)) return locationDoc;
                }
                _logger.LogWarning("SharePoint dossier GET falló ({Status}) para {Path}", responseDoc.StatusCode, archivoUrl);
                return null;
            }

            if ((archivoUrl ?? string.Empty).StartsWith("OPT/", StringComparison.OrdinalIgnoreCase))
            {
                const string driveIdOpt = "b!Bmji2TXVU0OWEBlZeOIDkC8Dt6ceUVNLiodQihkLPHxZH7QqINghTq0UWOH5DOFR";
                var encodedOpt = Uri.EscapeDataString(archivoUrl!).Replace("%2F", "/");
                var urlOpt = $"https://graph.microsoft.com/v1.0/sites/{siteId}/drives/{driveIdOpt}/root:/{encodedOpt}:/content";

                var clientOpt = _noRedirectClient.Value;
                clientOpt.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                var responseOpt = await clientOpt.GetAsync(urlOpt);

                if (responseOpt.StatusCode == System.Net.HttpStatusCode.Found ||
                    responseOpt.StatusCode == System.Net.HttpStatusCode.MovedPermanently)
                {
                    var locationOpt = responseOpt.Headers.Location?.ToString();
                    if (!string.IsNullOrWhiteSpace(locationOpt))
                        return locationOpt;

                    _logger.LogWarning("SharePoint /content devolvió redirect sin Location para {Path}", archivoUrl);
                    return null;
                }

                _logger.LogWarning("SharePoint /content GET falló ({Status}) para {Path}", responseOpt.StatusCode, archivoUrl);
                return null;
            }

            var libraryId = ResolverLibraryId(libraryContexto ?? archivoUrl ?? string.Empty);
            string? driveId;
            string path;

            if (libraryId != null)
            {
                driveId = await GetDriveIdAsync(siteId, token, libraryId);
                path = NormalizarPath(archivoUrl!);
            }
            else
            {
                // Extraer primer segmento como nombre de librería (ej: "PETSAbril2026/archivo.docx" → "PETSAbril2026")
                var normalizado = (archivoUrl ?? string.Empty).Trim().TrimStart('/');
                var slashIdx = normalizado.IndexOf('/');
                var libraryName = slashIdx > 0 ? normalizado[..slashIdx] : normalizado;
                var pathDentro   = slashIdx > 0 ? normalizado[(slashIdx + 1)..] : string.Empty;

                if (string.IsNullOrWhiteSpace(libraryName))
                {
                    _logger.LogWarning("GetDownloadUrlAsync: no se pudo extraer nombre de librería de '{Path}'", archivoUrl);
                    return null;
                }

                driveId = await GetDriveIdByNameAsync(siteId, token, libraryName);
                path = pathDentro;
            }

            if (string.IsNullOrWhiteSpace(driveId)) return null;
            if (string.IsNullOrWhiteSpace(path)) return null;

            var encoded = Uri.EscapeDataString(path).Replace("%2F", "/");
            var url = $"https://graph.microsoft.com/v1.0/sites/{siteId}/drives/{driveId}/root:/{encoded}:/content";

            var client = _noRedirectClient.Value;
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.Found ||
                response.StatusCode == System.Net.HttpStatusCode.MovedPermanently)
            {
                var location = response.Headers.Location?.ToString();
                if (!string.IsNullOrWhiteSpace(location))
                    return location;

                _logger.LogWarning("SharePoint /content devolvió redirect sin Location para {Path}", path);
                return null;
            }

            _logger.LogWarning("SharePoint /content GET falló ({Status}) para {Path}", response.StatusCode, path);
            return null;
        }

        public async Task<string> SubirArchivoAsync(Stream fileStream, string fileName, string contexto)
        {
            var siteId = ResolverSiteId(contexto);
            if (string.IsNullOrWhiteSpace(siteId))
                throw new AbrilException("SharePoint no está configurado.", 500);

            var token = await GetAccessTokenAsync()
                ?? throw new AbrilException("No se pudo obtener token de Microsoft Graph.", 500);

            var libraryId = ResolverLibraryId(contexto);
            var driveId = await GetDriveIdAsync(siteId, token, libraryId)
                ?? throw new AbrilException("No se pudo resolver el drive de SharePoint.", 500);

            var contextoLimpio = (contexto ?? string.Empty).Trim().Trim('/');
            if (contextoLimpio.StartsWith("habilitacion/", StringComparison.OrdinalIgnoreCase))
                contextoLimpio = contextoLimpio["habilitacion/".Length..].TrimStart('/');
            var fechaPrefix = DateTime.UtcNow.ToString("yyyyMMdd");
            var fileNameLimpio = SanitizarNombreArchivo(fileName);

            var path = string.IsNullOrEmpty(contextoLimpio)
                ? $"habilitacion/{fechaPrefix}_{fileNameLimpio}"
                : $"habilitacion/{contextoLimpio}/{fechaPrefix}_{fileNameLimpio}";

            var encoded = Uri.EscapeDataString(path).Replace("%2F", "/");
            var url = $"https://graph.microsoft.com/v1.0/sites/{siteId}/drives/{driveId}/root:/{encoded}:/content";

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var content = new StreamContent(fileStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            var response = await client.PutAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("SharePoint upload falló ({Status}) para {Path}: {Body}",
                    response.StatusCode, path, body);
                throw new AbrilException(
                    $"Error al subir archivo a SharePoint ({(int)response.StatusCode}).",
                    502);
            }

            return path;
        }

        // Límite documentado de Microsoft Graph para el PUT simple ".../content" (4 MB).
        // Archivos más grandes deben subirse por sesión (createUploadSession) en chunks.
        private const long GraphSimpleUploadMaxBytes = 4 * 1024 * 1024;

        public async Task<string> SubirArchivoEnRutaAsync(Stream fileStream, string fileName, string libraryContexto, string carpetaPath)
        {
            var siteId = ResolverSiteId(libraryContexto);
            if (string.IsNullOrWhiteSpace(siteId))
                throw new AbrilException("SharePoint no está configurado.", 500);

            var token = await GetAccessTokenAsync()
                ?? throw new AbrilException("No se pudo obtener token de Microsoft Graph.", 500);

            var libraryId = ResolverLibraryId(libraryContexto);
            var driveId = await GetDriveIdAsync(siteId, token, libraryId)
                ?? throw new AbrilException("No se pudo resolver el drive de SharePoint.", 500);

            var fechaPrefix = DateTime.UtcNow.ToString("yyyyMMdd");
            var fileNameLimpio = SanitizarNombreArchivo(fileName);
            var path = $"{carpetaPath.Trim('/')}/{fechaPrefix}_{fileNameLimpio}";

            var encoded = Uri.EscapeDataString(path).Replace("%2F", "/");

            long fileLength;
            try { fileLength = fileStream.Length; }
            catch (NotSupportedException) { fileLength = GraphSimpleUploadMaxBytes + 1; } // stream sin Length conocido → forzar chunked por seguridad

            if (fileLength > GraphSimpleUploadMaxBytes)
            {
                await SubirArchivoPorSesionAsync(siteId, driveId, token, encoded, fileStream, fileLength, path);
                return path;
            }

            var url = $"https://graph.microsoft.com/v1.0/sites/{siteId}/drives/{driveId}/root:/{encoded}:/content";

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var content = new StreamContent(fileStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            var response = await client.PutAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("SharePoint upload falló ({Status}) para {Path}: {Body}",
                    response.StatusCode, path, body);
                throw new AbrilException($"Error al subir archivo a SharePoint ({(int)response.StatusCode}).", 502);
            }

            return path;
        }

        /// <summary>
        /// Sube un archivo grande (&gt; 4 MB) a SharePoint usando una upload session de Microsoft Graph,
        /// en fragmentos de 5 MB (múltiplo de 320 KiB, tal como exige la API). Reintenta cada fragmento
        /// hasta 3 veces ante fallas transitorias antes de abortar.
        /// </summary>
        private async Task SubirArchivoPorSesionAsync(
            string siteId, string driveId, string token, string encodedPath,
            Stream fileStream, long fileLength, string pathParaLog)
        {
            const int chunkSize = 5 * 1024 * 1024; // 5 MB, múltiplo de 320 KiB
            const int maxIntentosPorChunk = 3;

            var client = _httpClientFactory.CreateClient();

            var createSessionUrl = $"https://graph.microsoft.com/v1.0/sites/{siteId}/drives/{driveId}/root:/{encodedPath}:/createUploadSession";
            using var sessionRequest = new HttpRequestMessage(HttpMethod.Post, createSessionUrl)
            {
                Content = JsonContent.Create(new
                {
                    item = new Dictionary<string, string> { ["@microsoft.graph.conflictBehavior"] = "replace" }
                })
            };
            sessionRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var sessionResponse = await client.SendAsync(sessionRequest);
            if (!sessionResponse.IsSuccessStatusCode)
            {
                var body = await sessionResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("No se pudo crear la upload session de SharePoint ({Status}) para {Path}: {Body}",
                    sessionResponse.StatusCode, pathParaLog, body);
                throw new AbrilException($"Error al iniciar la subida del archivo a SharePoint ({(int)sessionResponse.StatusCode}).", 502);
            }

            using var sessionDoc = JsonDocument.Parse(await sessionResponse.Content.ReadAsStringAsync());
            var uploadUrl = sessionDoc.RootElement.GetProperty("uploadUrl").GetString()
                ?? throw new AbrilException("SharePoint no devolvió una URL de subida válida.", 502);

            if (fileStream.CanSeek) fileStream.Position = 0;
            var buffer = new byte[chunkSize];
            long enviados = 0;

            try
            {
                while (enviados < fileLength)
                {
                    var tamanoChunk = (int)Math.Min(chunkSize, fileLength - enviados);
                    var leidos = await LeerBufferCompletoAsync(fileStream, buffer, tamanoChunk);
                    if (leidos != tamanoChunk)
                        throw new AbrilException("El archivo se cortó durante la lectura antes de completar la subida.", 400);

                    var rangoInicio = enviados;
                    var rangoFin = enviados + tamanoChunk - 1;

                    var exito = false;
                    Exception? ultimoError = null;

                    for (var intento = 1; intento <= maxIntentosPorChunk && !exito; intento++)
                    {
                        try
                        {
                            using var chunkContent = new ByteArrayContent(buffer, 0, tamanoChunk);
                            chunkContent.Headers.ContentLength = tamanoChunk;
                            chunkContent.Headers.ContentRange =
                                new System.Net.Http.Headers.ContentRangeHeaderValue(rangoInicio, rangoFin, fileLength);

                            // La uploadUrl de la sesión ya está pre-autorizada — no enviar Authorization aquí.
                            using var chunkResponse = await client.PutAsync(uploadUrl, chunkContent);
                            if (chunkResponse.IsSuccessStatusCode)
                            {
                                exito = true;
                            }
                            else
                            {
                                var body = await chunkResponse.Content.ReadAsStringAsync();
                                ultimoError = new AbrilException(
                                    $"Error al subir fragmento ({(int)chunkResponse.StatusCode}).", 502);
                                _logger.LogWarning(
                                    "Fragmento {Inicio}-{Fin}/{Total} falló ({Status}) intento {Intento} para {Path}: {Body}",
                                    rangoInicio, rangoFin, fileLength, chunkResponse.StatusCode, intento, pathParaLog, body);
                            }
                        }
                        catch (Exception ex) when (ex is not AbrilException)
                        {
                            ultimoError = ex;
                            _logger.LogWarning(ex,
                                "Excepción subiendo fragmento {Inicio}-{Fin}/{Total} intento {Intento} para {Path}",
                                rangoInicio, rangoFin, fileLength, intento, pathParaLog);
                        }

                        if (!exito && intento < maxIntentosPorChunk)
                            await Task.Delay(TimeSpan.FromSeconds(intento)); // backoff simple
                    }

                    if (!exito)
                    {
                        _logger.LogError(ultimoError,
                            "Subida de fragmento agotó reintentos para {Path}", pathParaLog);
                        throw new AbrilException(
                            "No se pudo completar la subida del archivo a SharePoint tras varios intentos. Intenta nuevamente.",
                            502);
                    }

                    enviados += tamanoChunk;
                }
            }
            catch
            {
                // Best-effort: cancelar la sesión para no dejar un upload huérfano en SharePoint.
                try
                {
                    using var cancelRequest = new HttpRequestMessage(HttpMethod.Delete, uploadUrl);
                    await client.SendAsync(cancelRequest);
                }
                catch (Exception cancelEx)
                {
                    _logger.LogWarning(cancelEx, "No se pudo cancelar la upload session huérfana para {Path}", pathParaLog);
                }
                throw;
            }
        }

        private static async Task<int> LeerBufferCompletoAsync(Stream stream, byte[] buffer, int cantidad)
        {
            var leidos = 0;
            while (leidos < cantidad)
            {
                var n = await stream.ReadAsync(buffer.AsMemory(leidos, cantidad - leidos));
                if (n == 0) break; // fin de stream
                leidos += n;
            }
            return leidos;
        }

        public async Task<string> SubirArchivoYObtenerUrlAsync(Stream fileStream, string fileName, string libraryContexto, string carpetaPath)
        {
            var siteId = ResolverSiteId(libraryContexto);
            if (string.IsNullOrWhiteSpace(siteId))
                throw new AbrilException("SharePoint no está configurado.", 500);

            var token = await GetAccessTokenAsync()
                ?? throw new AbrilException("No se pudo obtener token de Microsoft Graph.", 500);

            var libraryId = ResolverLibraryId(libraryContexto);
            var driveId = await GetDriveIdAsync(siteId, token, libraryId)
                ?? throw new AbrilException("No se pudo resolver el drive de SharePoint.", 500);

            var fechaPrefix = DateTime.UtcNow.ToString("yyyyMMdd");
            var fileNameLimpio = SanitizarNombreArchivo(fileName);
            var path = $"{carpetaPath.Trim('/')}/{fechaPrefix}_{fileNameLimpio}";

            var encoded = Uri.EscapeDataString(path).Replace("%2F", "/");
            var uploadUrl = $"https://graph.microsoft.com/v1.0/sites/{siteId}/drives/{driveId}/root:/{encoded}:/content";

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var content = new StreamContent(fileStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            var response = await client.PutAsync(uploadUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("SharePoint upload falló ({Status}) para {Path}: {Body}",
                    response.StatusCode, path, body);
                throw new AbrilException($"Error al subir archivo a SharePoint ({(int)response.StatusCode}).", 502);
            }

            // Leer webUrl del cuerpo del PUT — SharePoint siempre la incluye en la respuesta
            try
            {
                var putBody = await response.Content.ReadAsStringAsync();
                using var putDoc = JsonDocument.Parse(putBody);
                if (putDoc.RootElement.TryGetProperty("webUrl", out var webUrlEl))
                {
                    var webUrl = webUrlEl.GetString();
                    if (!string.IsNullOrWhiteSpace(webUrl))
                        return webUrl;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo leer webUrl del PUT de SharePoint para {Path}", path);
            }

            var getUrl = $"https://graph.microsoft.com/v1.0/sites/{siteId}/drives/{driveId}/root:/{encoded}?$select=id,%40microsoft.graph.downloadUrl";
            var getClient = _httpClientFactory.CreateClient();
            getClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var getResponse = await getClient.GetAsync(getUrl);
            if (getResponse.IsSuccessStatusCode)
            {
                var json = await getResponse.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("@microsoft.graph.downloadUrl", out var dlUrl))
                    return dlUrl.GetString()!;
            }

            _logger.LogWarning("No se pudo obtener downloadUrl para {Path}, se devuelve ruta relativa.", path);
            return path;
        }

        private async Task<string?> GetDriveIdByNameAsync(string siteId, string token, string libraryName)
        {
            if (string.IsNullOrWhiteSpace(libraryName)) return null;

            var cacheKey = $"name:{libraryName}";
            if (_driveIdCache.TryGetValue(cacheKey, out var cached)) return cached;

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var url = $"https://graph.microsoft.com/v1.0/sites/{siteId}/drives";
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Drives list GET falló ({Status}) para sitio {Site}", response.StatusCode, siteId);
                return null;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            if (!doc.RootElement.TryGetProperty("value", out var drives)) return null;

            foreach (var drive in drives.EnumerateArray())
            {
                var name = drive.TryGetProperty("name", out var n) ? n.GetString() : null;
                if (string.Equals(name, libraryName, StringComparison.OrdinalIgnoreCase))
                {
                    var driveId = drive.GetProperty("id").GetString()!;
                    _driveIdCache[cacheKey] = driveId;
                    _logger.LogInformation(
                        "SharePoint drive por nombre '{Name}' resuelto — id: {DriveId}",
                        libraryName, driveId);
                    return driveId;
                }
            }

            _logger.LogWarning("No se encontró drive con nombre '{Name}', usando drive default", libraryName);
            return await GetDriveIdAsync(siteId, token, null);
        }

        private static string NormalizarPath(string rawPath)
        {
            var path = rawPath.Trim().TrimStart('/');
            const string prefix = "habilitacion/";
            while (path.StartsWith(prefix + prefix, StringComparison.OrdinalIgnoreCase))
                path = path[prefix.Length..];
            return path;
        }

        private static string SanitizarNombreArchivo(string fileName)
        {
            var nombre = Path.GetFileName(fileName ?? string.Empty);
            foreach (var c in Path.GetInvalidFileNameChars())
                nombre = nombre.Replace(c, '_');
            nombre = nombre.Replace(' ', '_');
            return string.IsNullOrWhiteSpace(nombre) ? "archivo" : nombre;
        }

        private async Task<string?> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_cachedToken) && _tokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(2))
                return _cachedToken;

            var tenantId = _configuration["AzureAd:TenantId"];
            var clientId = _configuration["AzureAd:ClientId"];
            var secret = _configuration["AzureAd:ClientSecret"];

            if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(secret))
                return null;

            var client = _httpClientFactory.CreateClient();
            var tokenUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", secret),
                new KeyValuePair<string, string>("scope", "https://graph.microsoft.com/.default"),
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
            });

            var response = await client.PostAsync(tokenUrl, form);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Token Graph falló ({Status})", response.StatusCode);
                return null;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            var token = doc.RootElement.GetProperty("access_token").GetString();
            var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var ex) ? ex.GetInt32() : 3600;

            _cachedToken = token;
            _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
            return token;
        }

        private string ResolverSiteId(string contexto)
        {
            var c = (contexto ?? string.Empty).ToLowerInvariant();
            if (c.Contains("interconsulta") || c.Contains("lectura-emo"))
                return _configuration["SharePoint:Sites:SSOMAApps:SiteId"]!;
            return _configuration["SharePoint:Sites:SSOMAApps:SiteId"]!;
        }

        private string? ResolverLibraryId(string contexto)
        {
            var c = (contexto ?? string.Empty).ToLowerInvariant();
            if (c.Contains("trabajadores"))  return _configuration["SharePoint:Sites:Habilitacion:TrabajadoresLibraryId"];
            if (c.Contains("empresas"))      return _configuration["SharePoint:Sites:Habilitacion:EmpresaLibraryId"];
            if (c.Contains("equipos"))       return _configuration["SharePoint:Sites:Habilitacion:EquiposLibraryId"];
            if (c.Contains("sctr"))          return _configuration["SharePoint:Sites:SSOMAApps:SctrLibraryId"];
            if (c.Contains("emo-aptitud"))   return _configuration["SharePoint:Sites:SSOMAApps:AptitudesLibraryId"];
            if (c.Contains("emo-completo"))  return _configuration["SharePoint:Sites:SSOMAApps:EMOSLibraryId"];
            if (c.Contains("interconsulta")) return _configuration["SharePoint:Sites:SSOMAApps:EmoInterconsultasLibraryId"];
            if (c.Contains("lectura-emo"))      return _configuration["SharePoint:Sites:SSOMAApps:LecturaEmosLibraryId"];
            if (c.Contains("paso-evidencias")) return _configuration["SharePoint:Sites:SSOMAApps:PasoEvidenciasLibraryId"];
            if (c.Contains("rac-pdf"))           return _configuration["SharePoint:Sites:SSOMAApps:RacPdfLibraryId"];
            if (c.Contains("rac-fotos"))         return _configuration["SharePoint:Sites:SSOMAApps:RacFotosLibraryId"];
            if (c.Contains("rac-firmas"))        return _configuration["SharePoint:Sites:SSOMAApps:RacFirmasLibraryId"];
            if (c.Contains("opt-firmas"))        return _configuration["SharePoint:Sites:SSOMAApps:OptFirmasLibraryId"];
            if (c.Contains("inspeccion-fotos"))  return _configuration["SharePoint:Sites:SSOMAApps:InspeccionesAbril2026LibraryId"];
            if (c.Contains("inspeccion-firmas")) return _configuration["SharePoint:Sites:SSOMAApps:InspeccionesAbril2026LibraryId"];
            if (c.Contains("penalidad-pdf"))   return _configuration["SharePoint:Sites:SSOMAApps:PenalidadPdfLibraryId"];
            if (c.Contains("dossier-semanal")) return _configuration["SharePoint:Sites:SSOMAApps:DossierSemanal2026"];
            // paths guardados por DossierService: "{contributorId}/{proyectoId}/Sem{n}_{yyyyMMdd}/archivo"
            if (System.Text.RegularExpressions.Regex.IsMatch(c, @"^\d+/\d+/sem\d+_\d{8}/"))
                return _configuration["SharePoint:Sites:SSOMAApps:DossierSemanal2026"];
            if (c.Contains("charlas-evidencias"))  return _configuration["SharePoint:Sites:SSOMAApps:CharlasLibraryId"];
            if (c.Contains("flash-report"))         return _configuration["SharePoint:Sites:SSOMAApps:EvidenciaAccidentesLibraryId"];
            if (c.Contains("amonestacion-pdf"))     return _configuration["SharePoint:Sites:SSOMAApps:AmonestacionesPdfLibraryId"];
            return null;
        }

        public async Task<(byte[] Bytes, string ContentType, string FileName)?> DescargarPorWebUrlAsync(string webUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(webUrl)) return null;

                var token = await GetAccessTokenAsync();
                if (string.IsNullOrWhiteSpace(token)) return null;

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Graph "shares" API: cualquier URL de SharePoint (webUrl) se puede resolver
                // a su driveItem codificándola como "u!" + base64url, sin necesitar sesión
                // interactiva del usuario — solo el token de la app.
                var shareId = "u!" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(webUrl))
                    .TrimEnd('=')
                    .Replace('/', '_')
                    .Replace('+', '-');

                var itemResponse = await client.GetAsync(
                    $"https://graph.microsoft.com/v1.0/shares/{shareId}/driveItem?$select=id,name,file,parentReference,@microsoft.graph.downloadUrl");
                if (!itemResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("DescargarPorWebUrlAsync: no se pudo resolver el share ({Status}) para {Url}",
                        itemResponse.StatusCode, webUrl);
                    return null;
                }

                using var itemDoc = JsonDocument.Parse(await itemResponse.Content.ReadAsStringAsync());
                var root = itemDoc.RootElement;
                var fileName = root.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "archivo" : "archivo";
                var contentType = root.TryGetProperty("file", out var fileEl) && fileEl.TryGetProperty("mimeType", out var mimeEl)
                    ? mimeEl.GetString() ?? "application/octet-stream"
                    : "application/octet-stream";

                // Si Graph ya incluyó el link de descarga directa, usarlo (evita una llamada extra).
                if (root.TryGetProperty("@microsoft.graph.downloadUrl", out var dlEl) && dlEl.GetString() is { } directUrl)
                {
                    var directClient = _httpClientFactory.CreateClient();
                    var directResponse = await directClient.GetAsync(directUrl);
                    if (directResponse.IsSuccessStatusCode)
                        return (await directResponse.Content.ReadAsByteArrayAsync(), contentType, fileName);
                }

                // Fallback: descargar vía /content con el id + driveId del item resuelto.
                var itemId = root.GetProperty("id").GetString();
                var driveId = root.GetProperty("parentReference").GetProperty("driveId").GetString();
                var contentResponse = await client.GetAsync(
                    $"https://graph.microsoft.com/v1.0/drives/{driveId}/items/{itemId}/content");
                if (contentResponse.IsSuccessStatusCode)
                    return (await contentResponse.Content.ReadAsByteArrayAsync(), contentType, fileName);

                _logger.LogWarning("DescargarPorWebUrlAsync: /content falló ({Status}) para {Url}",
                    contentResponse.StatusCode, webUrl);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DescargarPorWebUrlAsync excepción para {Url}", webUrl);
                return null;
            }
        }

        public async Task<byte[]?> DescargarContenidoAsync(string path, string libraryContexto)
        {
            try
            {
                var siteId = ResolverSiteId(libraryContexto);
                if (string.IsNullOrWhiteSpace(siteId)) return null;

                var token = await GetAccessTokenAsync();
                if (string.IsNullOrWhiteSpace(token)) return null;

                var libraryId = ResolverLibraryId(libraryContexto);
                var driveId = await GetDriveIdAsync(siteId, token, libraryId);
                if (string.IsNullOrWhiteSpace(driveId)) return null;

                var normalizado = path.Trim().TrimStart('/');
                var encoded = Uri.EscapeDataString(normalizado).Replace("%2F", "/");
                var url = $"https://graph.microsoft.com/v1.0/sites/{siteId}/drives/{driveId}/root:/{encoded}:/content";

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsByteArrayAsync();

                // Graph devuelve 302 redirect → seguir la redirección
                if (response.StatusCode == System.Net.HttpStatusCode.Found ||
                    response.StatusCode == System.Net.HttpStatusCode.MovedPermanently)
                {
                    var location = response.Headers.Location?.ToString();
                    if (!string.IsNullOrWhiteSpace(location))
                    {
                        var redirectClient = _httpClientFactory.CreateClient();
                        var redirectResponse = await redirectClient.GetAsync(location);
                        if (redirectResponse.IsSuccessStatusCode)
                            return await redirectResponse.Content.ReadAsByteArrayAsync();
                    }
                }

                _logger.LogWarning("DescargarContenidoAsync falló ({Status}) para {Path}", response.StatusCode, path);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DescargarContenidoAsync excepción para {Path}", path);
                return null;
            }
        }

        private async Task<string?> GetDriveIdAsync(string siteId, string token, string? libraryId = null)
        {
            var cacheKey = libraryId ?? "default";
            if (_driveIdCache.TryGetValue(cacheKey, out var cached)) return cached;

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var url = string.IsNullOrWhiteSpace(libraryId)
                ? $"https://graph.microsoft.com/v1.0/sites/{siteId}/drive"
                : $"https://graph.microsoft.com/v1.0/sites/{siteId}/lists/{libraryId}/drive";

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Drive GET falló ({Status}) para sitio {Site} | library {Library}",
                    response.StatusCode, siteId, libraryId ?? "(default)");
                return null;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            var driveId     = doc.RootElement.GetProperty("id").GetString()!;
            var driveName   = doc.RootElement.TryGetProperty("name",   out var n) ? n.GetString() : "(sin nombre)";
            var driveWebUrl = doc.RootElement.TryGetProperty("webUrl", out var w) ? w.GetString() : "(sin webUrl)";
            _logger.LogInformation(
                "SharePoint drive resuelto — id: {DriveId} | name: {Name} | webUrl: {WebUrl} | library: {Library}",
                driveId, driveName, driveWebUrl, libraryId ?? "(default)");

            _driveIdCache[cacheKey] = driveId;
            return driveId;
        }
    }
}

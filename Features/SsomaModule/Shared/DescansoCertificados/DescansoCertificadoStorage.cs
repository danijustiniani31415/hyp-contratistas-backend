using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Services.SharePoint.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.SsomaModule.Shared.DescansoCertificados
{
    /// <inheritdoc cref="IDescansoCertificadoStorage"/>
    public class DescansoCertificadoStorage : IDescansoCertificadoStorage
    {
        /// <summary>Extensiones aceptadas como certificado médico (PDF o foto del descanso).</summary>
        private static readonly string[] ExtensionesPermitidas =
            [".pdf", ".jpg", ".jpeg", ".png", ".webp", ".heic"];

        /// <summary>
        /// Contexto de biblioteca con el que SharePointHabService resolvía los certificados
        /// antes de la carpeta configurable. Solo se usa para leer los adjuntos históricos.
        /// </summary>
        private const string ContextoLegado = "descanso-medico";

        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IGraphSharePointService _sharePoint;
        private readonly ISharePointHabService _sharePointLegado;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DescansoCertificadoStorage> _logger;

        public DescansoCertificadoStorage(
            IDbContextFactory<AppDbContext> factory,
            IGraphSharePointService sharePoint,
            ISharePointHabService sharePointLegado,
            IHttpClientFactory httpClientFactory,
            ILogger<DescansoCertificadoStorage> logger)
        {
            _factory           = factory;
            _sharePoint        = sharePoint;
            _sharePointLegado  = sharePointLegado;
            _httpClientFactory = httpClientFactory;
            _logger            = logger;
        }

        public async Task<List<DescansoCertificadoSubidoDto>> SubirAsync(
            IEnumerable<IFormFile> archivos,
            string prefijo)
        {
            var lista = (archivos ?? []).Where(f => f != null && f.Length > 0).ToList();
            if (lista.Count == 0) return [];

            foreach (var f in lista)
            {
                var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
                if (!ExtensionesPermitidas.Contains(ext))
                    throw new AbrilException(
                        $"Tipo de archivo no permitido: {f.FileName}. Solo PDF, JPG, PNG, WEBP o HEIC.", 400);
            }

            // Carpeta destino configurable desde BD (ss_descanso_carpeta): se guarda el link
            // tal cual y se resuelve una sola vez para todo el lote.
            string? linkUrl;
            using (var ctx = _factory.CreateDbContext())
            {
                linkUrl = await ctx.SsDescansoCarpeta
                    .Where(c => c.State && c.Active)
                    .OrderBy(c => c.Id)
                    .Select(c => c.LinkUrl)
                    .FirstOrDefaultAsync();
            }

            if (string.IsNullOrWhiteSpace(linkUrl))
                throw new AbrilException(
                    "No se ha configurado la carpeta de SharePoint donde guardar los certificados médicos. " +
                    "Pide al administrador registrarla en la tabla ss_descanso_carpeta.", 409);

            var carpeta = await _sharePoint.ResolveSharePointFolderUrlAsync(linkUrl);
            if (carpeta is null || !carpeta.IsFolder)
                throw new AbrilException(
                    "No se pudo acceder a la carpeta de certificados médicos configurada en SharePoint. " +
                    "Verifica el link registrado en ss_descanso_carpeta.", 502);

            var subidos = new List<DescansoCertificadoSubidoDto>(lista.Count);
            try
            {
                for (var i = 0; i < lista.Count; i++)
                {
                    var f        = lista[i];
                    var ext      = Path.GetExtension(f.FileName).ToLowerInvariant();
                    var safeName = SanitizarNombre(Path.GetFileNameWithoutExtension(f.FileName));
                    var stamp    = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
                    var fileName = $"{prefijo}_{stamp}_{i + 1}_{safeName}{ext}";

                    using var stream = f.OpenReadStream();
                    var result = await _sharePoint.UploadToOneDriveFolderAsync(
                        carpeta.DriveId, carpeta.ItemId, fileName, stream,
                        f.ContentType ?? "application/octet-stream",
                        autoRenameOnLock: true);

                    if (result?.WebUrl is null)
                        throw new AbrilException($"No se pudo subir el certificado {f.FileName}.", 502);

                    subidos.Add(new DescansoCertificadoSubidoDto
                    {
                        Url     = result.WebUrl,
                        Nombre  = f.FileName,
                        DriveId = carpeta.DriveId,
                        ItemId  = result.ItemId,
                    });
                }
            }
            catch (AbrilException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falló la subida de certificados de descanso médico a SharePoint");
                throw new AbrilException("Error al subir los certificados médicos a SharePoint.", 502);
            }

            return subidos;
        }

        public async Task<DescansoCertificadoArchivoDto?> DescargarAsync(
            string? driveId,
            string? itemId,
            string url,
            string? nombreArchivo)
        {
            var nombre = string.IsNullOrWhiteSpace(nombreArchivo)
                ? (string.IsNullOrWhiteSpace(url) ? "certificado" : Path.GetFileName(url.Split('?')[0]))
                : nombreArchivo;

            try
            {
                // Caso normal: adjunto subido a la carpeta configurada.
                if (!string.IsNullOrWhiteSpace(driveId) && !string.IsNullOrWhiteSpace(itemId))
                {
                    var (bytes, contentType) = await _sharePoint.DownloadFromOneDriveByItemIdAsync(driveId, itemId);
                    return Archivo(bytes, contentType, nombre);
                }

                if (string.IsNullOrWhiteSpace(url)) return null;
                var esAbsoluta = url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                              || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

                // Legado A: webUrl de SharePoint sin driveId/itemId guardados.
                if (esAbsoluta && url.Contains("sharepoint.com", StringComparison.OrdinalIgnoreCase))
                {
                    var resuelto = await _sharePoint.ResolveShareLinkAsync(url);
                    if (resuelto is null) return null;
                    var (bytes, contentType) = await _sharePoint.DownloadFromOneDriveByItemIdAsync(
                        resuelto.DriveId, resuelto.ItemId);
                    return Archivo(bytes, contentType, resuelto.Name ?? nombre);
                }

                // Legado B: URL absoluta de Azure Blob (los descansos que registraba SSOMA).
                if (esAbsoluta)
                {
                    var client = _httpClientFactory.CreateClient();
                    var response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("No se pudo descargar el certificado legado {Url} ({Status})",
                            url, response.StatusCode);
                        return null;
                    }
                    return Archivo(
                        await response.Content.ReadAsByteArrayAsync(),
                        response.Content.Headers.ContentType?.MediaType,
                        nombre);
                }

                // Legado C: ruta relativa del sitio SSOMAApps ("habilitacion/descanso-medico/…").
                var contenido = await _sharePointLegado.DescargarContenidoAsync(url, ContextoLegado);
                return contenido is null ? null : Archivo(contenido, null, nombre);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Error descargando el certificado de descanso médico (driveId={DriveId}, itemId={ItemId}, url={Url})",
                    driveId, itemId, url);
                return null;
            }
        }

        private static DescansoCertificadoArchivoDto Archivo(byte[] bytes, string? contentType, string nombre) => new()
        {
            Contenido     = bytes,
            ContentType   = string.IsNullOrWhiteSpace(contentType) ? InferirContentType(nombre) : contentType,
            NombreArchivo = string.IsNullOrWhiteSpace(nombre) ? "certificado" : nombre,
        };

        /// <summary>Tipo MIME por extensión, para cuando el origen no lo informa.</summary>
        private static string InferirContentType(string nombre) =>
            Path.GetExtension(nombre).ToLowerInvariant() switch
            {
                ".pdf"  => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png"  => "image/png",
                ".webp" => "image/webp",
                ".heic" => "image/heic",
                _       => "application/octet-stream",
            };

        private static string SanitizarNombre(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "certificado";
            var invalidos = Path.GetInvalidFileNameChars().Concat([' ', '#', '%', '&', '+']).ToHashSet();
            var limpio = new string(name.Select(c => invalidos.Contains(c) ? '_' : c).ToArray());
            return limpio.Length > 60 ? limpio[..60] : limpio;
        }
    }
}

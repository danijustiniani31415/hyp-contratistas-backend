using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Interfaces;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Interfaces;
using Abril_Backend.Shared.Services.SharePoint.Dtos;
using Abril_Backend.Shared.Services.SharePoint.Interfaces;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Services
{
    /// <inheritdoc cref="IReclutamientoArchivoStorage"/>
    public class ReclutamientoArchivoStorage : IReclutamientoArchivoStorage
    {
        private readonly IReclutamientoRepository _repo;
        private readonly IGraphSharePointService _sharePoint;
        private readonly ILogger<ReclutamientoArchivoStorage> _logger;

        public ReclutamientoArchivoStorage(
            IReclutamientoRepository repo,
            IGraphSharePointService sharePoint,
            ILogger<ReclutamientoArchivoStorage> logger)
        {
            _repo       = repo;
            _sharePoint = sharePoint;
            _logger     = logger;
        }

        public async Task<ShareLinkResolveDto> ResolverCarpetaRequerimientoAsync(string codigo)
        {
            var folderUrl = await _repo.GetSustentoFolderUrl();
            if (string.IsNullOrWhiteSpace(folderUrl))
                throw new AbrilException("No está configurada la carpeta de archivos de reclutamiento.", 500);

            var raiz = await _sharePoint.ResolveSharePointFolderUrlAsync(folderUrl);
            if (raiz == null || !raiz.IsFolder)
                throw new AbrilException("No se pudo resolver la carpeta de reclutamiento en SharePoint.", 502);

            // Subcarpeta por requerimiento para agrupar sus archivos.
            try
            {
                var subItemId = await _sharePoint.EnsureChildFolderAsync(
                    raiz.DriveId, raiz.ItemId, $"Long list {SanitizeFilename(codigo)}");
                return new ShareLinkResolveDto { DriveId = raiz.DriveId, ItemId = subItemId, IsFolder = true };
            }
            catch (Exception ex)
            {
                // Si no se pudo crear la subcarpeta, se cae a la carpeta raíz (los nombres de archivo
                // ya incluyen el código del requerimiento, así que no colisionan).
                _logger.LogWarning(ex, "No se pudo crear la subcarpeta de long list de {Codigo}; se usa la carpeta raíz", codigo);
                return raiz;
            }
        }

        public async Task<SharePointUploadResultDto> SubirArchivoRequerimientoAsync(
            ShareLinkResolveDto carpeta, string prefijo, string codigo, string pos,
            string origFileName, byte[] content, string? contentType)
        {
            var ext      = Path.GetExtension(origFileName);
            var stamp    = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            var filename = $"{prefijo}_{SanitizeFilename(codigo)}_{pos}_{stamp}{ext}";

            try
            {
                using var stream = new MemoryStream(content);
                var result = await _sharePoint.UploadToOneDriveFolderAsync(
                    carpeta.DriveId, carpeta.ItemId, filename,
                    stream, string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
                    autoRenameOnLock: true);

                if (result?.WebUrl is null)
                    throw new AbrilException("No se pudo subir un archivo del requerimiento a SharePoint.", 502);

                return result;
            }
            catch (AbrilException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falló la subida de un archivo ({Prefijo}) del requerimiento {Codigo}", prefijo, codigo);
                throw new AbrilException("Error al subir los archivos a SharePoint.", 502);
            }
        }

        /// <summary>
        /// Deja el texto usable como nombre de archivo de SharePoint: reemplaza los caracteres que
        /// rompen la url (incluidos espacio, #, %, & y +) y lo recorta a 60 caracteres.
        /// </summary>
        private static string SanitizeFilename(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "archivo";
            var invalid = Path.GetInvalidFileNameChars().Concat(new[] { ' ', '#', '%', '&', '+' }).ToHashSet();
            var clean = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
            return clean.Length > 60 ? clean.Substring(0, 60) : clean;
        }
    }
}

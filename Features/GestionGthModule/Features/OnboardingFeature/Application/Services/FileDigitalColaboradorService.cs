using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Application.Dtos;
using Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Application.Interfaces;
using Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Infrastructure.Interfaces;
using Abril_Backend.Shared.Services.SharePoint.Interfaces;

namespace Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Application.Services
{
    /// <summary>
    /// Subcarpetas del file del colaborador. GTH pidió que cada documento del expediente viva en su
    /// propia carpeta con nombre fijo (no una por onboarding), para poder ubicarlo siempre en el mismo
    /// sitio y dar permisos sobre el file completo. Son literales: si se renombran acá, los documentos
    /// nuevos van a una carpeta nueva y los ya subidos se quedan donde están.
    /// </summary>
    public static class SubcarpetaFileDigital
    {
        public const string CartaEnviada = "Carta Oferta Enviada";
        public const string CartaFirmada = "Carta Oferta Firmada";
    }

    /// <inheritdoc cref="IFileDigitalColaboradorService"/>
    public class FileDigitalColaboradorService : IFileDigitalColaboradorService
    {
        private readonly IOnboardingRepository _repo;
        private readonly IGraphSharePointService _sharePoint;
        private readonly ILogger<FileDigitalColaboradorService> _logger;

        public FileDigitalColaboradorService(
            IOnboardingRepository repo,
            IGraphSharePointService sharePoint,
            ILogger<FileDigitalColaboradorService> logger)
        {
            _repo       = repo;
            _sharePoint = sharePoint;
            _logger     = logger;
        }

        /// <summary>
        /// Resuelve la biblioteca configurada (link en <c>gth_carta_oferta_folder</c>) y, dentro, la
        /// carpeta del colaborador —«{DNI} - {NOMBRE}»—: esa carpeta es su file digital y de su nombre
        /// dependen tanto ubicarla como los permisos que se le den encima.
        ///
        /// El link se lee de la BD en cada uso, así que cambiar esa fila redirige los documentos nuevos
        /// sin redeploy y sin tocar los anteriores. Para un onboarding ya abierto no se vuelve a
        /// llamar: su carpeta queda persistida en la fila.
        ///
        /// Si la carpeta no se puede crear se corta con error, sin caer a la raíz de la biblioteca:
        /// dejar el documento suelto en la raíz lo saca del file del colaborador y lo pone donde el
        /// permiso que cuelga de su carpeta no aplica.
        /// </summary>
        public async Task<FileDigitalCarpetaDto> ResolverCarpetaAsync(string dni, string nombre)
        {
            // Al abrir el onboarding esto ya se validó con un mensaje más preciso; la guarda cubre a
            // los onboardings viejos, que rearman su carpeta acá al subirles un documento nuevo.
            if (string.IsNullOrWhiteSpace(dni))
                throw new AbrilException(
                    "El colaborador no tiene documento de identidad registrado y con él se nombra su carpeta en el file de colaboradores. Complétalo en su ficha de la base maestra.", 409);

            var folder = await _repo.GetCartaOfertaFolder();
            if (folder == null || string.IsNullOrWhiteSpace(folder.LinkUrl))
                throw new AbrilException(
                    "No está configurada la biblioteca de SharePoint donde se guarda el file de los colaboradores.", 500);

            var raiz = await _sharePoint.ResolveSharePointFolderUrlAsync(folder.LinkUrl);
            if (raiz == null || !raiz.IsFolder)
                throw new AbrilException(
                    "No se pudo resolver en SharePoint la biblioteca configurada para el file de los colaboradores. Revisa que el link apunte a una carpeta existente y accesible.", 502);

            var biblioteca = string.IsNullOrWhiteSpace(folder.FolderName) ? raiz.Name : folder.FolderName;

            // El nombre va en mayúsculas a propósito: viene tal cual lo escribió el postulante y la
            // carpeta es el identificador del file, que GTH lee a diario. EnsureChildFolder compara
            // sin distinguir mayúsculas, así que un cambio de capitalización no duplica la carpeta.
            var carpetaColaborador = $"{SanitizeFilename(dni)} - {SanitizeFilename(nombre).ToUpperInvariant()}";

            try
            {
                var itemId = await _sharePoint.EnsureChildFolderAsync(raiz.DriveId, raiz.ItemId, carpetaColaborador);
                return new FileDigitalCarpetaDto
                {
                    DriveId = raiz.DriveId,
                    ItemId  = itemId,
                    Ruta    = $"{biblioteca} / {carpetaColaborador}",
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "No se pudo crear el file del colaborador {Carpeta} en SharePoint", carpetaColaborador);
                throw new AbrilException(
                    $"No se pudo crear en SharePoint la carpeta «{carpetaColaborador}» del colaborador. Reintenta en unos minutos.", 502);
            }
        }

        public async Task<CartaOfertaPersistDto> SubirDocumentoAsync(
            FileDigitalCarpetaDto carpeta,
            string subcarpeta,
            string fileName,
            byte[] content,
            string contentType,
            string queEs)
        {
            var destino = await ResolverSubcarpetaAsync(carpeta, subcarpeta);

            try
            {
                using var stream = new MemoryStream(content);
                var result = await _sharePoint.UploadToOneDriveFolderAsync(
                    destino.DriveId, destino.ItemId, fileName,
                    stream, string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
                    autoRenameOnLock: true);

                if (result?.WebUrl == null)
                    throw new AbrilException($"No se pudo subir {queEs} a SharePoint.", 502);

                // La fila del onboarding se queda con el driveId/itemId/webUrl de ESTA subida: si
                // mañana se cambia la biblioteca configurada, el documento se sigue abriendo desde acá.
                return new CartaOfertaPersistDto
                {
                    Nombre  = result.FileName ?? fileName,
                    Url     = result.WebUrl,
                    ItemId  = result.ItemId,
                    DriveId = destino.DriveId,
                };
            }
            catch (AbrilException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falló la subida de {QueEs} al archivo {FileName}", queEs, fileName);
                throw new AbrilException($"Error al subir {queEs} a SharePoint.", 502);
            }
        }

        public string NombreArchivo(string prefijo, string codigo, string extension) =>
            $"{prefijo}_{SanitizeFilename(codigo)}_{DateTime.UtcNow:yyyyMMddHHmmssfff}{extension}";

        /// <summary>
        /// Devuelve la subcarpeta <paramref name="nombre"/> dentro del file del colaborador, creándola
        /// si es la primera vez. Se resuelve al subir y no se persiste: el onboarding guarda el file
        /// (la carpeta padre) y cada tipo de documento sabe en qué subcarpeta va. EnsureChildFolder es
        /// idempotente, así que la segunda carta cae en la misma.
        /// </summary>
        private async Task<FileDigitalCarpetaDto> ResolverSubcarpetaAsync(FileDigitalCarpetaDto carpeta, string nombre)
        {
            try
            {
                var itemId = await _sharePoint.EnsureChildFolderAsync(carpeta.DriveId, carpeta.ItemId, nombre);
                return new FileDigitalCarpetaDto
                {
                    DriveId = carpeta.DriveId,
                    ItemId  = itemId,
                    Ruta    = string.IsNullOrWhiteSpace(carpeta.Ruta) ? nombre : $"{carpeta.Ruta} / {nombre}",
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "No se pudo crear la subcarpeta «{Sub}» dentro del file {ItemId}", nombre, carpeta.ItemId);
                throw new AbrilException(
                    $"No se pudo crear en SharePoint la carpeta «{nombre}» dentro del file del colaborador. Reintenta en unos minutos.", 502);
            }
        }

        /// <summary>Deja el texto usable como nombre de archivo/carpeta en SharePoint.</summary>
        private static string SanitizeFilename(string value)
        {
            var limpio = new string((value ?? string.Empty)
                .Select(ch => Path.GetInvalidFileNameChars().Contains(ch) || ch == '#' || ch == '%' ? '_' : ch)
                .ToArray())
                .Trim();
            return string.IsNullOrWhiteSpace(limpio) ? "sin_nombre" : limpio;
        }
    }
}

using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Application.Dtos;
using Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Application.Interfaces;
using Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Infrastructure.Interfaces;
using Abril_Backend.Shared.Helpers;
using Abril_Backend.Shared.Services.Pdf;
using Abril_Backend.Shared.Services.SharePoint.Interfaces;

namespace Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Application.Services
{
    /// <inheritdoc cref="ICartaOfertaFirmaService"/>
    public class CartaOfertaFirmaService : ICartaOfertaFirmaService
    {
        private readonly ICartaOfertaFirmaRepository _repo;
        private readonly IFileDigitalColaboradorService _fileDigital;
        private readonly IGraphSharePointService _sharePoint;
        private readonly ILogger<CartaOfertaFirmaService> _logger;

        public CartaOfertaFirmaService(
            ICartaOfertaFirmaRepository repo,
            IFileDigitalColaboradorService fileDigital,
            IGraphSharePointService sharePoint,
            ILogger<CartaOfertaFirmaService> logger)
        {
            _repo        = repo;
            _fileDigital = fileDigital;
            _sharePoint  = sharePoint;
            _logger      = logger;
        }

        public Task<CartaOfertaFirmaPublicoDto> GetPublico(string token) =>
            _repo.GetPublicoByToken(ExigirToken(token));

        public async Task<(byte[] Content, string ContentType, string FileName)> GetDocumento(string token)
        {
            var ctx = await _repo.PrepararPorToken(ExigirToken(token));

            // Si ya firmó, el visor muestra el documento FIRMADO: es el que a él le importa revisar y
            // descargar, y ver el original después de firmar solo confunde.
            var (content, nombreOrigen) = ctx.YaFirmada
                ? (await DescargarAsync(ctx.CartaFirmadaDriveId, ctx.CartaFirmadaItemId, ctx.CartaFirmadaUrl),
                   ctx.CartaFirmadaNombre)
                : (await DescargarAsync(ctx.CartaOfertaDriveId, ctx.CartaOfertaItemId, ctx.CartaOfertaUrl),
                   ctx.CartaOfertaNombre);

            // La carta que sube GTH para firmar es siempre PDF, y lo que firma el postulante también.
            // Pero la carta firmada que GTH puede adjuntar a mano (la vía de respaldo) admite además
            // DOC/DOCX, así que el tipo se deriva de la extensión real en vez de asumir PDF: servir un
            // .docx como application/pdf le rompe la descarga al postulante.
            var extension = Path.GetExtension(nombreOrigen ?? string.Empty);
            var (contentType, extensionSalida) = TipoContenido(extension);

            // Nombre neutro: el de SharePoint lleva el código del requerimiento y un sello de tiempo,
            // que no le dicen nada al postulante.
            var fileName = (ctx.YaFirmada ? "carta-oferta-firmada" : "carta-oferta") + extensionSalida;

            return (content, contentType, fileName);
        }

        /// <summary>Tipo MIME y extensión con los que se sirve el documento, según su extensión real.</summary>
        private static (string ContentType, string Extension) TipoContenido(string extension) =>
            extension.ToLowerInvariant() switch
            {
                ".doc"  => ("application/msword", ".doc"),
                ".docx" => ("application/vnd.openxmlformats-officedocument.wordprocessingml.document", ".docx"),
                _       => ("application/pdf", ".pdf"),
            };

        public async Task<CartaOfertaFirmaGuardarResultDto> GuardarFirma(string token, CartaOfertaFirmaGuardarDto dto)
        {
            var ctx = await _repo.PrepararPorToken(ExigirToken(token));

            // Una vez aprobada, la firma que quedó estampada es la definitiva: cambiarla en la ficha
            // dejaría el documento aprobado firmado con una imagen que ya no es la registrada.
            if (ctx.Aprobada)
                throw new AbrilException(
                    "Tu carta oferta ya fue revisada y aprobada por Gestión de Talento Humano: la firma ya no se puede cambiar.", 409);

            // Mismas reglas que la firma del Gerente General en Contabilidad: las dos van a las mismas
            // columnas de la ficha y las dos se estampan con el mismo helper de PDF.
            var bytes = FirmaImagenHelper.DecodePng(dto?.ImageBase64);

            return await _repo.GuardarFirma(ctx.PersonId, bytes, FirmaImagenHelper.Mime);
        }

        public async Task<CartaOfertaFirmarResultDto> Firmar(string token)
        {
            var ctx = await _repo.PrepararPorToken(ExigirToken(token));

            if (ctx.Aprobada)
                throw new AbrilException(
                    "Tu carta oferta ya fue revisada y aprobada por Gestión de Talento Humano: el proceso de firma está cerrado.", 409);

            // Ya firmada pero sin aprobar: se permite volver a firmar (por ejemplo si rehízo su firma
            // porque la anterior salió mal). El documento nuevo reemplaza al anterior y la revisión de
            // GTH vuelve a quedar pendiente.
            var firma = await _repo.GetFirmaBytes(ctx.PersonId)
                ?? throw new AbrilException(
                    "Primero registra tu firma en esta página y después presiona «Firmar».", 409);

            byte[] original;
            try
            {
                original = await DescargarAsync(ctx.CartaOfertaDriveId, ctx.CartaOfertaItemId, ctx.CartaOfertaUrl);
            }
            catch (AbrilException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "CARTA OFERTA FIRMA · falló la descarga del original (onboarding {OnboardingId})", ctx.OnboardingId);
                throw new AbrilException(
                    "No pudimos abrir tu carta oferta para firmarla. Inténtalo de nuevo en unos minutos.", 502);
            }

            byte[] firmado;
            try
            {
                // Última página: es donde va la línea de firma de la carta oferta. (En una factura la
                // firma del Gerente General se estampa en todas, que es el otro uso del mismo helper.)
                firmado = SignaturePdfStamper.Stamp(original, firma.Bytes, SignatureStampScope.LastPage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "CARTA OFERTA FIRMA · falló el estampado (onboarding {OnboardingId}, bytes={Bytes}, firma={Firma})",
                    ctx.OnboardingId, original.Length, firma.Bytes.Length);
                throw new AbrilException(
                    "No pudimos estampar tu firma en el documento. Escríbele a Gestión de Talento Humano para que lo revise.", 502);
            }

            // El documento firmado va a la misma carpeta donde GTH dejaría la carta firmada si la
            // subiera a mano, para que el expediente se lea igual sin importar por dónde entró.
            var carpeta = ctx.Carpeta ?? await _fileDigital.ResolverCarpetaAsync(ctx.Dni, ctx.Nombre);

            var carta = await _fileDigital.SubirDocumentoAsync(
                carpeta, SubcarpetaFileDigital.CartaFirmada,
                _fileDigital.NombreArchivo("carta_oferta_firmada", ctx.Codigo, ".pdf"),
                firmado, "application/pdf", "tu carta oferta firmada");

            var firmadaEn = await _repo.GuardarCartaFirmadaPorPostulante(ctx.OnboardingId, carta, carpeta);

            return new CartaOfertaFirmarResultDto
            {
                Message   = "¡Listo! Tu carta oferta quedó firmada y enviada a Gestión de Talento Humano.",
                FirmadaEn = firmadaEn,
            };
        }

        /// <summary>
        /// Descarga un documento del file digital. Se prefiere driveId + itemId, que es lo que quedó
        /// guardado al subirlo; la webUrl es el respaldo para los documentos anteriores a que se
        /// persistiera el itemId.
        /// </summary>
        private async Task<byte[]> DescargarAsync(string? driveId, string? itemId, string? webUrl)
        {
            if (!string.IsNullOrWhiteSpace(driveId) && !string.IsNullOrWhiteSpace(itemId))
            {
                var (content, _) = await _sharePoint.DownloadFromOneDriveByItemIdAsync(driveId!, itemId!);
                return content;
            }

            if (!string.IsNullOrWhiteSpace(webUrl))
                return await _sharePoint.DownloadOneDriveFileByWebUrlAsync(webUrl!);

            throw new AbrilException(
                "No encontramos el archivo de tu carta oferta. Escríbele a Gestión de Talento Humano.", 409);
        }

        /// <summary>Token vacío: se corta acá con el mismo mensaje que un token inválido.</summary>
        private static string ExigirToken(string token) =>
            string.IsNullOrWhiteSpace(token)
                ? throw new AbrilException(
                    "El enlace no es válido o ya no está disponible. Escríbele a Gestión de Talento Humano para que te envíe uno nuevo.", 404)
                : token.Trim();
    }
}

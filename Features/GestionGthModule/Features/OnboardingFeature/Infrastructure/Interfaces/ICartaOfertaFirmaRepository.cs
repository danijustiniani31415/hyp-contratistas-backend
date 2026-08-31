using Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Application.Dtos;

namespace Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Infrastructure.Interfaces
{
    /// <summary>
    /// Acceso a datos de la página PÚBLICA donde el postulante ve y firma su carta oferta. Todo entra
    /// por el token del enlace: no hay usuario autenticado del que colgar los permisos, así que el
    /// token es lo único que decide a qué onboarding se está entrando.
    /// </summary>
    public interface ICartaOfertaFirmaRepository
    {
        /// <summary>
        /// Todo lo que la página necesita al abrirse, en una sola consulta: de qué puesto es la
        /// propuesta, el nombre del archivo que se está viendo, la firma que el postulante ya tenga
        /// registrada en su ficha y en qué estado quedó el documento.
        /// </summary>
        Task<CartaOfertaFirmaPublicoDto> GetPublicoByToken(string token);

        /// <summary>
        /// Resuelve el token para las acciones (mostrar el PDF, guardar la firma, firmar): a qué
        /// onboarding apunta, de qué ficha es la firma y dónde está la carta. No escribe nada.
        /// </summary>
        Task<CartaOfertaFirmaContextoDto> PrepararPorToken(string token);

        /// <summary>
        /// Guarda (o reemplaza) la firma en la ficha del postulante y devuelve la que quedó, ya como
        /// data URL, para repintarla en la página sin una segunda consulta.
        /// </summary>
        Task<CartaOfertaFirmaGuardarResultDto> GuardarFirma(int personId, byte[] imageBytes, string mime);

        /// <summary>
        /// Firma registrada en la ficha, en bytes, para estamparla en el PDF. Null si el postulante
        /// todavía no dibujó ninguna: es lo que mantiene bloqueado el botón «Firmar».
        /// </summary>
        Task<(byte[] Bytes, string Mime)?> GetFirmaBytes(int personId);

        /// <summary>
        /// Deja la carta ya firmada por el postulante en la fila del onboarding: llena las columnas
        /// <c>carta_firmada_*</c> —las mismas que llenaba GTH cuando subía el documento a mano, así el
        /// checklist y el avance de fase no cambian— y marca que la firmó él desde el enlace. La
        /// aprobación previa se limpia: lo que GTH hubiera aprobado ya no es lo que está adjunto.
        /// Devuelve el momento de la firma en hora de Perú.
        /// </summary>
        Task<DateTime> GuardarCartaFirmadaPorPostulante(
            int onboardingId, CartaOfertaPersistDto carta, FileDigitalCarpetaDto? carpeta);
    }
}

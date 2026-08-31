using Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Application.Dtos;

namespace Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Application.Interfaces
{
    /// <summary>
    /// Página PÚBLICA donde el postulante ve su carta oferta, registra su firma y la firma. Se entra
    /// solo con el token del enlace que le llegó por correo: no hay usuario autenticado detrás de
    /// ninguna de estas operaciones.
    /// </summary>
    public interface ICartaOfertaFirmaService
    {
        /// <summary>Contexto de la página por token (puesto, firma registrada y estado del documento).</summary>
        Task<CartaOfertaFirmaPublicoDto> GetPublico(string token);

        /// <summary>
        /// Carta oferta que subió GTH, para mostrarla en el visor. Se descarga de SharePoint con los
        /// permisos de la aplicación: el postulante nunca recibe la URL de SharePoint ni necesita
        /// acceso a la biblioteca.
        /// </summary>
        Task<(byte[] Content, string ContentType, string FileName)> GetDocumento(string token);

        /// <summary>
        /// Guarda la firma que el postulante dibujó, en su ficha de la base maestra
        /// (<c>person.signature_*</c>). Es lo que habilita el botón «Firmar».
        /// </summary>
        Task<CartaOfertaFirmaGuardarResultDto> GuardarFirma(string token, CartaOfertaFirmaGuardarDto dto);

        /// <summary>
        /// Firma la carta oferta: estampa la firma registrada en la última página del PDF, sube el
        /// resultado a la carpeta «Carta Oferta Firmada» del file digital del colaborador y lo deja
        /// como carta firmada del onboarding, pendiente de la revisión de GTH.
        /// </summary>
        Task<CartaOfertaFirmarResultDto> Firmar(string token);
    }
}

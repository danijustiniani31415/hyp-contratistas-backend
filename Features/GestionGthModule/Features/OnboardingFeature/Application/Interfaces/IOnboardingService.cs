using Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Application.Dtos;

namespace Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Application.Interfaces
{
    public interface IOnboardingService
    {
        /// <summary>Bandeja de Onboarding (resumen + fases + colaboradores + candidatos aptos).</summary>
        Task<BandejaOnboardingDto> GetBandeja();

        /// <summary>
        /// Inicia el onboarding de un candidato seleccionado: sube la carta oferta (PDF) a SharePoint,
        /// registra el proceso en la fase «Carta oferta firmada» y le manda al colaborador un correo
        /// con el enlace donde la lee y la firma. La carta no se adjunta al correo.
        /// </summary>
        Task<OnboardingCreateResultDto> Iniciar(
            OnboardingCreateDto dto,
            string cartaFileName,
            string cartaContentType,
            byte[] cartaContent,
            int? userId);

        /// <summary>
        /// Vuelve a mandarle al colaborador el correo con el enlace para firmar su carta oferta (por
        /// ejemplo si el primer correo no salió, o si cambió de correo). Conserva el token del enlace
        /// original; <paramref name="correo"/> solo viaja si GTH lo corrigió a mano.
        /// </summary>
        Task<OnboardingAccionResultDto> ReenviarEnlaceFirma(int onboardingId, string? correo, int? userId);

        /// <summary>
        /// Adjunta la carta oferta que el colaborador devolvió firmada. Es la vía de RESPALDO del
        /// flujo: lo normal es que la firme él mismo desde el enlace público, pero se conserva para el
        /// que la firme en papel. Se sube al file digital del onboarding — la misma carpeta de
        /// SharePoint donde quedó la carta oferta enviada — y la deja pendiente de aprobación por GTH.
        /// </summary>
        Task<OnboardingAccionResultDto> SubirCartaFirmada(
            int onboardingId,
            string fileName,
            string contentType,
            byte[] content,
            int? userId);

        /// <summary>Aprueba la carta oferta firmada adjunta (RF-ONB-02).</summary>
        Task<OnboardingAccionResultDto> AprobarCartaFirmada(int onboardingId, int? userId);

        /// <summary>Avanza el onboarding a la fase siguiente del checklist.</summary>
        Task<OnboardingAccionResultDto> Avanzar(int onboardingId, int? userId);
    }
}

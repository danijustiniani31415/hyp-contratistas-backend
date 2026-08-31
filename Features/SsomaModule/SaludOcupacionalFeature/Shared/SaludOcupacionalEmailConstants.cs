using Abril_Backend.Shared.Services.Email.Configuration;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Shared
{
    public static class SaludOcupacionalEmailConstants
    {
        /// <summary>
        /// Clave del buzón remitente para el parámetro <c>sender</c> de
        /// <c>IEmailService.SendAsync</c>. No es una dirección de correo.
        /// </summary>
        public const string SenderKey = EmailSenders.MedicinaOcupacional;

        /// <summary>
        /// Dirección del buzón de Salud Ocupacional. Usar solo donde se necesita la dirección
        /// literal (ej. incluirlo en copia); para elegir el remitente de un envío va
        /// <see cref="SenderKey"/>.
        /// </summary>
        public const string Remitente = "medicinaocupacionalnm@abril.pe";
    }
}

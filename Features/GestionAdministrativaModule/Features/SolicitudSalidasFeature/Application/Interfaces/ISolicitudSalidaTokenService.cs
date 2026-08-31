namespace Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Application.Interfaces
{
    public enum SolicitudSalidaAction { Aprobar, Rechazar }

    public class SolicitudSalidaTokenPayload
    {
        public int SolicitudId { get; set; }
        public SolicitudSalidaAction Action { get; set; }
    }

    /// <summary>
    /// Genera y valida tokens HMAC firmados que se incrustan en los links
    /// del email de notificación al aprobador.
    /// </summary>
    public interface ISolicitudSalidaTokenService
    {
        /// <summary>Genera un token firmado para aprobar/rechazar una solicitud.</summary>
        string Generate(int solicitudId, SolicitudSalidaAction action, TimeSpan validity);

        /// <summary>Valida y desencripta un token. Devuelve null si es inválido o expiró.</summary>
        SolicitudSalidaTokenPayload? Validate(string token);
    }
}

namespace Abril_Backend.Features.NotificacionesModule.Application.Interfaces
{
    public interface INotificacionService
    {
        /// <summary>Dispara un evento: resuelve destinatarios por las reglas activas de ese
        /// evento (rol + scope), deja bitácora en lb_notif_envio, y envía el correo. Un correo
        /// que falla no revierte nada — el evento origen ya se guardó, esto es solo informativo.
        /// </summary>
        /// <param name="evento">Código del evento, ej. PEDIDO_ESTANCADO_VISADO.</param>
        /// <param name="proyectoId">Proyecto del registro que disparó el evento — resuelve scope
        /// MISMO_PROYECTO.</param>
        /// <param name="solicitanteUsuarioSistemaId">Usuario que originó el registro — resuelve
        /// scope SOLICITANTE. Null si no aplica ningún destinatario SOLICITANTE para este evento.
        /// </param>
        /// <param name="referenciaTipo">PEDIDO, COMPRA, etc. — mismo valor que el evento origen,
        /// para poder preguntar "¿ya se avisó esto?" (ver <see cref="YaSeNotificoHoy"/>).</param>
        Task Dispatch(
            string evento, int proyectoId, long? solicitanteUsuarioSistemaId,
            string asunto, string mensajeHtml, string? referenciaTipo, long? referenciaId);

        /// <summary>true si ya se registró un envío para esta referencia con este evento en las
        /// últimas 24h — evita que un job que corre cada N minutos reenvíe la misma alerta de SLA
        /// una y otra vez.</summary>
        Task<bool> YaSeNotificoHoy(string evento, string referenciaTipo, long referenciaId);
    }
}

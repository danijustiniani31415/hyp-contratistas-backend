namespace Abril_Backend.Shared.Models
{
    /// <summary>
    /// Notificación in-app de la campanita del encabezado (tabla <c>notificacion</c>). Una fila
    /// por destinatario × evento. <c>Leida</c> marca el estado leído/no leído (al hacer click la
    /// notificación "apaga sus colores"); <c>Origen*</c> identifica a quien generó el evento
    /// (para las iniciales del avatar del panel).
    /// </summary>
    public class Notificacion
    {
        public int NotificacionId { get; set; }

        /// <summary>Destinatario (FK a <c>app_user</c>).</summary>
        public int UserId { get; set; }

        public int NotificacionTipoId { get; set; }

        public string Titulo { get; set; } = null!;

        /// <summary>Línea secundaria, p.ej. "Puesto — Área".</summary>
        public string? Subtitulo { get; set; }

        /// <summary>Detalle, p.ej. la justificación de la solicitud.</summary>
        public string? Descripcion { get; set; }

        /// <summary>Código de referencia del evento, p.ej. REQ-AAAA-NNNN.</summary>
        public string? Referencia { get; set; }

        /// <summary>Usuario que generó el evento (FK a <c>app_user</c>). Null si no aplica.</summary>
        public int? OrigenUserId { get; set; }

        /// <summary>Snapshot del nombre de quien generó el evento (iniciales del avatar).</summary>
        public string? OrigenNombre { get; set; }

        public bool Leida { get; set; }
        public DateTimeOffset? LeidaDateTime { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; } = true;
        public bool State { get; set; } = true;
    }
}

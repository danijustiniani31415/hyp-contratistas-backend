namespace Abril_Backend.Shared.Services.Notificaciones.Dtos
{
    /// <summary>Respuesta de la campanita: contador de no leídas + lista, en una sola petición.</summary>
    public class NotificacionesDto
    {
        /// <summary>Total de notificaciones no leídas del usuario (badge de la campanita).</summary>
        public int NoLeidas { get; set; }

        /// <summary>Notificaciones del usuario, más recientes primero (últimas 50).</summary>
        public List<NotificacionItemDto> Notificaciones { get; set; } = new();
    }

    /// <summary>Una notificación del panel.</summary>
    public class NotificacionItemDto
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Subtitulo { get; set; }
        public string? Descripcion { get; set; }
        public string? Referencia { get; set; }
        public string? OrigenNombre { get; set; }
        public bool Leida { get; set; }

        /// <summary>Fecha de creación en hora Perú (UTC-5).</summary>
        public DateTime Fecha { get; set; }
    }

    /// <summary>Contenido de una notificación a crear (el servicio la replica por destinatario).</summary>
    public class NuevaNotificacionDto
    {
        public string Titulo { get; set; } = string.Empty;
        public string? Subtitulo { get; set; }
        public string? Descripcion { get; set; }
        public string? Referencia { get; set; }
    }
}

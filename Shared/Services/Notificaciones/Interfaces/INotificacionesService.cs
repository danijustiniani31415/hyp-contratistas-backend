using Abril_Backend.Shared.Services.Notificaciones.Dtos;

namespace Abril_Backend.Shared.Services.Notificaciones.Interfaces
{
    /// <summary>
    /// Notificaciones in-app (campanita del encabezado). Cualquier módulo puede crear
    /// notificaciones vía <see cref="CrearPorCorreosAsync"/>; la campanita las consume con los
    /// métodos de lectura/marcado.
    /// </summary>
    public interface INotificacionesService
    {
        /// <summary>
        /// Crea las notificaciones (una por destinatario × item) para los usuarios cuyos correos
        /// coincidan con <paramref name="destinatarioEmails"/> (correos sin usuario del sistema se
        /// ignoran, p.ej. buzones grupales). <paramref name="origenUserId"/> identifica a quien
        /// generó el evento; su nombre se resuelve y guarda como snapshot.
        /// </summary>
        Task CrearPorCorreosAsync(
            string tipoCodigo,
            IReadOnlyCollection<string> destinatarioEmails,
            int? origenUserId,
            IReadOnlyCollection<NuevaNotificacionDto> items);

        /// <summary>Campanita del usuario: contador de no leídas + últimas notificaciones, en 1 petición.</summary>
        Task<NotificacionesDto> GetMisNotificaciones(int userId);

        /// <summary>Marca una notificación del usuario como leída ("apagar colores"). 404 si no existe o no es suya.</summary>
        Task MarcarLeida(int notificacionId, int userId);

        /// <summary>Marca todas las notificaciones no leídas del usuario como leídas.</summary>
        Task MarcarTodasLeidas(int userId);
    }
}

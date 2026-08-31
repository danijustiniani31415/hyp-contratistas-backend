using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Abril_Backend.Shared.Realtime
{
    /// <summary>
    /// Hub de notificaciones en tiempo real. Es solo un punto de conexión: el servidor
    /// empuja eventos a los clientes; los clientes no invocan métodos aquí.
    /// SignalR asocia cada conexión a su usuario mediante el claim NameIdentifier del
    /// JWT, lo que permite dirigir mensajes con <c>Clients.User(userId)</c>.
    /// </summary>
    [Authorize]
    public class NotificationsHub : Hub
    {
    }
}

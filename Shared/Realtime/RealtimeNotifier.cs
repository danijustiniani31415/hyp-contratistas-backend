using Microsoft.AspNetCore.SignalR;

namespace Abril_Backend.Shared.Realtime
{
    public class RealtimeNotifier : IRealtimeNotifier
    {
        private readonly IHubContext<NotificationsHub> _hub;

        public RealtimeNotifier(IHubContext<NotificationsHub> hub)
        {
            _hub = hub;
        }

        /// <summary>
        /// Evento único que escucha el frontend; al recibirlo, llama a /auth/refresh
        /// para regenerar su JWT con los permisos actuales.
        /// </summary>
        private const string RefreshEvent = "refreshSession";

        public Task NotifyUserRolesChanged(int userId) =>
            _hub.Clients.User(userId.ToString()).SendAsync(RefreshEvent);

        public Task NotifyRoleFeaturesChanged(IEnumerable<int> userIds)
        {
            var ids = userIds.Select(id => id.ToString()).Distinct().ToList();
            if (ids.Count == 0) return Task.CompletedTask;
            return _hub.Clients.Users(ids).SendAsync(RefreshEvent);
        }
    }
}

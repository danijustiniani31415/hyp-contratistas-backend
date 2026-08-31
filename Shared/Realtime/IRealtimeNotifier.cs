namespace Abril_Backend.Shared.Realtime
{
    /// <summary>
    /// Empuja a los clientes conectados la señal de "refresca tu sesión" cuando cambian
    /// sus permisos. Es best-effort: solo llega a quien tenga la app abierta en ese
    /// instante; el resto se entera en su próximo refresh de token (red de seguridad).
    /// </summary>
    public interface IRealtimeNotifier
    {
        /// <summary>Cambió(aron) el/los rol(es) de un usuario puntual.</summary>
        Task NotifyUserRolesChanged(int userId);

        /// <summary>Cambiaron las funcionalidades de un rol: afecta a varios usuarios.</summary>
        Task NotifyRoleFeaturesChanged(IEnumerable<int> userIds);
    }
}

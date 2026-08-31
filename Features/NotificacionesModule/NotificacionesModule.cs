using Abril_Backend.Shared.Services.Notificaciones.Interfaces;
using Abril_Backend.Shared.Services.Notificaciones.Services;

namespace Abril_Backend.Features.NotificacionesModule
{
    /// <summary>
    /// Módulo de Notificaciones in-app (campanita del encabezado). La lógica vive en el servicio
    /// compartido <see cref="INotificacionesService"/> (Shared/Services/Notificaciones) porque
    /// otros módulos también lo usan para crear notificaciones (p.ej. Gestión GTH al registrar
    /// una solicitud de personal); este módulo aporta el controller de la campanita.
    /// </summary>
    public static class NotificacionesModule
    {
        public static IServiceCollection AddNotificacionesModule(this IServiceCollection services)
        {
            services.AddScoped<INotificacionesService, NotificacionesService>();
            return services;
        }
    }
}

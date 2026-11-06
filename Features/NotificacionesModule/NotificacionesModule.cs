using Abril_Backend.Features.NotificacionesModule.Application.Interfaces;
using Abril_Backend.Features.NotificacionesModule.Application.Services;

namespace Abril_Backend.Features.NotificacionesModule
{
    /// <summary>Motor de notificaciones transversal — CONTEXT_LOGISTICA.md sección 5.</summary>
    public static class NotificacionesModule
    {
        public static IServiceCollection AddNotificacionesModule(this IServiceCollection services)
        {
            services.AddScoped<Abril_Backend.Shared.Services.Notificaciones.Interfaces.INotificacionesService,
                Abril_Backend.Shared.Services.Notificaciones.Services.NotificacionesService>();
            services.AddScoped<INotificacionService, NotificacionService>();
            return services;
        }
    }
}

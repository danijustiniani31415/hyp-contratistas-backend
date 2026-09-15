using Abril_Backend.Features.HerramientasModule.Application.Interfaces;
using Abril_Backend.Features.HerramientasModule.Application.Services;

namespace Abril_Backend.Features.HerramientasModule
{
    /// <summary>Herramientas y Equipos — Fase 2 (CONTEXT_LOGISTICA.md sección 3, punto 6).</summary>
    public static class HerramientasModule
    {
        public static IServiceCollection AddHerramientasModule(this IServiceCollection services)
        {
            services.AddScoped<IPrestamoService, PrestamoService>();
            return services;
        }
    }
}

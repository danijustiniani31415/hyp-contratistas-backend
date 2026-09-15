using Abril_Backend.Features.ComprasModule.Application.Interfaces;
using Abril_Backend.Features.ComprasModule.Application.Services;

namespace Abril_Backend.Features.ComprasModule
{
    /// <summary>Compras — Fase 3 (CONTEXT_LOGISTICA.md sección 3, punto 7).</summary>
    public static class ComprasModule
    {
        public static IServiceCollection AddComprasModule(this IServiceCollection services)
        {
            services.AddScoped<IComprasService, ComprasService>();
            return services;
        }
    }
}

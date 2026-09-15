using Abril_Backend.Features.EppModule.Application.Interfaces;
using Abril_Backend.Features.EppModule.Application.Services;

namespace Abril_Backend.Features.EppModule
{
    /// <summary>EPP — Fase 2 (CONTEXT_LOGISTICA.md sección 3, punto 5).</summary>
    public static class EppModule
    {
        public static IServiceCollection AddEppModule(this IServiceCollection services)
        {
            services.AddScoped<IEppService, EppService>();
            return services;
        }
    }
}

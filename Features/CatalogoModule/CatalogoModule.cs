using Abril_Backend.Features.CatalogoModule.Application.Interfaces;
using Abril_Backend.Features.CatalogoModule.Application.Services;

namespace Abril_Backend.Features.CatalogoModule
{
    /// <summary>Catálogo Maestro de Las Bravas — CONTEXT_LOGISTICA.md sección 6 (Fase 1, punto 2).</summary>
    public static class CatalogoModule
    {
        public static IServiceCollection AddCatalogoModule(this IServiceCollection services)
        {
            services.AddScoped<ICatalogoService, CatalogoService>();
            return services;
        }
    }
}

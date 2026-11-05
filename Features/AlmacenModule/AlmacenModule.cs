using Abril_Backend.Features.AlmacenModule.Application.Interfaces;
using Abril_Backend.Features.AlmacenModule.Application.Services;

namespace Abril_Backend.Features.AlmacenModule
{
    /// <summary>Almacén / Kardex multi-almacén — CONTEXT_LOGISTICA.md sección 7 (Fase 1, punto 3).</summary>
    public static class AlmacenModule
    {
        public static IServiceCollection AddAlmacenModule(this IServiceCollection services)
        {
            services.AddScoped<IAlmacenKardexService, AlmacenKardexService>();
            services.AddScoped<IAlmacenGestionService, AlmacenGestionService>();
            services.AddScoped<IAlmacenCargaMasivaService, AlmacenCargaMasivaService>();
            return services;
        }
    }
}

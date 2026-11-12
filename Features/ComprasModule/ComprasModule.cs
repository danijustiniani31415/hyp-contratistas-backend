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
            // Lectura de comprobantes con IA (Gemini). Cambiar de proveedor de IA = otra implementación de IComprobanteLectorService.
            services.AddHttpClient<IComprobanteLectorService, GeminiComprobanteLectorService>(c => c.Timeout = TimeSpan.FromSeconds(90));
            services.AddScoped<IComprobanteCompraService, ComprobanteCompraService>();
            services.AddScoped<CompraControlService>();
            services.AddScoped<CotizacionService>();
            services.AddScoped<PagosProveedorService>();
            return services;
        }
    }
}

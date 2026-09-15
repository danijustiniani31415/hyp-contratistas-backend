using Abril_Backend.Features.PedidosModule.Application.Interfaces;
using Abril_Backend.Features.PedidosModule.Application.Services;

namespace Abril_Backend.Features.PedidosModule
{
    /// <summary>Pedidos — Fase 2 (CONTEXT_LOGISTICA.md sección 3, punto 4).</summary>
    public static class PedidosModule
    {
        public static IServiceCollection AddPedidosModule(this IServiceCollection services)
        {
            services.AddScoped<IPedidoService, PedidoService>();
            return services;
        }
    }
}

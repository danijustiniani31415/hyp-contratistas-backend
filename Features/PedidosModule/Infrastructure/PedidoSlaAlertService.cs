using Abril_Backend.Features.NotificacionesModule.Application.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.PedidosModule.Infrastructure
{
    /// <summary>Fase F (CONTEXT_LOGISTICA.md sección 5, punto 9) — corre cada hora, revisa pedidos
    /// parados en PENDIENTE o PENDIENTE_GERENTE más de <see cref="DiasUmbral"/> días y dispara la
    /// alerta correspondiente vía el motor de notificaciones. [DECIDIDO 2026-09-26] el usuario
    /// confirmó 2 días como umbral de "estancado" para pedidos de material del socavón.</summary>
    public class PedidoSlaAlertService : BackgroundService
    {
        private const int DiasUmbral = 2;
        private static readonly TimeSpan Intervalo = TimeSpan.FromHours(1);

        private readonly IServiceProvider _services;
        private readonly ILogger<PedidoSlaAlertService> _log;

        public PedidoSlaAlertService(IServiceProvider services, ILogger<PedidoSlaAlertService> log)
        {
            _services = services;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            using var timer = new PeriodicTimer(Intervalo);
            do
            {
                try
                {
                    await RevisarPedidosEstancados(ct);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "[Pedidos SLA] Error revisando pedidos estancados.");
                }
            } while (await timer.WaitForNextTickAsync(ct));
        }

        private async Task RevisarPedidosEstancados(CancellationToken ct)
        {
            using var scope = _services.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            var notificaciones = scope.ServiceProvider.GetRequiredService<INotificacionService>();

            using var ctx = factory.CreateDbContext();
            var limite = DateTimeOffset.UtcNow.AddDays(-DiasUmbral);

            var candidatos = await ctx.Pedido
                .Where(p => (p.Estado == "PENDIENTE" || p.Estado == "PENDIENTE_GERENTE") && p.CreadoEn <= limite)
                .Select(p => new { p.Id, p.Codigo, p.ProyectoId, p.Estado, p.SolicitanteUsuarioSistemaId, p.CreadoEn })
                .ToListAsync(ct);

            foreach (var pedido in candidatos)
            {
                var evento = pedido.Estado == "PENDIENTE" ? "PEDIDO_ESTANCADO_VISADO" : "PEDIDO_ESTANCADO_APROBACION";
                if (await notificaciones.YaSeNotificoHoy(evento, "PEDIDO", pedido.Id)) continue;

                var dias = (int)(DateTimeOffset.UtcNow - pedido.CreadoEn).TotalDays;
                var etapa = pedido.Estado == "PENDIENTE" ? "visado del Residente" : "aprobación de Gerencia";
                await notificaciones.Dispatch(
                    evento,
                    pedido.ProyectoId,
                    pedido.SolicitanteUsuarioSistemaId,
                    $"Pedido {pedido.Codigo} lleva {dias} días esperando {etapa}",
                    $"El pedido <strong>{pedido.Codigo}</strong> lleva <strong>{dias} días</strong> esperando " +
                    $"{etapa} sin moverse. Revísalo cuando puedas.",
                    "PEDIDO",
                    pedido.Id);
            }
        }
    }
}

using Abril_Backend.Features.GuiasRemisionModule.Application.Services;

namespace Abril_Backend.Features.GuiasRemisionModule.Infrastructure
{
    /// <summary>Cada hora revisa las guías aceptadas hace más de 24 h que siguen sin recibirse y avisa una sola vez por correo
    /// al destino y a logística (ver RecepcionGuiaService.EnviarRecordatorios).</summary>
    public class RecordatorioRecepcionService : BackgroundService
    {
        private static readonly TimeSpan Intervalo = TimeSpan.FromHours(1);
        private readonly IServiceProvider _services;
        private readonly ILogger<RecordatorioRecepcionService> _log;

        public RecordatorioRecepcionService(IServiceProvider services, ILogger<RecordatorioRecepcionService> log)
        {
            _services = services;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            // Espera inicial: no competir con el arranque de la aplicación.
            try { await Task.Delay(TimeSpan.FromMinutes(2), ct); } catch (OperationCanceledException) { return; }
            using var timer = new PeriodicTimer(Intervalo);
            do
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var n = await scope.ServiceProvider.GetRequiredService<RecepcionGuiaService>().EnviarRecordatorios();
                    if (n > 0) _log.LogInformation("[Recepción guías] {N} recordatorio(s) enviados.", n);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "[Recepción guías] Error enviando recordatorios de recepción.");
                }
            } while (await timer.WaitForNextTickAsync(ct));
        }
    }
}

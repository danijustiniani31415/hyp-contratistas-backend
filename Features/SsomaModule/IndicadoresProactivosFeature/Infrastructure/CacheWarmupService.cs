using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Application.Interfaces;

namespace Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Infrastructure;

/// <summary>
/// Pre-calienta el caché de indicadores SSOMA al arrancar el backend.
/// Así el primer usuario recibe respuesta instantánea.
/// </summary>
public class SsomaIndicadoresCacheWarmup : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SsomaIndicadoresCacheWarmup> _log;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    public SsomaIndicadoresCacheWarmup(
        IServiceProvider services,
        IMemoryCache cache,
        ILogger<SsomaIndicadoresCacheWarmup> log)
    {
        _services = services;
        _cache = cache;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Espera 5 seg para que el backend termine de arrancar
        await Task.Delay(TimeSpan.FromSeconds(5), ct);

        var now = DateTime.UtcNow;
        int mes = now.Month, anio = now.Year;

        _log.LogInformation("[SSOMA Cache] Pre-calentando indicadores {Mes}/{Anio}...", mes, anio);

        try
        {
            using var scope = _services.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IIndicadoresProactivosService>();

            // Seguimiento primero, y se reutiliza para el puntaje (evita duplicar las bulk queries)
            var seguimiento = await WarmAsync($"ind_seguimiento_{mes}_{anio}",
                () => svc.GetSeguimientoTodosProyectosAsync(mes, anio));
            await WarmAsync($"ind_puntaje_todos_{mes}_{anio}",
                () => svc.GetPuntajeTodosProyectosAsync(mes, anio, seguimiento));

            _log.LogInformation("[SSOMA Cache] Pre-calentamiento completo.");
        }
        catch (Exception ex)
        {
            _log.LogWarning("[SSOMA Cache] Error en pre-calentamiento: {Msg}", ex.Message);
        }
    }

    private async Task<T> WarmAsync<T>(string key, Func<Task<T>> factory)
    {
        if (_cache.TryGetValue(key, out T? cached) && cached != null) return cached;
        var result = await factory();
        _cache.Set(key, result, CacheTtl);
        return result;
    }
}

using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Services;

/// <summary>
/// Ratios historicos de los drivers del proyecto (HH total y N Trabajadores) por m2 de area
/// techada — mismo patron IQR que RatioService usa para materiales, pero calculado desde
/// fuentes reales independientes, no desde Project.HhTotalCasa/CantTrabajadoresCasa (campos
/// tipeados a mano, pueden ser una estimacion — ver el caso real donde 3 proyectos "cerrados"
/// distintos dieron casi el mismo ratio, señal de que reusaron el mismo indice en vez de medir):
///   - HH: suma del Tareo de Control de Acceso (personas del dia x horas de jornada). OJO: si
///     el Tareo no arranca junto con el proyecto, el total queda parcial — limitacion conocida
///     de la fuente, pendiente de resolver con una carga manual complementaria.
///   - Trabajadores: cantidad de trabajadores DISTINTOS que alguna vez tuvieron una vinculacion
///     (worker_vinculaciones) a ese proyecto — "los que alguna vez pisaron la obra", no un
///     promedio de dotacion diaria.
/// A proposito no segmenta por tipo de proyecto (cartera homogenea); "es outlier" es solo
/// informativo, la unica autoridad real es IncluidoManual — el responsable decide caso por
/// caso, incluso si el proyecto sigue activo.
/// </summary>
public class RatioDriverService : IRatioDriverService
{
    public const string HH = "HH";
    public const string TRABAJADORES = "TRABAJADORES";

    private readonly IRatioDriverRepository _repo;
    public RatioDriverService(IRatioDriverRepository repo) => _repo = repo;

    public async Task<CalcularRatiosDriversResultDto> CalcularRatiosAsync()
    {
        var proyectos = await _repo.ObtenerProyectosConAreaAsync();
        if (proyectos.Count == 0)
            return new CalcularRatiosDriversResultDto { RatiosCalculados = 0, ProyectosSinArea = 0, ProyectosSinTareo = 0 };

        var projectIds = proyectos.Select(p => p.ProjectId).ToList();
        var hhPorProyecto = (await _repo.ObtenerHhRealPorProyectoAsync(projectIds)).ToDictionary(h => h.ProjectId);
        var trabPorProyecto = (await _repo.ObtenerTrabajadoresRealPorProyectoAsync(projectIds)).ToDictionary(t => t.ProjectId);

        var items = new List<RatioDriverUpsertItem>();
        var sinTareo = 0;

        foreach (var p in proyectos)
        {
            if (hhPorProyecto.TryGetValue(p.ProjectId, out var hh) && hh.HhTotal > 0)
            {
                items.Add(new RatioDriverUpsertItem
                {
                    TipoDriver = HH,
                    ProjectId = p.ProjectId,
                    AreaTechada = p.AreaTechada,
                    Cantidad = hh.HhTotal,
                    Ratio = hh.HhTotal / p.AreaTechada,
                    DiasRegistrados = hh.DiasRegistrados,
                });
            }
            else
            {
                sinTareo++;
            }

            if (trabPorProyecto.TryGetValue(p.ProjectId, out var trab) && trab.TotalTrabajadoresDistintos > 0)
            {
                items.Add(new RatioDriverUpsertItem
                {
                    TipoDriver = TRABAJADORES,
                    ProjectId = p.ProjectId,
                    AreaTechada = p.AreaTechada,
                    Cantidad = trab.TotalTrabajadoresDistintos,
                    Ratio = trab.TotalTrabajadoresDistintos / p.AreaTechada,
                    DiasRegistrados = 0,
                });
            }
        }

        if (items.Count > 0)
        {
            await _repo.UpsertRatiosBulkAsync(items);
            await RecalcularOutliersAsync();
        }

        return new CalcularRatiosDriversResultDto
        {
            RatiosCalculados = items.Count,
            ProyectosSinArea = 0,
            ProyectosSinTareo = sinTareo,
        };
    }

    public async Task<RatioDriverComparacionDto> ObtenerComparacionAsync(string tipoDriver)
    {
        var tipo = NormalizarTipo(tipoDriver);
        var proyectos = await _repo.ObtenerPorTipoAsync(tipo);
        // Igual que en materiales: el checkbox manual es la unica autoridad sobre que entra
        // al calculo — no se filtra por HhFuente ni por si el proyecto sigue activo.
        var incluidos = proyectos.Where(p => p.IncluidoManual).Select(p => p.Ratio).OrderBy(x => x).ToList();

        return new RatioDriverComparacionDto
        {
            TipoDriver = tipo,
            Proyectos = proyectos,
            MedianaRatio = incluidos.Count > 0 ? Mediana(incluidos) : 0,
            PromedioRatio = incluidos.Count > 0 ? incluidos.Average() : 0,
            MinRatio = incluidos.Count > 0 ? incluidos.Min() : 0,
            MaxRatio = incluidos.Count > 0 ? incluidos.Max() : 0,
        };
    }

    public Task ActualizarIncluidoManualAsync(string tipoDriver, int projectId, bool incluir) =>
        _repo.ActualizarIncluidoManualAsync(NormalizarTipo(tipoDriver), projectId, incluir);

    public async Task<RatiosDriversRecomendadosDto> ObtenerRecomendadosAsync() =>
        new()
        {
            Hh = await CalcularRecomendadoAsync(HH),
            Trabajadores = await CalcularRecomendadoAsync(TRABAJADORES),
        };

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task<RatioDriverRecomendadoDto?> CalcularRecomendadoAsync(string tipo)
    {
        var proyectos = await _repo.ObtenerPorTipoAsync(tipo);
        var validos = proyectos
            .Where(p => p.IncluidoManual && !p.EsOutlier)
            .Select(p => p.Ratio)
            .OrderBy(x => x)
            .ToList();
        if (validos.Count == 0) return null;
        return new RatioDriverRecomendadoDto { TipoDriver = tipo, RatioRecomendado = Mediana(validos), NProyectos = validos.Count };
    }

    /// <summary>Recalcula el flag de outlier (IQR) por tipo de driver, en un solo lote.</summary>
    private async Task RecalcularOutliersAsync()
    {
        var filas = await _repo.ObtenerTodosParaOutlierAsync();
        var updates = new List<RatioDriverOutlierUpdate>();

        foreach (var grupo in filas.GroupBy(f => f.TipoDriver))
        {
            var enGrupo = grupo.ToList();
            if (enGrupo.Count < 4)
            {
                foreach (var f in enGrupo)
                    updates.Add(new RatioDriverOutlierUpdate { Id = f.Id, EsOutlier = false });
                continue;
            }

            var valores = enGrupo.Select(f => f.Ratio).OrderBy(x => x).ToList();
            var n = valores.Count;
            var q1 = valores[n / 4];
            var q3 = valores[3 * n / 4];
            var iqr = q3 - q1;
            var limiteInf = q1 - 1.5m * iqr;
            var limiteSup = q3 + 1.5m * iqr;

            foreach (var f in enGrupo)
            {
                var esOutlier = f.Ratio < limiteInf || f.Ratio > limiteSup;
                updates.Add(new RatioDriverOutlierUpdate { Id = f.Id, EsOutlier = esOutlier });
            }
        }

        if (updates.Count > 0)
            await _repo.ActualizarOutliersBulkAsync(updates);
    }

    private static string NormalizarTipo(string tipo) => tipo.Trim().ToUpperInvariant();

    private static decimal Mediana(List<decimal> sorted)
    {
        var n = sorted.Count;
        return n % 2 == 0 ? (sorted[n / 2 - 1] + sorted[n / 2]) / 2 : sorted[n / 2];
    }
}

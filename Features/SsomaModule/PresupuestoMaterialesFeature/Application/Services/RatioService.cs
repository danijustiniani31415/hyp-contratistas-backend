using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Services;

public class RatioService : IRatioService
{
    private readonly IRatioRepository _repo;
    private readonly IDbContextFactory<AppDbContext> _factory;

    public RatioService(IRatioRepository repo, IDbContextFactory<AppDbContext> factory)
    {
        _repo = repo;
        _factory = factory;
    }

    public async Task<CalcularRatiosResultDto> CalcularRatiosProyectoAsync(int projectId)
    {
        var familiaIdsAfectadas = new HashSet<int>();
        var resultado = await CalcularRatiosProyectoInternoAsync(projectId, familiaIdsAfectadas);

        // Detectar outliers con IQR comparando la MISMA familia entre todos los proyectos
        // (no familias distintas dentro de este proyecto — eso comparaba peras con manzanas),
        // en un solo lote (antes era una consulta + guardado por familia).
        await RecalcularOutliersDeFamiliasAsync(familiaIdsAfectadas.ToList());

        return resultado;
    }

    /// <summary>
    /// Calcula ratios de TODOS los proyectos que tengan consumo SSOMA estandarizado, de una sola vez
    /// — antes había que entrar a la ficha de cada proyecto y darle "Calcular" uno por uno, algo que
    /// hay que repetir cada vez que cambia el Kardex de cualquier histórico. La detección de outliers
    /// corre una sola vez al final, sobre todas las familias que se tocaron en el lote entero (no una
    /// vez por proyecto), que es más rápido y da el mismo resultado porque solo mira el estado final.
    /// </summary>
    public async Task<CalcularRatiosTodosResultDto> CalcularRatiosTodosLosProyectosAsync()
    {
        var proyectos = await _repo.ObtenerProyectosConConsumoEstandarizadoAsync();
        var resultado = new CalcularRatiosTodosResultDto { TotalProyectosProcesados = proyectos.Count };
        var familiaIdsAfectadas = new HashSet<int>();

        foreach (var (projectId, projectDescription) in proyectos)
        {
            try
            {
                resultado.Proyectos.Add(await CalcularRatiosProyectoInternoAsync(projectId, familiaIdsAfectadas));
            }
            catch (AbrilException ex)
            {
                resultado.Proyectos.Add(new CalcularRatiosResultDto
                {
                    ProjectId = projectId,
                    ProjectDescription = projectDescription,
                    Advertencias = [ex.Message]
                });
            }
        }

        await RecalcularOutliersDeFamiliasAsync(familiaIdsAfectadas.ToList());
        return resultado;
    }

    private async Task<CalcularRatiosResultDto> CalcularRatiosProyectoInternoAsync(int projectId, HashSet<int> familiaIdsAfectadas)
    {
        using var ctx = _factory.CreateDbContext();
        var proyecto = await ctx.Project.FindAsync(projectId)
            ?? throw new AbrilException("Proyecto no encontrado.", 404);

        var consumos = await _repo.ObtenerConsumosPorProyectoAsync(projectId);
        if (consumos.Count == 0)
            throw new AbrilException("El proyecto no tiene consumos estandarizados para calcular ratios.", 400);

        var resultado = new CalcularRatiosResultDto
        {
            ProjectId = projectId,
            ProjectDescription = proyecto.ProjectDescription
        };

        // Obtener valores driver del proyecto
        var hh           = proyecto.HhTotalCasa ?? 0;
        var areaTechada  = proyecto.AreaTechadaM2 ?? 0;
        var trabajadores = ParseTrabajadores(proyecto.CantTrabajadoresCasa);

        // Calcular ratio para cada familia
        var itemsAGuardar = new List<RatioUpsertItem>();

        foreach (var consumo in consumos)
        {
            var driver = ObtenerDriver(consumo.VariableBase, hh, areaTechada, trabajadores, consumo.CantidadTotal);

            if (driver == 0)
            {
                resultado.FamiliasSinDriver++;
                resultado.Advertencias.Add($"'{consumo.NombreFamilia}': driver '{consumo.VariableBase}' = 0 en proyecto {proyecto.ProjectDescription}. Ratio no calculado.");
                continue;
            }

            var ratio = consumo.CantidadTotal / driver;

            itemsAGuardar.Add(new RatioUpsertItem
            {
                FamiliaId = consumo.FamiliaId,
                ProjectId = projectId,
                VariableBase = consumo.VariableBase,
                CantidadTotal = consumo.CantidadTotal,
                PrecioUnitarioPromedio = consumo.PrecioUnitarioPromedio,
                ValorDriver = driver,
                RatioCantidad = ratio
            });

            resultado.RatiosCalculados++;
        }

        // Una sola conexion para todas las familias (antes se abria una por familia).
        await _repo.UpsertRatiosBulkAsync(itemsAGuardar);
        foreach (var id in itemsAGuardar.Select(i => i.FamiliaId).Distinct())
            familiaIdsAfectadas.Add(id);

        return resultado;
    }

    public async Task<List<RatioProyectoDto>> ObtenerRatiosProyectoAsync(int projectId) =>
        await _repo.ObtenerRatiosPorProyectoAsync(projectId);

    public async Task<RatioFamiliaComparacionDto?> ObtenerComparacionFamiliaAsync(int familiaId)
    {
        var ratios = await _repo.ObtenerRatiosPorFamiliaAsync(familiaId);
        if (ratios.Count == 0) return null;

        var primero = ratios[0];
        // El checkbox manual es la unica autoridad sobre que entra al calculo — EsOutlier ya se
        // reflejo ahi automaticamente cuando cambio (ver RecalcularOutliersDeFamiliasAsync), y el
        // usuario puede marcar de nuevo un outlier si de verdad quiere forzar que cuente.
        var paraRatio = ratios.Where(r => r.IncluidoManualRatio).ToList();
        var paraPrecio = ratios.Where(r => r.IncluidoManualPrecio).ToList();
        var valores = paraRatio.Select(r => r.RatioCantidad).OrderBy(x => x).ToList();

        return new RatioFamiliaComparacionDto
        {
            FamiliaId = familiaId,
            NombreFamilia = primero.NombreFamilia,
            TipoMaterial = primero.TipoMaterial,
            VariableBase = primero.VariableBase,
            PromedioRatio = valores.Count > 0 ? valores.Average() : 0,
            MedianaRatio = valores.Count > 0 ? Mediana(valores) : 0,
            MinRatio = valores.Count > 0 ? valores.Min() : 0,
            MaxRatio = valores.Count > 0 ? valores.Max() : 0,
            PromedioPrecioUnitario = paraPrecio.Select(r => r.PrecioUnitarioPromedio).DefaultIfEmpty(0).Average(),
            Proyectos = ratios.Select(r => new RatioProyectoItemDto
            {
                ProjectId = r.ProjectId,
                ProjectDescription = r.ProjectDescription,
                RatioCantidad = r.RatioCantidad,
                PrecioUnitario = r.PrecioUnitarioPromedio,
                CantidadTotal = r.CantidadTotal,
                ValorDriver = r.ValorDriver,
                EsOutlier = r.EsOutlier,
                IncluidoManualRatio = r.IncluidoManualRatio,
                IncluidoManualPrecio = r.IncluidoManualPrecio
            }).ToList()
        };
    }

    public Task ActualizarIncluidoManualAsync(int familiaId, int projectId, bool incluir, string campo) =>
        _repo.ActualizarIncluidoManualAsync(familiaId, projectId, incluir, campo);

    public Task<List<FamiliaConRatioDto>> ListarFamiliasConRatioAsync() =>
        _repo.ListarFamiliasConRatioAsync();

    public async Task<ResumenRatiosDto> ObtenerResumenAsync()
    {
        var proyectos = await _repo.ObtenerResumenAsync();
        using var ctx = _factory.CreateDbContext();
        var totalFamilias = await ctx.SsMaterialFamilia.CountAsync(f => f.Activo && f.PerteneceSsoma);
        return new ResumenRatiosDto { Proyectos = proyectos, TotalFamilias = totalFamilias };
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static decimal ObtenerDriver(string variableBase, decimal hh, decimal area, decimal trabajadores, decimal cantidadTotal) =>
        variableBase switch
        {
            "HH"           => hh,
            "AREATECHADA"  => area,
            "TRABAJADORES" => trabajadores,
            "CALCULADO"    => cantidadTotal > 0 ? 1 : 0, // ratio = cantidad / 1 = cantidad misma
            "FIJO"         => 1,
            "METRADO"      => 1,
            _              => hh
        };

    private static decimal ParseTrabajadores(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return 0;
        var clean = new string(valor.Where(c => char.IsDigit(c) || c == '.').ToArray());
        return decimal.TryParse(clean, out var d) ? d : 0;
    }

    /// <summary>
    /// Recalcula el flag de outlier de varias familias en un solo lote, comparando el ratio de cada
    /// familia a través de TODOS los proyectos que la tienen calculada (IQR) — no familias distintas
    /// dentro de un mismo proyecto. Una sola consulta y un solo guardado para todas las familias.
    /// </summary>
    private async Task RecalcularOutliersDeFamiliasAsync(List<int> familiaIds)
    {
        if (familiaIds.Count == 0) return;

        using var ctx = _factory.CreateDbContext();
        var registros = await ctx.SsRatioProyecto
            .Where(r => familiaIds.Contains(r.FamiliaId))
            .ToListAsync();

        foreach (var grupo in registros.GroupBy(r => r.FamiliaId))
        {
            var enGrupo = grupo.ToList();
            if (enGrupo.Count < 4)
            {
                foreach (var r in enGrupo) AplicarNuevoEstadoOutlier(r, false);
                continue;
            }

            var valores = enGrupo.Select(r => r.RatioCantidad).OrderBy(x => x).ToList();
            var n = valores.Count;
            var q1 = valores[n / 4];
            var q3 = valores[3 * n / 4];
            var iqr = q3 - q1;
            var limiteInf = q1 - 1.5m * iqr;
            var limiteSup = q3 + 1.5m * iqr;

            foreach (var r in enGrupo)
            {
                var nuevoEsOutlier = r.RatioCantidad < limiteInf || r.RatioCantidad > limiteSup;
                AplicarNuevoEstadoOutlier(r, nuevoEsOutlier);
            }
        }

        await ctx.SaveChangesAsync();
    }

    /// <summary>
    /// Aplica el nuevo estado de outlier a un registro. Si el estado realmente cambió (pasó a ser
    /// outlier o dejó de serlo), sincroniza el checkbox manual con esa realidad — así el checkbox
    /// nunca queda "marcado" mostrando inclusión mientras el outlier lo excluye por detrás. Si el
    /// usuario ya lo había re-marcado a mano para forzar su inclusión, esa decisión se respeta
    /// mientras el estado de outlier no vuelva a cambiar.
    /// </summary>
    private static void AplicarNuevoEstadoOutlier(SsRatioProyecto r, bool nuevoEsOutlier)
    {
        if (r.EsOutlier != nuevoEsOutlier)
        {
            r.IncluidoManualRatio = !nuevoEsOutlier;
            r.IncluidoManualPrecio = !nuevoEsOutlier;
        }
        r.EsOutlier = nuevoEsOutlier;
    }

    private static decimal Mediana(List<decimal> sorted)
    {
        var n = sorted.Count;
        return n % 2 == 0 ? (sorted[n / 2 - 1] + sorted[n / 2]) / 2 : sorted[n / 2];
    }
}

using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.SsomaModule.Shared;
using Abril_Backend.Infrastructure.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Repositories;

public class RatioDriverRepository : IRatioDriverRepository
{
    private readonly IConfiguration _config;
    private readonly IDbContextFactory<AppDbContext> _factory;

    public RatioDriverRepository(IConfiguration config, IDbContextFactory<AppDbContext> factory)
    {
        _config = config;
        _factory = factory;
    }

    private NpgsqlConnection Conn() => new(_config["Database:PostgreSQL"]!);

    public async Task<List<ProyectoAreaRow>> ObtenerProyectosConAreaAsync()
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.Project
            .Where(p => p.AreaTechadaM2 != null && p.AreaTechadaM2 > 0)
            .Select(p => new ProyectoAreaRow
            {
                ProjectId = p.ProjectId,
                AreaTechada = p.AreaTechadaM2!.Value,
                CicloVida = p.Activo ?? "Activo",
            })
            .ToListAsync();
    }

    /// <summary>
    /// HH real por proyecto. Prioridad por proyecto (no se mezclan las dos fuentes para el mismo
    /// proyecto, para no arriesgar doble conteo entre semanas de planilla y días de Tareo que se
    /// solapen):
    ///   1. Excel de planilla/Tareo semanal (HhCargaService) si el proyecto tiene alguna carga
    ///      activa — es la fuente más completa cuando alguien se tomó el trabajo de subirla.
    ///   2. Si no, Tareo de Control de Acceso (personas del día x horas de jornada de ese día,
    ///      sumado en todo el rango registrado) — misma fórmula que el dashboard de Horas Hombre.
    ///      OJO: si el Tareo no arranca junto con el proyecto (se empezó a registrar después de
    ///      que la obra ya había avanzado), este total queda por debajo del HH real de toda la
    ///      obra — es la limitación que la carga por Excel busca complementar.
    /// </summary>
    public async Task<List<ProyectoHhRealRow>> ObtenerHhRealPorProyectoAsync(List<int> projectIds)
    {
        if (projectIds.Count == 0) return [];
        using var ctx = _factory.CreateDbContext();

        var hhExcel = await ctx.SsHhCargaLinea
            .Where(l => l.Activo && projectIds.Contains(l.ProjectId))
            .Select(l => new { l.ProjectId, l.Anio, l.SemanaNum, l.HorasLaboradas })
            .ToListAsync();

        var resultado = hhExcel.GroupBy(l => l.ProjectId).Select(g => new ProyectoHhRealRow
        {
            ProjectId = g.Key,
            HhTotal = g.Sum(l => l.HorasLaboradas),
            // No hay fecha diaria en el Excel de planilla, solo semana — se aproxima a días
            // calendario para que sea comparable en escala con DiasRegistrados del Tareo.
            DiasRegistrados = g.Select(l => (l.Anio, l.SemanaNum)).Distinct().Count() * 7,
        }).ToList();

        var proyectosConExcel = resultado.Select(r => r.ProjectId).ToHashSet();
        var projectIdsSoloTareo = projectIds.Where(id => !proyectosConExcel.Contains(id)).ToList();
        if (projectIdsSoloTareo.Count == 0) return resultado;

        var tareos = await ctx.SsTareo
            .Where(t => projectIdsSoloTareo.Contains(t.ProyectoId))
            .Select(t => new { t.Id, t.ProyectoId, t.Fecha })
            .ToListAsync();

        if (tareos.Count == 0) return resultado;

        var tareoIds = tareos.Select(t => t.Id).ToList();

        var casaPorTareo = await ctx.SsTareoDetalleCasa
            .Where(d => tareoIds.Contains(d.TareoId))
            .GroupBy(d => d.TareoId)
            .Select(g => new { TareoId = g.Key, Cantidad = g.Sum(d => d.CantidadPersonas) })
            .ToDictionaryAsync(g => g.TareoId, g => g.Cantidad);

        var contratistaPorTareo = await ctx.SsTareoDetalleContratista
            .Where(d => tareoIds.Contains(d.TareoId))
            .GroupBy(d => d.TareoId)
            .Select(g => new { TareoId = g.Key, Cantidad = g.Sum(d => d.CantidadPersonas) })
            .ToDictionaryAsync(g => g.TareoId, g => g.Cantidad);

        resultado.AddRange(tareos.GroupBy(t => t.ProyectoId).Select(g =>
        {
            decimal hhTotal = 0;
            foreach (var t in g)
            {
                var casa = casaPorTareo.GetValueOrDefault(t.Id);
                var contratista = contratistaPorTareo.GetValueOrDefault(t.Id);
                hhTotal += (casa + contratista) * HorarioLaboralCalculator.HorasPorDia(t.Fecha);
            }
            return new ProyectoHhRealRow
            {
                ProjectId = g.Key,
                HhTotal = hhTotal,
                DiasRegistrados = g.Select(t => t.Fecha).Distinct().Count(),
            };
        }));

        return resultado;
    }

    /// <summary>
    /// N Trabajadores real = cantidad de trabajadores DISTINTOS que alguna vez tuvieron una
    /// vinculación (worker_vinculaciones) a ese proyecto, sin importar si son de casa o de una
    /// contratista ni cuánto duró la vinculación — "los totales que alguna vez han pisado la
    /// obra", no un promedio de dotación diaria.
    /// </summary>
    public async Task<List<ProyectoTrabajadoresRealRow>> ObtenerTrabajadoresRealPorProyectoAsync(List<int> projectIds)
    {
        if (projectIds.Count == 0) return [];
        using var ctx = _factory.CreateDbContext();

        return await ctx.WorkerVinculacion
            .Where(v => v.ProyectoId != null && projectIds.Contains(v.ProyectoId.Value))
            .GroupBy(v => v.ProyectoId!.Value)
            .Select(g => new ProyectoTrabajadoresRealRow
            {
                ProjectId = g.Key,
                TotalTrabajadoresDistintos = g.Select(v => v.WorkerId).Distinct().Count(),
            })
            .ToListAsync();
    }

    public async Task UpsertRatiosBulkAsync(List<RatioDriverUpsertItem> items)
    {
        if (items.Count == 0) return;
        using var conn = Conn();
        const string sql = """
            INSERT INTO ss_ratio_proyecto_driver
              (tipo_driver, project_id, area_techada, cantidad, ratio, dias_registrados, es_outlier)
            VALUES
              (@TipoDriver, @ProjectId, @AreaTechada, @Cantidad, @Ratio, @DiasRegistrados, false)
            ON CONFLICT (tipo_driver, project_id)
            DO UPDATE SET
              area_techada     = EXCLUDED.area_techada,
              cantidad         = EXCLUDED.cantidad,
              ratio            = EXCLUDED.ratio,
              dias_registrados = EXCLUDED.dias_registrados,
              es_outlier       = false,
              calculado_en     = now()
            """;
        // incluido_manual no se toca en el UPDATE: si el usuario ya lo marco a mano,
        // ese criterio se respeta aunque se recalculen los ratios.
        await conn.ExecuteAsync(sql, items);
    }

    public async Task<List<RatioDriverOutlierRow>> ObtenerTodosParaOutlierAsync()
    {
        using var conn = Conn();
        const string sql = "SELECT id AS Id, tipo_driver AS TipoDriver, ratio AS Ratio FROM ss_ratio_proyecto_driver";
        var result = await conn.QueryAsync<RatioDriverOutlierRow>(sql);
        return result.ToList();
    }

    public async Task ActualizarOutliersBulkAsync(List<RatioDriverOutlierUpdate> updates)
    {
        if (updates.Count == 0) return;
        using var conn = Conn();
        const string sql = "UPDATE ss_ratio_proyecto_driver SET es_outlier = @EsOutlier WHERE id = @Id";
        await conn.ExecuteAsync(sql, updates);
    }

    public async Task<List<RatioDriverProyectoDto>> ObtenerPorTipoAsync(string tipoDriver)
    {
        using var conn = Conn();
        const string sql = """
            SELECT d.project_id AS ProjectId, p.project_description AS ProjectDescription,
                   COALESCE(p.activo, 'Activo') AS CicloVida, d.dias_registrados AS DiasRegistrados,
                   d.area_techada AS AreaTechada, d.cantidad AS Cantidad, d.ratio AS Ratio,
                   d.es_outlier AS EsOutlier, d.incluido_manual AS IncluidoManual
            FROM ss_ratio_proyecto_driver d
            JOIN project p ON p.project_id = d.project_id
            WHERE d.tipo_driver = @tipoDriver
            ORDER BY d.ratio
            """;
        var result = await conn.QueryAsync<RatioDriverProyectoDto>(sql, new { tipoDriver });
        return result.ToList();
    }

    public async Task ActualizarIncluidoManualAsync(string tipoDriver, int projectId, bool incluir)
    {
        using var conn = Conn();
        const string sql = """
            UPDATE ss_ratio_proyecto_driver
            SET incluido_manual = @incluir
            WHERE tipo_driver = @tipoDriver AND project_id = @projectId
            """;
        await conn.ExecuteAsync(sql, new { tipoDriver, projectId, incluir });
    }
}

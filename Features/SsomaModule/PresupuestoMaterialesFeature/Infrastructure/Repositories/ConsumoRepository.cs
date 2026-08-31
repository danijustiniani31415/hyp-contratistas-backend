using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Repositories;

public class ConsumoRepository : IConsumoRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public ConsumoRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<SsConsumoCarga> CrearCargaAsync(SsConsumoCarga carga)
    {
        using var ctx = _factory.CreateDbContext();
        ctx.SsConsumoCarga.Add(carga);
        await ctx.SaveChangesAsync();
        return carga;
    }

    public async Task<List<SsConsumoLinea>> ObtenerLineasSinEstandarizarAsync(int cargaId)
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsConsumoLinea
            .Where(l => l.CargaId == cargaId && !l.Estandarizado)
            .ToListAsync();
    }

    public async Task<List<SsConsumoLinea>> ObtenerLineasActivasConGuiaPorProyectoAsync(int projectId)
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsConsumoLinea
            .Where(l => l.ProjectId == projectId && l.Activo && l.NroGuia != null)
            .ToListAsync();
    }

    public async Task AplicarDiffCargaAsync(
        IEnumerable<SsConsumoLinea> nuevas,
        IEnumerable<(long LineaId, decimal Cantidad, decimal PrecioUnitario, decimal PrecioTotal, DateOnly FechaGuia)> actualizaciones,
        IEnumerable<long> idsDarDeBaja,
        string motivoBaja)
    {
        using var ctx = _factory.CreateDbContext();
        var ahora = DateTimeOffset.UtcNow;

        ctx.SsConsumoLinea.AddRange(nuevas);

        var idsActualizar = actualizaciones.Select(a => a.LineaId).ToList();
        if (idsActualizar.Count > 0)
        {
            var porActualizar = await ctx.SsConsumoLinea
                .Where(l => idsActualizar.Contains(l.Id))
                .ToDictionaryAsync(l => l.Id);
            foreach (var (lineaId, cantidad, precioUnitario, precioTotal, fechaGuia) in actualizaciones)
            {
                if (!porActualizar.TryGetValue(lineaId, out var linea)) continue;
                linea.Cantidad = cantidad;
                linea.PrecioUnitario = precioUnitario;
                linea.PrecioTotal = precioTotal;
                linea.FechaGuia = fechaGuia;
                linea.ActualizadoEn = ahora;
            }
        }

        var idsBaja = idsDarDeBaja.ToList();
        if (idsBaja.Count > 0)
        {
            var porDarDeBaja = await ctx.SsConsumoLinea.Where(l => idsBaja.Contains(l.Id)).ToListAsync();
            foreach (var linea in porDarDeBaja)
            {
                linea.Activo = false;
                linea.MotivoInactivo = motivoBaja;
                linea.ActualizadoEn = ahora;
            }
        }

        await ctx.SaveChangesAsync();
    }

    public async Task ActualizarLineaEstandarizadaAsync(long lineaId, int itemId, bool perteneceSsoma, string metodo, decimal score, string? estadoRevision, decimal factorConversion = 1)
    {
        using var ctx = _factory.CreateDbContext();
        var linea = await ctx.SsConsumoLinea.FindAsync(lineaId);
        if (linea == null) return;
        linea.ItemId = itemId;
        linea.Estandarizado = true;
        linea.PerteneceSsoma = perteneceSsoma;
        linea.MetodoMatch = metodo;
        linea.ScoreMatch = score;
        linea.EstadoRevision = estadoRevision;
        linea.CantidadReal = linea.Cantidad * factorConversion;
        linea.PrecioUnitarioReal = linea.CantidadReal > 0 ? linea.PrecioTotal / linea.CantidadReal : 0;
        await ctx.SaveChangesAsync();
    }

    public async Task MarcarSinMatchAsync(long lineaId)
    {
        using var ctx = _factory.CreateDbContext();
        var linea = await ctx.SsConsumoLinea.FindAsync(lineaId);
        if (linea == null) return;
        linea.EstadoRevision = "PENDIENTE";
        linea.MetodoMatch = "SIN_MATCH";
        await ctx.SaveChangesAsync();
    }

    public async Task MarcarRechazadoAutomaticoAsync(long lineaId)
    {
        using var ctx = _factory.CreateDbContext();
        var linea = await ctx.SsConsumoLinea.FindAsync(lineaId);
        if (linea == null) return;
        linea.EstadoRevision = "RECHAZADO";
        linea.PerteneceSsoma = false;
        linea.MetodoMatch = "ALIAS_RECHAZO";
        await ctx.SaveChangesAsync();
    }

    public async Task AplicarResultadosEstandarizacionAsync(List<ResultadoLineaParaGuardar> resultados)
    {
        if (resultados.Count == 0) return;
        using var ctx = _factory.CreateDbContext();
        var ids = resultados.Select(r => r.LineaId).ToList();
        var lineasPorId = await ctx.SsConsumoLinea.Where(l => ids.Contains(l.Id)).ToDictionaryAsync(l => l.Id);

        foreach (var r in resultados)
        {
            if (!lineasPorId.TryGetValue(r.LineaId, out var linea)) continue;
            linea.Estandarizado = r.Estandarizado;
            linea.ItemId = r.ItemId;
            linea.PerteneceSsoma = r.PerteneceSsoma;
            linea.MetodoMatch = r.MetodoMatch;
            linea.ScoreMatch = r.ScoreMatch;
            linea.EstadoRevision = r.EstadoRevision;
            if (r.Estandarizado)
            {
                linea.CantidadReal = linea.Cantidad * r.FactorConversion;
                linea.PrecioUnitarioReal = linea.CantidadReal > 0 ? linea.PrecioTotal / linea.CantidadReal : 0;
            }
        }
        await ctx.SaveChangesAsync();
    }

    public async Task ActualizarContadoresCargaAsync(int cargaId, int estandarizadas, int pendientes)
    {
        using var ctx = _factory.CreateDbContext();
        var carga = await ctx.SsConsumoCarga.FindAsync(cargaId);
        if (carga == null) return;
        carga.LineasEstandarizadas = estandarizadas;
        carga.LineasPendientes = pendientes;
        await ctx.SaveChangesAsync();
    }

    public async Task ActualizarResumenCargaAsync(int cargaId, int totalLineas, int nuevas, int actualizadas, int eliminadas)
    {
        using var ctx = _factory.CreateDbContext();
        var carga = await ctx.SsConsumoCarga.FindAsync(cargaId);
        if (carga == null) return;
        carga.TotalLineas = totalLineas;
        carga.LineasNuevas = nuevas;
        carga.LineasActualizadas = actualizadas;
        carga.LineasEliminadas = eliminadas;
        await ctx.SaveChangesAsync();
    }

    public async Task<List<ConsumoCargaResumenDto>> ObtenerCargasPorProyectoAsync(int projectId)
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsConsumoCarga
            .Where(c => c.ProjectId == projectId)
            .OrderByDescending(c => c.CreadoEn)
            .Select(c => new ConsumoCargaResumenDto
            {
                Id = c.Id,
                ProjectId = c.ProjectId,
                NombreArchivo = c.NombreArchivo,
                FechaMin = c.FechaMin,
                FechaMax = c.FechaMax,
                TotalLineas = c.TotalLineas,
                LineasNuevas = c.LineasNuevas,
                LineasActualizadas = c.LineasActualizadas,
                LineasEliminadas = c.LineasEliminadas,
                LineasEstandarizadas = c.LineasEstandarizadas,
                LineasPendientes = c.LineasPendientes,
                Estado = c.Estado,
                CreadoEn = c.CreadoEn
            })
            .ToListAsync();
    }

    public async Task<List<MaterialPendienteDto>> ObtenerPendientesRevisionAsync(int projectId)
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsConsumoLinea
            .Where(l => l.ProjectId == projectId && l.Activo && l.EstadoRevision == "PENDIENTE")
            .Include(l => l.Item).ThenInclude(i => i!.Familia)
            .OrderByDescending(l => l.PrecioTotal)
            .Select(l => new MaterialPendienteDto
            {
                LineaId = l.Id,
                RecursoCrudo = l.RecursoCrudo,
                Cantidad = l.Cantidad,
                PrecioUnitario = l.PrecioUnitario,
                PrecioTotal = l.PrecioTotal,
                FechaGuia = l.FechaGuia,
                NombreItemSugerido = l.Item != null ? l.Item.Nombre : null,
                NombreFamiliaSugerida = l.Item != null ? l.Item.Familia.Nombre : null,
                ScoreMatch = l.ScoreMatch,
                MetodoMatch = l.MetodoMatch,
                EstadoRevision = l.EstadoRevision,
                ItemIdSugerido = l.ItemId
            })
            .ToListAsync();
    }

    public async Task<List<MaterialPendienteGlobalDto>> ObtenerPendientesRevisionGlobalAsync()
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsConsumoLinea
            .Where(l => l.Activo && l.EstadoRevision == "PENDIENTE")
            .Include(l => l.Item).ThenInclude(i => i!.Familia)
            .Include(l => l.Proyecto)
            .OrderByDescending(l => l.PrecioTotal)
            .Select(l => new MaterialPendienteGlobalDto
            {
                LineaId = l.Id,
                ProjectId = l.ProjectId,
                ProjectDescription = l.Proyecto.ProjectDescription ?? string.Empty,
                RecursoCrudo = l.RecursoCrudo,
                Cantidad = l.Cantidad,
                PrecioUnitario = l.PrecioUnitario,
                PrecioTotal = l.PrecioTotal,
                FechaGuia = l.FechaGuia,
                NombreItemSugerido = l.Item != null ? l.Item.Nombre : null,
                NombreFamiliaSugerida = l.Item != null ? l.Item.Familia.Nombre : null,
                ItemIdSugerido = l.ItemId,
                ScoreMatch = l.ScoreMatch,
                MetodoMatch = l.MetodoMatch,
            })
            .ToListAsync();
    }

    public async Task<List<MaterialNoSsomaDto>> ObtenerNoSsomaAsync()
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsConsumoLinea
            .Where(l => l.Activo && (!l.PerteneceSsoma || l.EstadoRevision == "RECHAZADO"))
            .Include(l => l.Proyecto)
            .OrderByDescending(l => l.FechaGuia)
            .Select(l => new MaterialNoSsomaDto
            {
                LineaId = l.Id,
                ProjectId = l.ProjectId,
                ProjectDescription = l.Proyecto.ProjectDescription ?? string.Empty,
                RecursoCrudo = l.RecursoCrudo,
                PrecioTotal = l.PrecioTotal,
                FechaGuia = l.FechaGuia,
                EstadoRevision = l.EstadoRevision,
            })
            .ToListAsync();
    }

    public async Task<SsConsumoLinea?> ObtenerLineaPorIdAsync(long lineaId)
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsConsumoLinea.FindAsync(lineaId);
    }

    /// <summary>
    /// Asigna hito_id a cada línea de consumo del proyecto cruzando fecha_guia contra el cronograma
    /// real de hitos (milestone_schedule) de la versión activa del proyecto. A cada línea le
    /// corresponde el hito más reciente cuya fecha planeada sea &lt;= fecha_guia — el hito "cubre"
    /// desde su propia fecha hasta la fecha del siguiente hito. Solo se consideran hitos marcados
    /// como críticos (es_hito_critico = true): los informativos no cortan etapa.
    /// </summary>
    public async Task<int> AsignarHitosPorFechaAsync(int projectId)
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.Database.ExecuteSqlInterpolatedAsync($"""
            WITH cronograma AS (
                SELECT ms.milestone_schedule_id, ms.planned_start_date
                FROM milestone_schedule ms
                JOIN milestone_schedule_history msh
                  ON msh.milestone_schedule_history_id = ms.milestone_schedule_history_id
                WHERE msh.project_id = {projectId}
                  AND msh.is_equal_to_last_version = true
                  AND msh.active = true
                  AND ms.active = true
                  AND ms.planned_start_date IS NOT NULL
                  AND ms.es_hito_critico = true
            )
            UPDATE ss_consumo_linea l
            SET hito_id = c.milestone_schedule_id
            FROM cronograma c
            WHERE l.project_id = {projectId}
              AND c.planned_start_date = (
                  SELECT MAX(c2.planned_start_date)
                  FROM cronograma c2
                  WHERE c2.planned_start_date <= l.fecha_guia
              )
            """);
    }

    public async Task ActualizarRevisionAsync(long lineaId, string decision, int? itemIdConfirmado)
    {
        using var ctx = _factory.CreateDbContext();
        var linea = await ctx.SsConsumoLinea.FindAsync(lineaId);
        if (linea == null) return;
        linea.EstadoRevision = decision;
        if (itemIdConfirmado.HasValue)
        {
            linea.ItemId = itemIdConfirmado.Value;
            // Una línea que llegó a Revisión sin match automático (Estandarizado seguía en false,
            // ver EstandarizacionService/MarcarSinMatchAsync) recién tiene item real acá — sin esto,
            // quedaba con item asignado pero invisible para el cálculo de ratios (exige
            // estandarizado=true). Las que ya venían con sugerencia automática ya estaban en true.
            linea.Estandarizado = true;
            // El item confirmado en revisión siempre genera un alias nuevo con factor 1 (ver RevisionMaterialesService).
            linea.CantidadReal = linea.Cantidad;
            linea.PrecioUnitarioReal = linea.Cantidad > 0 ? linea.PrecioTotal / linea.Cantidad : 0;
        }
        await ctx.SaveChangesAsync();
    }
}

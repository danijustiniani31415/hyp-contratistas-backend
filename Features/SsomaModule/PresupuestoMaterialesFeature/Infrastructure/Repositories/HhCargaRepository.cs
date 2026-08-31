using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Repositories;

public class HhCargaRepository : IHhCargaRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public HhCargaRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<SsHhCarga> CrearCargaAsync(SsHhCarga carga)
    {
        using var ctx = _factory.CreateDbContext();
        ctx.SsHhCarga.Add(carga);
        await ctx.SaveChangesAsync();
        return carga;
    }

    public async Task<List<SsHhCargaLinea>> ObtenerLineasActivasPorProyectoAsync(int projectId)
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsHhCargaLinea
            .Where(l => l.ProjectId == projectId && l.Activo)
            .ToListAsync();
    }

    public async Task AplicarDiffCargaAsync(
        IEnumerable<SsHhCargaLinea> nuevas,
        IEnumerable<(long LineaId, decimal HorasLaboradas, decimal? CostoHhNormal, decimal? Parcial)> actualizaciones,
        IEnumerable<long> idsDarDeBaja,
        string motivoBaja)
    {
        using var ctx = _factory.CreateDbContext();
        var ahora = DateTimeOffset.UtcNow;

        ctx.SsHhCargaLinea.AddRange(nuevas);

        var idsActualizar = actualizaciones.Select(a => a.LineaId).ToList();
        if (idsActualizar.Count > 0)
        {
            var porActualizar = await ctx.SsHhCargaLinea
                .Where(l => idsActualizar.Contains(l.Id))
                .ToDictionaryAsync(l => l.Id);
            foreach (var (lineaId, horas, costo, parcial) in actualizaciones)
            {
                if (!porActualizar.TryGetValue(lineaId, out var linea)) continue;
                linea.HorasLaboradas = horas;
                linea.CostoHhNormal = costo;
                linea.Parcial = parcial;
                linea.ActualizadoEn = ahora;
            }
        }

        var idsBaja = idsDarDeBaja.ToList();
        if (idsBaja.Count > 0)
        {
            var porDarDeBaja = await ctx.SsHhCargaLinea.Where(l => idsBaja.Contains(l.Id)).ToListAsync();
            foreach (var linea in porDarDeBaja)
            {
                linea.Activo = false;
                linea.MotivoInactivo = motivoBaja;
                linea.ActualizadoEn = ahora;
            }
        }

        await ctx.SaveChangesAsync();
    }

    public async Task ActualizarResumenCargaAsync(int cargaId, int totalLineas, int nuevas, int actualizadas, int eliminadas)
    {
        using var ctx = _factory.CreateDbContext();
        var carga = await ctx.SsHhCarga.FindAsync(cargaId);
        if (carga == null) return;
        carga.TotalLineas = totalLineas;
        carga.LineasNuevas = nuevas;
        carga.LineasActualizadas = actualizadas;
        carga.LineasEliminadas = eliminadas;
        await ctx.SaveChangesAsync();
    }

    public async Task<List<HhCargaResumenDto>> ObtenerCargasPorProyectoAsync(int projectId)
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsHhCarga
            .Where(c => c.ProjectId == projectId)
            .OrderByDescending(c => c.CreadoEn)
            .Select(c => new HhCargaResumenDto
            {
                Id = c.Id,
                ProjectId = c.ProjectId,
                NombreArchivo = c.NombreArchivo,
                AnioMin = c.AnioMin,
                SemanaMin = c.SemanaMin,
                AnioMax = c.AnioMax,
                SemanaMax = c.SemanaMax,
                TotalLineas = c.TotalLineas,
                LineasNuevas = c.LineasNuevas,
                LineasActualizadas = c.LineasActualizadas,
                LineasEliminadas = c.LineasEliminadas,
                Estado = c.Estado,
                CreadoEn = c.CreadoEn
            })
            .ToListAsync();
    }

    public async Task<(decimal HhTotal, int SemanasRegistradas)> ObtenerHhTotalPorProyectoAsync(int projectId)
    {
        using var ctx = _factory.CreateDbContext();
        var lineas = await ctx.SsHhCargaLinea
            .Where(l => l.ProjectId == projectId && l.Activo)
            .Select(l => new { l.Anio, l.SemanaNum, l.HorasLaboradas })
            .ToListAsync();

        if (lineas.Count == 0) return (0, 0);

        var hhTotal = lineas.Sum(l => l.HorasLaboradas);
        var semanas = lineas.Select(l => (l.Anio, l.SemanaNum)).Distinct().Count();
        return (hhTotal, semanas);
    }
}

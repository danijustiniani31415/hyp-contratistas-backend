using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Services;

public class DriversService : IDriversService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IRatioRepository _ratioRepo;
    private readonly IRatioService _ratioService;

    public DriversService(
        IDbContextFactory<AppDbContext> factory,
        IRatioRepository ratioRepo,
        IRatioService ratioService)
    {
        _factory     = factory;
        _ratioRepo   = ratioRepo;
        _ratioService = ratioService;
    }

    public async Task<List<DriverProyectoDto>> ObtenerTodosAsync()
    {
        using var ctx = _factory.CreateDbContext();

        // Proyectos habilitados para el módulo SSOMA (misma fuente de verdad que el resto del módulo)
        var proyectosHabilitados = await ctx.SsProyectoHabilitado
            .Where(h => h.State && h.Active)
            .Select(h => h.ProyectoId)
            .ToListAsync();

        // Proyectos que tienen al menos un consumo cargado (histórico o activos)
        var proyectosConConsumo = await ctx.SsConsumoCarga
            .Where(c => c.Estado == "ACTIVA")
            .Select(c => c.ProjectId)
            .Distinct()
            .ToListAsync();

        // Ratios calculados por proyecto
        var ratiosPorProyecto = await ctx.SsRatioProyecto
            .GroupBy(r => r.ProjectId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        // Se muestran todos los que tengan consumo O estén habilitados para SSOMA — no se
        // oculta ninguno, "habilitado" solo se marca como dato visible (badge) para identificarlos.
        var idsAMostrar = proyectosConConsumo.Union(proyectosHabilitados).ToList();

        var proyectos = await ctx.Project
            .Where(p => idsAMostrar.Contains(p.ProjectId))
            .OrderBy(p => p.ProjectDescription)
            .ToListAsync();

        return proyectos.Select(p =>
        {
            var trabInt = ParseTrab(p.CantTrabajadoresCasa);
            return new DriverProyectoDto
            {
                ProjectId          = p.ProjectId,
                ProjectDescription = p.ProjectDescription ?? "",
                Estado             = p.Estado ?? "Inactivo",
                HhTotalCasa        = p.HhTotalCasa,
                AreaTechadaM2      = p.AreaTechadaM2,
                Trabajadores       = trabInt,
                HhFuente           = p.HhFuente ?? "HH_REAL",
                FamiliasConRatio   = ratiosPorProyecto.GetValueOrDefault(p.ProjectId, 0),
                TieneConsumos      = proyectosConConsumo.Contains(p.ProjectId),
                HabilitadoSsoma    = proyectosHabilitados.Contains(p.ProjectId)
            };
        }).ToList();
    }

    public async Task<ActualizarDriversResultDto> ActualizarYRecalcularAsync(int projectId, ActualizarDriversDto dto)
    {
        using var ctx = _factory.CreateDbContext();
        var proyecto = await ctx.Project.FindAsync(projectId)
            ?? throw new AbrilException("Proyecto no encontrado.", 404);

        // Guardar nuevos valores
        proyecto.HhTotalCasa          = dto.HhTotalCasa;
        proyecto.AreaTechadaM2        = dto.AreaTechadaM2;
        proyecto.CantTrabajadoresCasa = dto.Trabajadores.ToString();
        proyecto.HhFuente             = dto.HhFuente;
        await ctx.SaveChangesAsync();

        if (!dto.RecalcularRatios)
            return new ActualizarDriversResultDto
            {
                ProjectId          = projectId,
                ProjectDescription = proyecto.ProjectDescription ?? "",
                HhFuente           = dto.HhFuente,
                RatiosActualizados = 0
            };

        // Recalcular ratios con los nuevos drivers
        var resultado = await _ratioService.CalcularRatiosProyectoAsync(projectId);

        return new ActualizarDriversResultDto
        {
            ProjectId          = projectId,
            ProjectDescription = proyecto.ProjectDescription ?? "",
            HhFuente           = dto.HhFuente,
            RatiosActualizados = resultado.RatiosCalculados,
            FamiliasSinDriver  = resultado.FamiliasSinDriver,
            Advertencias       = resultado.Advertencias
        };
    }

    private static int? ParseTrab(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var clean = new string(s.Where(char.IsDigit).ToArray());
        return int.TryParse(clean, out var v) ? v : null;
    }
}

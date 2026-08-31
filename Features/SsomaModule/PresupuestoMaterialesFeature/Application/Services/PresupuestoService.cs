using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Services;

public class PresupuestoService : IPresupuestoService
{
    private readonly IPresupuestoRepository _repo;
    private readonly IDbContextFactory<AppDbContext> _factory;

    public PresupuestoService(IPresupuestoRepository repo, IDbContextFactory<AppDbContext> factory)
    {
        _repo    = repo;
        _factory = factory;
    }

    public async Task<PresupuestoDetalleDto> GenerarAsync(int projectId, GenerarPresupuestoDto dto, int? userId)
    {
        using var ctx = _factory.CreateDbContext();
        var proyecto = await ctx.Project.FindAsync(projectId)
            ?? throw new AbrilException("Proyecto no encontrado.", 404);

        // Drivers: usar override si viene en request, si no los del proyecto
        var hh    = dto.HhTotalCasa   ?? proyecto.HhTotalCasa   ?? 0;
        var area  = dto.AreaTechadaM2 ?? proyecto.AreaTechadaM2 ?? 0;
        var trab  = dto.Trabajadores  ?? ParseTrab(proyecto.CantTrabajadoresCasa);

        if (hh == 0 && area == 0)
            throw new AbrilException(
                "El proyecto no tiene HH ni Área Techada configurados. Actualice los drivers primero.", 400);

        // Ratios recomendados de todos los proyectos históricos
        var ratios = await _repo.ObtenerRatiosRecomendadosAsync();

        // Calcular líneas de presupuesto
        var lineas = ratios.Select(r =>
        {
            var driver   = ObtenerDriver(r.VariableBase, hh, area, (decimal)trab);
            var cantidad = driver > 0 ? Math.Round(r.RatioRecomendado * driver, 4) : 0;
            var total    = Math.Round(cantidad * r.PrecioRecomendado, 2);

            return new PresupuestoLineaDto
            {
                FamiliaId        = r.FamiliaId,
                NombreFamilia    = r.NombreFamilia,
                TipoId           = r.TipoId,
                NombreTipo       = r.NombreTipo,
                VariableBase     = r.VariableBase,
                RatioRecomendado = r.RatioRecomendado,
                NProyectosBase   = (int)r.NProyectos,
                ValorDriver      = driver,
                CantidadEstimada = cantidad,
                PrecioUnitario   = r.PrecioRecomendado,
                TotalEstimado    = total,
                TieneHistoria    = r.NProyectos > 0
            };
        }).ToList();

        var totalPresupuesto = lineas.Sum(l => l.TotalEstimado);
        var version          = await _repo.SiguienteVersionAsync(projectId);

        var presupuestoId = await _repo.CrearPresupuestoAsync(
            projectId, version, hh, area, trab, totalPresupuesto, userId, dto.Notas);

        await _repo.InsertarLineasAsync(presupuestoId, lineas);

        return (await _repo.ObtenerDetalleAsync(presupuestoId))!;
    }

    public Task<PresupuestoDetalleDto?> ObtenerDetalleAsync(int presupuestoId) =>
        _repo.ObtenerDetalleAsync(presupuestoId);

    public Task<List<PresupuestoResumenDto>> ObtenerPorProyectoAsync(int projectId) =>
        _repo.ObtenerPorProyectoAsync(projectId);

    public async Task<PresupuestoDetalleDto> ActualizarLineaAsync(
        int presupuestoId, int lineaId, ActualizarLineaPresupuestoDto dto)
    {
        await _repo.ActualizarLineaAsync(lineaId, dto.CantidadManual, dto.PrecioManual, dto.NotasLinea);
        return (await _repo.ObtenerDetalleAsync(presupuestoId))!;
    }

    public async Task<string> AprobarAsync(int presupuestoId) =>
        await _repo.AprobarAsync(presupuestoId);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static decimal ObtenerDriver(string variableBase, decimal hh, decimal area, decimal trab) =>
        variableBase switch
        {
            "HH"           => hh,
            "AREATECHADA"  => area,
            "TRABAJADORES" => trab,
            _              => 1   // CALCULADO / FIJO / METRADO → ratio = cantidad absoluta
        };

    private static int ParseTrab(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;
        var clean = new string(s.Where(char.IsDigit).ToArray());
        return int.TryParse(clean, out var v) ? v : 0;
    }
}

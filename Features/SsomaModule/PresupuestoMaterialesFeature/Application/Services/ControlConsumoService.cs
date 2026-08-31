using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Services;

public class ControlConsumoService : IControlConsumoService
{
    private readonly IControlConsumoRepository _repo;
    private readonly IDbContextFactory<AppDbContext> _factory;

    public ControlConsumoService(IControlConsumoRepository repo, IDbContextFactory<AppDbContext> factory)
    {
        _repo    = repo;
        _factory = factory;
    }

    public async Task<ControlSemanaDto> AbrirSemanaAsync(AbrirSemanaDto dto, int? userId)
    {
        // Verificar que el presupuesto existe y está aprobado
        using var ctx = _factory.CreateDbContext();
        var presupuesto = await ctx.SsPresupuesto.FindAsync(dto.PresupuestoId)
            ?? throw new AbrilException("Presupuesto no encontrado.", 404);

        if (presupuesto.Estado != "APROBADO")
            throw new AbrilException("El presupuesto debe estar APROBADO antes de registrar consumo.", 400);

        var semanaNum = await _repo.SiguienteSemanaNumAsync(dto.PresupuestoId);
        var controlId = await _repo.CrearSemanaAsync(
            dto.PresupuestoId, presupuesto.ProjectId, semanaNum,
            dto.FechaInicio, dto.FechaFin, dto.Observaciones, userId);

        return (await _repo.ObtenerSemanaAsync(controlId))!;
    }

    public async Task<ControlSemanaDto> RegistrarConsumoAsync(int controlId, List<RegistrarConsumoLineaDto> lineas)
    {
        using var ctx = _factory.CreateDbContext();
        var semana = await ctx.SsControlSemana.FindAsync(controlId)
            ?? throw new AbrilException("Semana de control no encontrada.", 404);

        if (semana.Estado == "CERRADO")
            throw new AbrilException("No se puede modificar una semana cerrada.", 400);

        await _repo.UpsertLineasAsync(controlId, lineas);
        return (await _repo.ObtenerSemanaAsync(controlId))!;
    }

    public async Task<ControlSemanaDto> CerrarSemanaAsync(int controlId)
    {
        using var ctx = _factory.CreateDbContext();
        var semana = await ctx.SsControlSemana.FindAsync(controlId)
            ?? throw new AbrilException("Semana de control no encontrada.", 404);

        if (semana.Estado == "CERRADO")
            throw new AbrilException("La semana ya está cerrada.", 400);

        await _repo.CerrarSemanaAsync(controlId);
        return (await _repo.ObtenerSemanaAsync(controlId))!;
    }

    public Task<ControlSemanaDto?> ObtenerSemanaAsync(int controlId) =>
        _repo.ObtenerSemanaAsync(controlId);

    public Task<List<ControlSemanaDto>> ListarSemanasAsync(int presupuestoId) =>
        _repo.ListarSemanasPorPresupuestoAsync(presupuestoId);

    public Task<DashboardPresupuestoDto?> ObtenerDashboardAsync(int presupuestoId) =>
        _repo.ObtenerDashboardAsync(presupuestoId);
}

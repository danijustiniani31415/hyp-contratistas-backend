using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;

public interface IControlConsumoRepository
{
    Task<int>  SiguienteSemanaNumAsync(int presupuestoId);
    Task<int>  CrearSemanaAsync(int presupuestoId, int projectId, int semanaNum,
                   DateOnly fechaInicio, DateOnly fechaFin, string? obs, int? userId);
    Task       UpsertLineasAsync(int controlId, IEnumerable<RegistrarConsumoLineaDto> lineas);
    Task       CerrarSemanaAsync(int controlId);
    Task<ControlSemanaDto?> ObtenerSemanaAsync(int controlId);
    Task<List<ControlSemanaDto>> ListarSemanasPorPresupuestoAsync(int presupuestoId);
    Task<DashboardPresupuestoDto?> ObtenerDashboardAsync(int presupuestoId);
}

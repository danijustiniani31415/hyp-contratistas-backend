using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;

public interface IControlConsumoService
{
    Task<ControlSemanaDto> AbrirSemanaAsync(AbrirSemanaDto dto, int? userId);
    Task<ControlSemanaDto> RegistrarConsumoAsync(int controlId, List<RegistrarConsumoLineaDto> lineas);
    Task<ControlSemanaDto> CerrarSemanaAsync(int controlId);
    Task<ControlSemanaDto?> ObtenerSemanaAsync(int controlId);
    Task<List<ControlSemanaDto>> ListarSemanasAsync(int presupuestoId);
    Task<DashboardPresupuestoDto?> ObtenerDashboardAsync(int presupuestoId);
}

using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;

public interface IPresupuestoService
{
    Task<PresupuestoDetalleDto> GenerarAsync(int projectId, GenerarPresupuestoDto dto, int? userId);
    Task<PresupuestoDetalleDto?> ObtenerDetalleAsync(int presupuestoId);
    Task<List<PresupuestoResumenDto>> ObtenerPorProyectoAsync(int projectId);
    Task<PresupuestoDetalleDto> ActualizarLineaAsync(int presupuestoId, int lineaId, ActualizarLineaPresupuestoDto dto);
    Task<string> AprobarAsync(int presupuestoId);
}

using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;

public interface IDriversService
{
    Task<List<DriverProyectoDto>> ObtenerTodosAsync();
    Task<ActualizarDriversResultDto> ActualizarYRecalcularAsync(int projectId, ActualizarDriversDto dto);
}

using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Microsoft.AspNetCore.Http;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;

public interface IConsumoService
{
    Task<ImportConsumoResultDto> ImportarS10Async(IFormFile archivo, int projectId, int usuarioId);
    Task<List<ConsumoCargaResumenDto>> ObtenerCargasAsync(int projectId);
    Task<int> AsignarHitosAsync(int projectId);
}

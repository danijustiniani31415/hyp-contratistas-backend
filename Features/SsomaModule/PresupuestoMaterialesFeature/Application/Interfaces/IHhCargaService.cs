using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Microsoft.AspNetCore.Http;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;

public interface IHhCargaService
{
    Task<ImportHhResultDto> ImportarHhAsync(IFormFile archivo, int projectId, int usuarioId);
    Task<List<HhCargaResumenDto>> ObtenerCargasAsync(int projectId);
}

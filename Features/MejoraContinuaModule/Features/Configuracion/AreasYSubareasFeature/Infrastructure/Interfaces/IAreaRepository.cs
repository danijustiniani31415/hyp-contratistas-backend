using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Application.Dtos;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Infrastructure.Interfaces
{
    public interface IAreaRepository
    {
        Task<object> GetPaged(int page);
        Task<List<AreaSimpleDTO>> GetAllSimple();
        Task CreateAsync(AreaCreateDTO dto, int userId);
        Task UpdateAsync(AreaEditDTO dto, int userId);
        Task<bool> DeleteSoftAsync(int areaId, int userId);
    }
}

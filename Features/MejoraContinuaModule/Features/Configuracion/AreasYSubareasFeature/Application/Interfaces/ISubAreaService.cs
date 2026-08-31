using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Application.Dtos;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Application.Interfaces
{
    public interface ISubAreaService
    {
        Task<object> GetPaged(int page, int? areaId);
        Task<List<SubAreaSimpleDTO>> GetSimpleByAreaAsync(int areaId);
        Task<List<SubAreaSimpleDTO>> GetAllSimpleAsync();
        Task CreateAsync(SubAreaCreateDTO dto, int userId);
        Task UpdateAsync(SubAreaEditDTO dto, int userId);
        Task<bool> DeleteSoftAsync(int subAreaId, int userId);
        Task<bool> AreaHasScopeAsync(int areaId);
    }
}

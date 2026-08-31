using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Application.Dtos;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Application.Interfaces;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Application.Services
{
    public class SubAreaService : ISubAreaService
    {
        private readonly ISubAreaRepository _repository;

        public SubAreaService(ISubAreaRepository repository)
        {
            _repository = repository;
        }

        public Task<object> GetPaged(int page, int? areaId) => _repository.GetPaged(page, areaId);
        public Task<List<SubAreaSimpleDTO>> GetSimpleByAreaAsync(int areaId) => _repository.GetSimpleByAreaAsync(areaId);
        public Task<List<SubAreaSimpleDTO>> GetAllSimpleAsync() => _repository.GetAllSimpleAsync();
        public Task CreateAsync(SubAreaCreateDTO dto, int userId) => _repository.CreateAsync(dto, userId);
        public Task UpdateAsync(SubAreaEditDTO dto, int userId) => _repository.UpdateAsync(dto, userId);
        public Task<bool> DeleteSoftAsync(int subAreaId, int userId) => _repository.DeleteSoftAsync(subAreaId, userId);
        public Task<bool> AreaHasScopeAsync(int areaId) => _repository.AreaHasScopeAsync(areaId);
    }
}

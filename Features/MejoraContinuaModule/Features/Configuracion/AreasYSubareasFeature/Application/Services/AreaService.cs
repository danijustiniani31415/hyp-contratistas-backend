using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Application.Dtos;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Application.Interfaces;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Application.Services
{
    public class AreaService : IAreaService
    {
        private readonly IAreaRepository _repository;

        public AreaService(IAreaRepository repository)
        {
            _repository = repository;
        }

        public Task<object> GetPaged(int page) => _repository.GetPaged(page);
        public Task<List<AreaSimpleDTO>> GetAllSimple() => _repository.GetAllSimple();
        public Task CreateAsync(AreaCreateDTO dto, int userId) => _repository.CreateAsync(dto, userId);
        public Task UpdateAsync(AreaEditDTO dto, int userId) => _repository.UpdateAsync(dto, userId);
        public Task<bool> DeleteSoftAsync(int areaId, int userId) => _repository.DeleteSoftAsync(areaId, userId);
    }
}

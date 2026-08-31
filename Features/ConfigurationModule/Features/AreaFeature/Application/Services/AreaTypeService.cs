using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Application.Dtos;
using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Application.Interfaces;
using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Application.Services
{
    public class AreaTypeService : IAreaTypeService
    {
        private readonly IAreaTypeRepository _repository;
        public AreaTypeService(IAreaTypeRepository repository) => _repository = repository;

        public Task<PagedResult<AreaTypeDto>> GetPaged(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            return _repository.GetPaged(page, pageSize);
        }

        public Task<List<AreaTypeSimpleDto>> GetSimple() => _repository.GetSimple();
        public Task Create(AreaTypeCreateDto dto) => _repository.Create(dto);
        public Task Update(AreaTypeEditDto dto) => _repository.Update(dto);
        public Task<bool> DeleteSoftAsync(int areaTypeId) => _repository.DeleteSoftAsync(areaTypeId);
    }
}

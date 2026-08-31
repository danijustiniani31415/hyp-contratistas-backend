using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Application.Dtos;
using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Application.Interfaces;
using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Application.Services
{
    public class AreaItemService : IAreaItemService
    {
        private readonly IAreaItemRepository _repository;
        public AreaItemService(IAreaItemRepository repository) => _repository = repository;

        public Task<PagedResult<AreaItemDto>> GetPaged(AreaItemFilterDto filter)
        {
            if (filter.Page < 1) filter.Page = 1;
            if (filter.PageSize < 1) filter.PageSize = 10;
            return _repository.GetPaged(filter);
        }

        public Task<List<AreaItemSimpleDto>> GetSimple(int? areaTypeId) => _repository.GetSimple(areaTypeId);
        public Task Create(AreaItemCreateDto dto) => _repository.Create(dto);
        public Task Update(AreaItemEditDto dto) => _repository.Update(dto);
        public Task<bool> DeleteSoftAsync(int areaItemId) => _repository.DeleteSoftAsync(areaItemId);
    }
}

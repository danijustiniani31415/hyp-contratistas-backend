using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Application.Dtos;

namespace Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Infrastructure.Interfaces
{
    public interface IAreaItemRepository
    {
        Task<PagedResult<AreaItemDto>> GetPaged(AreaItemFilterDto filter);
        Task<List<AreaItemSimpleDto>> GetSimple(int? areaTypeId);
        Task Create(AreaItemCreateDto dto);
        Task Update(AreaItemEditDto dto);
        Task<bool> DeleteSoftAsync(int areaItemId);
    }
}

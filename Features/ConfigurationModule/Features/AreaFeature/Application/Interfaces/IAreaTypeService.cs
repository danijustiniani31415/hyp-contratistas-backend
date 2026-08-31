using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Application.Dtos;

namespace Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Application.Interfaces
{
    public interface IAreaTypeService
    {
        Task<PagedResult<AreaTypeDto>> GetPaged(int page, int pageSize);
        Task<List<AreaTypeSimpleDto>> GetSimple();
        Task Create(AreaTypeCreateDto dto);
        Task Update(AreaTypeEditDto dto);
        Task<bool> DeleteSoftAsync(int areaTypeId);
    }
}

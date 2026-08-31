using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkSpecialtyFeature.Application.Dtos;

namespace Abril_Backend.Features.CostsModule.Features.Configuration.WorkSpecialtyFeature.Application.Interfaces
{
    public interface IWorkSpecialtyService
    {
        Task<PagedResult<WorkSpecialtyDto>> GetPaged(WorkSpecialtyFilterDto filter);
        Task Create(WorkSpecialtyCreateDto dto, int userId);
        Task Update(WorkSpecialtyEditDto dto, int userId);
        Task<bool> Delete(int workSpecialtyId, int userId);
    }
}

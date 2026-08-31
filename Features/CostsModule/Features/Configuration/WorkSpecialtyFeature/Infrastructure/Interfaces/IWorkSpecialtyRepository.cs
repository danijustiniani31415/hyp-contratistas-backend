using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkSpecialtyFeature.Application.Dtos;

namespace Abril_Backend.Features.CostsModule.Features.Configuration.WorkSpecialtyFeature.Infrastructure.Interfaces
{
    public interface IWorkSpecialtyRepository
    {
        Task<PagedResult<WorkSpecialtyDto>> GetPaged(WorkSpecialtyFilterDto filter);
        Task Create(WorkSpecialtyCreateDto dto, int userId);
        Task Update(WorkSpecialtyEditDto dto, int userId);
        Task<bool> Delete(int workSpecialtyId, int userId);
    }
}

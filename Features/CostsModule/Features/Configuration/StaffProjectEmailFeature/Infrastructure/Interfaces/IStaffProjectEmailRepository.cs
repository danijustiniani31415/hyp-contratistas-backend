using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.CostsModule.Features.Configuration.StaffProjectEmailFeature.Application.Dtos;

namespace Abril_Backend.Features.CostsModule.Features.Configuration.StaffProjectEmailFeature.Infrastructure.Interfaces
{
    public interface IStaffProjectEmailRepository
    {
        Task<List<StaffProjectEmailTypeDto>> GetTypesFactory();
        Task<PagedResult<StaffProjectEmailDto>> GetPaged(StaffProjectEmailFilterDto filter);
        Task Create(StaffProjectEmailCreateDto dto, int userId);
        Task Update(StaffProjectEmailEditDto dto, int userId);
        Task<bool> Delete(int staffProjectEmailId, int userId);
    }
}

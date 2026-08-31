using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.ConfigurationModule.Features.HolidayFeature.Application.Dtos;

namespace Abril_Backend.Features.ConfigurationModule.Features.HolidayFeature.Infrastructure.Interfaces
{
    public interface IHolidayRepository
    {
        Task<List<HolidayTypeSimpleDto>> GetTypes();
        Task<PagedResult<HolidayDto>> GetPaged(int page, int pageSize);
        Task Create(HolidayCreateDto dto);
        Task Update(HolidayEditDto dto);
        Task<bool> DeleteSoftAsync(int holidayId);
    }
}

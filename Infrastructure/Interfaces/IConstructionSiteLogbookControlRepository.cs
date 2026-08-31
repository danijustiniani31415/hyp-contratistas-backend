using Abril_Backend.Application.DTOs;

namespace Abril_Backend.Infrastructure.Interfaces
{
    public interface IConstructionSiteLogbookControlRepository
    {
        Task<bool> Create(ConstructionSiteLogbookControlCreateDTO dto, List<string> fileUrls, int userId, List<string> fileDescriptions);
        Task<PagedResult<ConstructionSiteLogbookControlGetDTO>> GetPaged(int page, DateOnly? periodDate, int? userId);
        Task<int> CountByScheduleAndPeriod(int scheduleId, DateOnly periodDate);
        Task<List<DateOnly>> GetIvtControlPeriods();
    }
}
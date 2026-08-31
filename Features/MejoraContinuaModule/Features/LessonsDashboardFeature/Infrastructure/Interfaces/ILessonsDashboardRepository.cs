using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsDashboardFeature.Application.Dtos;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.LessonsDashboardFeature.Infrastructure.Interfaces
{
    public interface ILessonsDashboardRepository
    {
        Task<LessonsDashboardDataDTO> GetDataAsync(DateTimeOffset? periodDate, int? userId, List<int>? lessonAreaIds, int? obraOficinaStaffId, List<int>? projectIds, string? approvalStatus);
        Task<LessonsDashboardFiltersDTO> GetFiltersAsync();
    }
}

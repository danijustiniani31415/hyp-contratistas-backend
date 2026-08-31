using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsDashboardFeature.Application.Dtos;
using Microsoft.AspNetCore.Http;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.LessonsDashboardFeature.Application.Interfaces
{
    public interface ILessonsDashboardService
    {
        Task<LessonsDashboardDataDTO> GetData(DateTimeOffset? periodDate, int? userId, List<int>? lessonAreaIds, int? obraOficinaStaffId, List<int>? projectIds, string? approvalStatus);
        Task<LessonsDashboardFiltersDTO> GetFilters();
        Task SendPdfAsync(IFormFile pdf);
    }
}

using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Dtos;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Infrastructure.Interfaces
{
    public interface ILessonRepository
    {
        Task<LessonDetailDTO?> GetByIdAsync(int id, int currentUserId);

        Task<PagedResult<LessonListDTO>> GetLessonsFilterPaged(
            DateTimeOffset? periodDate,
            int? stateId,
            int? projectId,
            int? areaId,
            int? userId,
            int page,
            int pageSize);

        Task<LessonsPagedWithFiltersDTO> GetPagedWithFiltersAsync(LessonFilterDTO filter);

        Task<List<LessonListDTO>> GetLessonsFilterAsync(
            string? period,
            int? stateId,
            int? projectId,
            int? areaId,
            int? userId,
            List<int>? lessonAreaIds = null,
            int? obraOficinaStaffId = null);

        Task<object?> CreateAsync(LessonCreateDTO dto, int userId);
        Task<bool> UpdateAsync(int lessonId, LessonUpdateDTO dto, int currentUserId);
        Task<bool> DeleteSoftAsync(int lessonId, int userId);

        Task<LessonReviewResultDTO> ApproveAsync(int lessonId, int currentUserId);
        Task<LessonReviewResultDTO> RejectAsync(int lessonId, int currentUserId, string? comment);

        /// <summary>
        /// Feriados/días no laborables (vivos y activos) del año/mes indicado,
        /// resolviendo los recurrentes al año pedido. Se usan para calcular la
        /// ventana de revisión de la jefatura (días hábiles de fin de mes).
        /// </summary>
        Task<HashSet<DateOnly>> GetHolidayDatesAsync(int year, int month);
    }
}

using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Dtos;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Interfaces
{
    public interface ILessonService
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

        Task<LessonsPagedWithFiltersDTO> GetPagedWithFilters(LessonFilterDTO filter);

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

        Task ApproveAsync(int lessonId, int currentUserId);
        Task RejectAsync(int lessonId, int currentUserId, string? comment);

        /// <summary>
        /// Estado de la ventana de subida para hoy (hora Lima): permite al frontend
        /// deshabilitar el botón "Nuevo registro" durante la ventana de revisión.
        /// </summary>
        Task<LessonUploadWindowDTO> GetUploadWindowAsync();
    }
}

using Abril_Backend.Application.DTOs;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Dtos
{
    public class LessonsPagedWithFiltersDTO
    {
        public PagedResult<LessonListDTO> Paged { get; set; } = null!;
        public LessonFiltersFormDataDTO Filters { get; set; } = null!;
    }
}

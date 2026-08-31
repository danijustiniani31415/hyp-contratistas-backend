using Abril_Backend.Application.DTOs;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Dtos
{
    /// <summary>
    /// DTO legacy de filtros. Solo conserva proyecto/área porque las tablas
    /// phase/stage/layer/sub_stage/sub_specialty/partida fueron eliminadas.
    /// </summary>
    public class LessonFiltersDTO
    {
        public List<ProjectDTO> Projects { get; set; } = new();
        public List<AreaDTO> Areas { get; set; } = new();
    }
}

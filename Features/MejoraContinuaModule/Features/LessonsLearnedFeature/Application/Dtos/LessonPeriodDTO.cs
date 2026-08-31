namespace Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Dtos
{
    /// <summary>
    /// Período (mes/año) en formato fecha. Copia del DTO en
    /// Abril_Backend.Application.DTOs.LessonPeriodDTO — vive aquí para que la
    /// feature LessonsLearned sea autosuficiente, y la copia legacy se mantiene
    /// porque la feature UnidadDeProyectos.LessonsLearnedDashboard también la usa.
    /// </summary>
    public class LessonPeriodDTO
    {
        public DateTimeOffset? PeriodDate { get; set; }
    }
}

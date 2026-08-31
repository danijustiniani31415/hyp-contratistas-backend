namespace Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Dtos
{
    /// <summary>
    /// Datos del autor que devuelve el repo tras aprobar/rechazar, para que el
    /// service le envíe el correo de notificación.
    /// </summary>
    public class LessonReviewResultDTO
    {
        public int LessonId { get; set; }
        public string? LessonCode { get; set; }
        public string? CreatorEmail { get; set; }
        public string? CreatorFullName { get; set; }
    }
}

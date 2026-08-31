namespace Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Dtos
{
    /// <summary>
    /// Estado de la ventana de subida de lecciones aprendidas para "hoy" (hora Lima).
    /// El frontend lo usa para deshabilitar el botón "Nuevo registro" durante la
    /// ventana de revisión de la jefatura.
    /// </summary>
    public class LessonUploadWindowDTO
    {
        /// <summary>true si hoy se pueden registrar lecciones (no es ventana de revisión).</summary>
        public bool CanUpload { get; set; }

        /// <summary>true si hoy cae en la ventana de revisión de la jefatura (subida bloqueada).</summary>
        public bool IsReviewWindow { get; set; }

        /// <summary>Inicio (4.º último día hábil) de la ventana de revisión del mes actual.</summary>
        public DateOnly? ReviewStart { get; set; }

        /// <summary>Fin (último día hábil) de la ventana de revisión del mes actual.</summary>
        public DateOnly? ReviewEnd { get; set; }

        /// <summary>Mensaje a mostrar cuando la subida está bloqueada (null si está habilitada).</summary>
        public string? Message { get; set; }
    }
}

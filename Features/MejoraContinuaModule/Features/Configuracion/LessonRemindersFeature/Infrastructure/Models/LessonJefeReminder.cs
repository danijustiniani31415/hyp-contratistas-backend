namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonRemindersFeature.Infrastructure.Models
{
    /// <summary>
    /// Jefaturas (worker.categoria = 'Jefe') que reciben el recordatorio del 4.º
    /// día hábil de revisión de Lecciones Aprendidas.
    ///   • Sin fila para el worker  → NO recibe.
    ///   • Fila con active = false   → NO recibe.
    ///   • Fila con active = true    → SÍ recibe.
    /// state = soft-delete: pueden existir varias filas con state=false pero solo
    /// una con state=true por worker (índice único ux_lesson_jefe_reminder_worker_alive).
    /// Apunta a workers.id.
    /// </summary>
    public class LessonJefeReminder
    {
        public int LessonJefeReminderId { get; set; }
        public int WorkerId { get; set; }
        public bool Active { get; set; } = false;
        public bool State { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}

namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonRemindersFeature.Infrastructure.Models
{
    /// <summary>
    /// Tabla filtro que decide qué proyectos (project.staff_email) reciben los
    /// recordatorios mensuales de Lecciones Aprendidas. active = true significa que
    /// el staff_email del proyecto recibirá el correo.
    /// </summary>
    public class ProjectStaffReminder
    {
        public int ProjectStaffReminderId { get; set; }
        public int ProjectId { get; set; }
        public bool Active { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}

namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonRemindersFeature.Application.Dtos
{
    /// <summary>Opcion del filtro Obra / Staff / Oficina Central (workers_obra_oficina_staff).</summary>
    public class LessonReminderObraOficinaDTO
    {
        public int ObraOficinaStaffId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class LessonReminderDTO
    {
        public int UserProjectId { get; set; }
        public int WorkerId { get; set; }
        public string? WorkerFullName { get; set; }
        public string? Email { get; set; } // worker.email_corporativo
        public int ProjectId { get; set; }
        public string? ProjectDescription { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
    }

    public class LessonReminderCreateDTO
    {
        public int WorkerId { get; set; }
        public int ProjectId { get; set; }
        public bool Active { get; set; }
    }

    public class ToggleLessonReminderResultDTO
    {
        public int UserProjectId { get; set; }
        public bool Active { get; set; }
    }

    /// <summary>
    /// Cambia el proyecto de un recordatorio existente. El trabajador NO se puede
    /// modificar; solo se reasigna el proyecto.
    /// </summary>
    public class LessonReminderUpdateProjectDTO
    {
        public int ProjectId { get; set; }
    }

    public class LessonReminderCreateDataDTO
    {
        public List<LessonReminderWorkerDTO> Workers { get; set; } = new();
        public List<LessonReminderProjectDTO> Projects { get; set; } = new();
    }

    public class LessonReminderWorkerDTO
    {
        public int WorkerId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; } // worker.email_corporativo (@abril.pe)
    }

    public class LessonReminderProjectDTO
    {
        public int ProjectId { get; set; }
        public string? ProjectDescription { get; set; }
    }

    /// <summary>
    /// Una fila por proyecto que tiene staff_email registrado.
    /// Refleja el toggle de project_staff_reminder.active.
    /// </summary>
    public class ProjectStaffReminderConfigItemDTO
    {
        public int? ProjectStaffReminderId { get; set; } // null si todavía no se ha togglado
        public int ProjectId { get; set; }
        public string? ProjectDescription { get; set; }
        public string? StaffEmail { get; set; }
        public bool Active { get; set; }
    }

    public class ToggleProjectStaffReminderResultDTO
    {
        public int ProjectStaffReminderId { get; set; }
        public bool Active { get; set; }
    }

    /// <summary>Proyecto + email staff activo. Usado por el cron de recordatorios.</summary>
    public class ActiveProjectStaffEmailDTO
    {
        public int ProjectId { get; set; }
        public string ProjectDescription { get; set; } = string.Empty;
        public string StaffEmail { get; set; } = string.Empty;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Jefaturas (lesson_jefe_reminder) — recordatorio del 4.º día
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Una fila por trabajador con categoria='Jefe'. Refleja el toggle de
    /// lesson_jefe_reminder.active (false / sin fila = no recibe el recordatorio).
    /// </summary>
    public class JefeReminderConfigItemDTO
    {
        public int? LessonJefeReminderId { get; set; } // null si todavía no se ha activado nunca
        public int WorkerId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Categoria { get; set; } // Jefe | Coordinador | Residente
        public bool Active { get; set; }
    }

    public class ToggleJefeReminderResultDTO
    {
        public int LessonJefeReminderId { get; set; }
        public bool Active { get; set; }
    }

    /// <summary>
    /// Jefatura activa (active=true, con correo) + cuántas lecciones de su equipo
    /// (subordinados) están PENDIENTE de revisión. Usado por el aviso del 4.º día:
    /// si PendingCount = 0 se envía un correo con contenido distinto.
    /// </summary>
    public class JefeReviewStatusDTO
    {
        public int WorkerId { get; set; }
        public string? FullName { get; set; }
        public string Email { get; set; } = string.Empty;
        public int PendingCount { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Revisor de Trabajadores (workers.worker_lesson_jefe_id)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Una fila por trabajador con email_corporativo @abril.pe, junto a su jefe
    /// directo (workers.worker_lesson_jefe_id) encargado de revisar sus lecciones.
    /// </summary>
    public class WorkerRevisorItemDTO
    {
        public int WorkerId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public int? CategoryId { get; set; }
        public string? Category { get; set; }
        public int? JefeWorkerId { get; set; }
        public string? JefeFullName { get; set; }
        public string? JefeEmail { get; set; }
        public int? JefeCategoryId { get; set; }
        public string? JefeCategory { get; set; }
        public bool AutoApproveLesson { get; set; }
    }

    /// <summary>Opción del selector de jefe: cualquier worker.</summary>
    public class WorkerRevisorOptionDTO
    {
        public int WorkerId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
    }

    public class WorkerRevisorUpdateDTO
    {
        public int? JefeWorkerId { get; set; }
    }

    public class ToggleAutoApproveLessonResultDTO
    {
        public int WorkerId { get; set; }
        public bool AutoApproveLesson { get; set; }
    }

    /// <summary>
    /// Miembro de un grupo de correos (staff_email expandido) que está pendiente
    /// de subir su lección aprendida del mes para un proyecto específico.
    ///   • Si UserId != null: el correo está en la tabla user; FullName puede venir.
    ///   • Si UserId == null: el correo no existe como user en el sistema;
    ///     se considera pendiente porque no hay cómo verificar.
    /// </summary>
    public class PendingStaffMemberDTO
    {
        public string Email { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public string? FullName { get; set; }
    }
}

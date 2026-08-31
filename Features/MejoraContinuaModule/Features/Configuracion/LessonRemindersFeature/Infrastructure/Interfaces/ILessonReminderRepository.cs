using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonRemindersFeature.Application.Dtos;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonRemindersFeature.Infrastructure.Interfaces
{
    public interface ILessonReminderRepository
    {
        Task<object> GetPaged(int page, int pageSize, string? subarea = null, int? workerId = null, bool includeWorkers = false, int? obraOficinaStaffId = null);
        Task<LessonReminderCreateDataDTO> GetCreateData();
        Task Create(LessonReminderCreateDTO dto, int userId);
        Task UpdateProjectAsync(int userProjectId, int newProjectId, int userId);
        Task<bool> DeleteSoftAsync(int userProjectId, int userId);
        Task<ToggleLessonReminderResultDTO> ToggleActiveAsync(int userProjectId, int userId);

        /// <summary>
        /// Fechas que caen como feriado/día no laborable dentro del mes indicado.
        /// Los registros recurrentes (recurring_yearly=true, ej. feriados) se resuelven
        /// al año solicitado por mes/día; los no recurrentes solo aplican a su fecha
        /// exacta. Usado por el cron de recordatorios para excluir feriados del cálculo
        /// de días hábiles.
        /// </summary>
        Task<HashSet<DateOnly>> GetHolidayDatesAsync(int year, int month);

        // ── Recordatorios por correo (consumidos por ReminderService) ──────────
        /// <summary>
        /// Usuarios (con sus proyectos) asignados vía user_project que NO han subido
        /// lecciones en el período indicado (formato "MM-yyyy"). Filtra por
        /// user_project.state/active = true y ausencia de lesson(created_user_id,
        /// project_id, period, state, active).
        /// </summary>
        Task<List<UserWithoutLessonsDTO>> GetUsersWithoutLessonsThisMonth(string period);

        /// <summary>
        /// Igual que el anterior pero filtrando por period_date (primer día del mes
        /// de <paramref name="periodDate"/>). Usado para el reporte a supervisores.
        /// </summary>
        Task<List<UserWithoutLessonsDTO>> GetUsersWithoutLessonsByPeriod(DateTime periodDate);

        // Filtro de staff_email por proyecto
        Task<List<ProjectStaffReminderConfigItemDTO>> GetAllProjectStaffAsync();
        Task<ToggleProjectStaffReminderResultDTO> ToggleProjectStaffAsync(int projectId);
        Task<List<ActiveProjectStaffEmailDTO>> GetActiveStaffEmailsAsync();

        /// <summary>
        /// Dado un proyecto, un período (formato "MM-yyyy") y una lista de correos
        /// (ya expandidos desde un grupo), devuelve los miembros que NO han subido
        /// lección al proyecto en el período. Un correo que no existe en `user`
        /// también se devuelve como pendiente (no hay cómo verificar).
        /// </summary>
        Task<List<PendingStaffMemberDTO>> GetPendingMembersForProjectAsync(
            int projectId,
            string period,
            IReadOnlyList<string> emails);

        /// <summary>
        /// Correos corporativos (@abril) de los trabajadores que además tienen un
        /// usuario registrado en el sistema. El correo corporativo vive en
        /// worker.email_corporativo (la columna email_corporativo está en NULL); el
        /// enlace con usuario es worker.person_id → person.person_id → person.user_id
        /// → app_user.user_id. Devuelve la lista deduplicada (case-insensitive).
        /// </summary>
        Task<List<string>> GetAbrilWorkerEmailsWithUserAsync();

        /// <summary>
        /// Correos (worker.email_corporativo) de los trabajadores asignados a proyectos
        /// vía user_project con recordatorio vivo y activo (state=true, active=true),
        /// SIN filtrar por si subieron o no su lección. Es el CANAL 1 de la audiencia
        /// del recordatorio mensual de subida; lo usa el aviso de publicación para
        /// dirigirse exactamente a las mismas personas que reciben el recordatorio.
        /// Devuelto deduplicado case-insensitive.
        /// </summary>
        Task<List<string>> GetLessonReminderUserProjectEmailsAsync();

        // ── Jefaturas (lesson_jefe_reminder) — recordatorio del 4.º día ────────
        /// <summary>
        /// Todos los trabajadores con categoria='Jefe' + su fila viva (state=true)
        /// si existe. active=false cuando no hay fila → la UI los muestra inactivos.
        /// </summary>
        Task<List<JefeReminderConfigItemDTO>> GetAllJefesAsync();

        /// <summary>
        /// Alterna el envío para un jefe. Si no existe fila viva la crea con
        /// active=true (primera activación); si existe, invierte active.
        /// </summary>
        Task<ToggleJefeReminderResultDTO> ToggleJefeAsync(int workerId);

        /// <summary>
        /// Jefaturas con correo y active=true + cuántas lecciones de su equipo están
        /// PENDIENTE de revisión (0 = no tiene nada que revisar). Usado por el cron
        /// del 4.º día para decidir el contenido del correo.
        /// </summary>
        Task<List<JefeReviewStatusDTO>> GetActiveJefesReviewStatusAsync();

        // ── Revisor de Trabajadores (workers.worker_lesson_jefe_id) ────────────
        /// <summary>
        /// Trabajadores con email_corporativo @abril.pe + su jefe directo asignado
        /// (workers.worker_lesson_jefe_id), si lo tiene.
        /// </summary>
        Task<List<WorkerRevisorItemDTO>> GetWorkerRevisoresAsync();

        /// <summary>Todos los workers (con persona) como opciones del selector de jefe.</summary>
        Task<List<WorkerRevisorOptionDTO>> GetWorkerRevisorOptionsAsync();

        /// <summary>Asigna (o quita, con null) el jefe directo de un trabajador.</summary>
        Task UpdateWorkerRevisorAsync(int workerId, int? jefeWorkerId);

        /// <summary>Alterna auto_approve_lesson de un trabajador.</summary>
        Task<ToggleAutoApproveLessonResultDTO> ToggleAutoApproveLessonAsync(int workerId);
    }
}

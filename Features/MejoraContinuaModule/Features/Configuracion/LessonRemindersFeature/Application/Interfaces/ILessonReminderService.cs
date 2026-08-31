using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonRemindersFeature.Application.Dtos;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonRemindersFeature.Application.Interfaces
{
    public interface ILessonReminderService
    {
        Task<object> GetPaged(int page, int pageSize, string? subarea = null, int? workerId = null, bool includeWorkers = false, int? obraOficinaStaffId = null);
        Task<LessonReminderCreateDataDTO> GetCreateData();
        Task Create(LessonReminderCreateDTO dto, int userId);
        Task UpdateProjectAsync(int userProjectId, int newProjectId, int userId);
        Task<bool> DeleteSoftAsync(int userProjectId, int userId);
        Task<ToggleLessonReminderResultDTO> ToggleActiveAsync(int userProjectId, int userId);

        // Filtro de staff_email por proyecto
        Task<List<ProjectStaffReminderConfigItemDTO>> GetAllProjectStaffAsync();
        Task<ToggleProjectStaffReminderResultDTO> ToggleProjectStaffAsync(int projectId);
        Task<List<ActiveProjectStaffEmailDTO>> GetActiveStaffEmailsAsync();

        // Jefaturas (lesson_jefe_reminder) — recordatorio del 4.º día
        Task<List<JefeReminderConfigItemDTO>> GetAllJefesAsync();
        Task<ToggleJefeReminderResultDTO> ToggleJefeAsync(int workerId);
        Task<List<JefeReviewStatusDTO>> GetActiveJefesReviewStatusAsync();

        // Revisor de Trabajadores (workers.worker_lesson_jefe_id)
        Task<List<WorkerRevisorItemDTO>> GetWorkerRevisoresAsync();
        Task<List<WorkerRevisorOptionDTO>> GetWorkerRevisorOptionsAsync();
        Task UpdateWorkerRevisorAsync(int workerId, int? jefeWorkerId);
        Task<ToggleAutoApproveLessonResultDTO> ToggleAutoApproveLessonAsync(int workerId);
    }
}

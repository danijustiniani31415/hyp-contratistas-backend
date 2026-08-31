using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonRemindersFeature.Application.Dtos;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonRemindersFeature.Application.Interfaces;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonRemindersFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonRemindersFeature.Application.Services
{
    public class LessonReminderService : ILessonReminderService
    {
        private readonly ILessonReminderRepository _repository;

        public LessonReminderService(ILessonReminderRepository repository)
        {
            _repository = repository;
        }

        public Task<object> GetPaged(int page, int pageSize, string? subarea = null, int? workerId = null, bool includeWorkers = false, int? obraOficinaStaffId = null) => _repository.GetPaged(page, pageSize, subarea, workerId, includeWorkers, obraOficinaStaffId);
        public Task<LessonReminderCreateDataDTO> GetCreateData() => _repository.GetCreateData();
        public Task Create(LessonReminderCreateDTO dto, int userId) => _repository.Create(dto, userId);
        public Task UpdateProjectAsync(int userProjectId, int newProjectId, int userId) => _repository.UpdateProjectAsync(userProjectId, newProjectId, userId);
        public Task<bool> DeleteSoftAsync(int userProjectId, int userId) => _repository.DeleteSoftAsync(userProjectId, userId);
        public Task<ToggleLessonReminderResultDTO> ToggleActiveAsync(int userProjectId, int userId) => _repository.ToggleActiveAsync(userProjectId, userId);

        public Task<List<ProjectStaffReminderConfigItemDTO>> GetAllProjectStaffAsync() => _repository.GetAllProjectStaffAsync();
        public Task<ToggleProjectStaffReminderResultDTO> ToggleProjectStaffAsync(int projectId) => _repository.ToggleProjectStaffAsync(projectId);
        public Task<List<ActiveProjectStaffEmailDTO>> GetActiveStaffEmailsAsync() => _repository.GetActiveStaffEmailsAsync();

        public Task<List<JefeReminderConfigItemDTO>> GetAllJefesAsync() => _repository.GetAllJefesAsync();
        public Task<ToggleJefeReminderResultDTO> ToggleJefeAsync(int workerId) => _repository.ToggleJefeAsync(workerId);
        public Task<List<JefeReviewStatusDTO>> GetActiveJefesReviewStatusAsync() => _repository.GetActiveJefesReviewStatusAsync();

        public Task<List<WorkerRevisorItemDTO>> GetWorkerRevisoresAsync() => _repository.GetWorkerRevisoresAsync();
        public Task<List<WorkerRevisorOptionDTO>> GetWorkerRevisorOptionsAsync() => _repository.GetWorkerRevisorOptionsAsync();
        public Task UpdateWorkerRevisorAsync(int workerId, int? jefeWorkerId) => _repository.UpdateWorkerRevisorAsync(workerId, jefeWorkerId);
        public Task<ToggleAutoApproveLessonResultDTO> ToggleAutoApproveLessonAsync(int workerId) => _repository.ToggleAutoApproveLessonAsync(workerId);
    }
}

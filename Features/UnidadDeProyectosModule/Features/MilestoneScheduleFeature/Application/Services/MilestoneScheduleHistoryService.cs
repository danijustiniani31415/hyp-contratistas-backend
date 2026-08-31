using Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Application.Dtos;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Application.Interfaces;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Application.Services
{
    public class MilestoneScheduleHistoryService : IMilestoneScheduleHistoryService
    {
        private readonly IMilestoneScheduleHistoryRepository _repository;

        public MilestoneScheduleHistoryService(IMilestoneScheduleHistoryRepository repository)
        {
            _repository = repository;
        }

        public Task<List<MilestoneScheduleHistoryDTO>> GetAllByProjectId(int projectId)
            => _repository.GetAllByProjectIdFactory(projectId);

        public Task<ScheduleChangeResult> Create(MilestoneScheduleHistoryCreateDTO dto, int userId)
            => _repository.Create(dto, userId);
    }
}

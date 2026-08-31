using Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Application.Dtos;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Application.Interfaces
{
    public interface IMilestoneScheduleHistoryService
    {
        Task<List<MilestoneScheduleHistoryDTO>> GetAllByProjectId(int projectId);
        Task<ScheduleChangeResult> Create(MilestoneScheduleHistoryCreateDTO dto, int userId);
    }
}

using Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Application.Dtos;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Infrastructure.Interfaces
{
    public interface IMilestoneScheduleHistoryRepository
    {
        Task<List<MilestoneScheduleHistoryDTO>> GetAllByProjectIdFactory(int projectId);
        Task<ScheduleChangeResult> Create(MilestoneScheduleHistoryCreateDTO dto, int userId);
        Task<List<UserWithoutMilestoneDTO>> GetUsersWithoutScheduleHistoryThisMonth();
    }
}

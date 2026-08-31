using Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Application.Dtos;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Infrastructure.Interfaces
{
    public interface IMilestoneScheduleRepository
    {
        Task<List<MilestoneScheduleDTO>> GetAllByMilestoneScheduleHistoryIdFactory(int milestoneScheduleHistoryId);
        Task<List<ScheduleChangeInfoDTO>> GetSchedulesWithChangesThisMonthAsync();
        Task CulminarAsync(int milestoneScheduleId, DateOnly? fechaRealFin, int userId);
        Task MarcarCriticoAsync(int milestoneScheduleId, bool esHitoCritico, int userId);
    }
}

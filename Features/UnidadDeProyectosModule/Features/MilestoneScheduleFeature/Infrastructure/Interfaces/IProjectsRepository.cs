using Abril_Backend.Application.DTOs;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Infrastructure.Interfaces
{
    public interface IProjectsRepository
    {
        Task<PagedResult<ProjectDTO>> GetPagedWithResidents(int page, int pageSize = 10, string? search = null);
        Task UpdateFotoUrlAsync(int projectId, string? fotoUrl);
    }
}

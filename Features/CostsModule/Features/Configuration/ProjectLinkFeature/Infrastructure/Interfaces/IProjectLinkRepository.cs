using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.CostsModule.Features.Configuration.ProjectLinkFeature.Application.Dtos;
using Abril_Backend.Features.CostsModule.Features.Configuration.ProjectLinkFeature.Infrastructure.Models;

namespace Abril_Backend.Features.CostsModule.Features.Configuration.ProjectLinkFeature.Infrastructure.Interfaces
{
    public interface IProjectLinkRepository
    {
        // allowedProjectIds = null → sin restricción; lista → solo esos proyectos (Oficina Técnica).
        Task<PagedResult<ProjectLinkDto>> GetPaged(ProjectLinkFilterDto filter, List<int>? allowedProjectIds = null);
        Task<ProjectLinkFormDataDto> GetFormData(List<int>? allowedProjectIds = null);
        Task<List<ProjectLink>> GetByProjectIdAsync(int projectId);
        /// <summary>Proyectos asignados al usuario en user_project (activos).</summary>
        Task<List<int>> GetUserProjectIdsAsync(int userId);
        /// <summary>ProjectId del link, o null si no existe.</summary>
        Task<int?> GetProjectIdAsync(int projectLinkId);
        Task Create(ProjectLinkCreateDto dto, int userId);
        Task Update(ProjectLinkUpdateDto dto, int userId);
        Task<bool> Delete(int projectLinkId, int userId);
    }
}

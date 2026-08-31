using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.CostsModule.Features.Configuration.ProjectLinkFeature.Application.Dtos;

namespace Abril_Backend.Features.CostsModule.Features.Configuration.ProjectLinkFeature.Application.Interfaces
{
    public interface IProjectLinkService
    {
        // restrictToOwnProjects = true (Oficina Técnica): solo opera sobre los proyectos
        // asignados al usuario en user_project. false (Admin / Of. Central): sin límites.
        Task<PagedResult<ProjectLinkDto>> GetPaged(ProjectLinkFilterDto filter, int userId, bool restrictToOwnProjects);
        Task<ProjectLinkFormDataDto> GetFormData(int userId, bool restrictToOwnProjects);
        Task Create(ProjectLinkCreateDto dto, int userId, bool restrictToOwnProjects);
        Task Update(ProjectLinkUpdateDto dto, int userId, bool restrictToOwnProjects);
        Task<bool> Delete(int projectLinkId, int userId, bool restrictToOwnProjects);
    }
}

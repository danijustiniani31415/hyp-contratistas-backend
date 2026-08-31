using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.ConfigurationModule.Features.ProjectFeature.Application.Dtos;
using Abril_Backend.Features.CostsModule.Shared.Models;

namespace Abril_Backend.Features.ConfigurationModule.Features.ProjectFeature.Infrastructure.Interfaces
{
    public interface IProjectRepository
    {
        Task<PagedResult<ProjectDto>> GetPaged(int page, int pageSize, string? ruc = null, string? razonSocial = null, string? projectDescription = null, bool? active = null);
        Task Create(ProjectCreateDto dto, int userId);
        Task Update(ProjectEditDto dto, int userId);
        Task<bool> DeleteSoftAsync(int projectId, int userId);
        Task<Contributor?> FindContributorByRuc(string ruc);
        Task<Contributor> CreateContributor(string ruc, string name, string address, string economicActivity, string? district, string? province, string? department, int userId);
        Task UpdateContributorLocationAsync(int contributorId, string? district, string? province, string? department);
        Task UpdateEmails(int id, ProjectEmailsUpdateDto dto);
        Task<ProjectEmailsDto?> GetEmails(int projectId);
        Task<bool?> ToggleArquitecturaComercial(int projectId);
        Task<List<ResponsableLookupDto>> GetResponsables(string tipo);
        Task<List<int>> GetMyProjectIds(int userId);
    }
}

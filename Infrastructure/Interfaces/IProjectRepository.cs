using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.ConfigurationModule.Features.ProjectFeature.Application.Dtos;

namespace Abril_Backend.Infrastructure.Interfaces
{
    public interface IProjectRepository
    {
        Task<List<ProjectDTO>> GetAll();
        Task<List<ProjectSimpleDTO>> GetAllFactory();
        Task<string> GetProjectNameByProjectId(int projectId);
    }
}

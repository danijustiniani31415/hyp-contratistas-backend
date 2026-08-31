using Abril_Backend.Application.DTOs;

namespace Abril_Backend.Application.Interfaces
{
    public interface IProjectResidentService
    {
        Task<List<ProjectSimpleDTO>> GetProjectByResidentUserId(int userId);
        Task<List<ProjectSimpleDTO>> GetProjectsDescription();
    }
}
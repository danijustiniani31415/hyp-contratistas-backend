using Abril_Backend.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Application.Interfaces
{
    public interface IProjectsService
    {
        Task<PagedResult<ProjectDTO>> GetPagedWithResidents(int page, int pageSize = 10, string? search = null);
        Task<string> UploadFotoAsync(int projectId, IFormFile foto);
    }
}

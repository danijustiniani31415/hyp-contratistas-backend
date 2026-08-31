using Abril_Backend.Application.DTOs;

namespace Abril_Backend.Application.Interfaces
{
    public interface IResidentReportIncidenceService
    {
        Task<PagedResult<ResidentReportIncidenceDTO>> GetPaged(int page, int userId, bool isResidente, int? projectId = null, int? stateId = null);
        Task<List<ProjectSimpleDTO>> GetAssignedProjects(int userId, bool isResidente);
        Task Create(ResidentReportIncidenceCreateDTO dto, int userId);
        Task CreateResponse(ResidentReportResponseCreateDTO dto, int userId);
        Task UpdateIncidenceState(UpdateIncidenceDTO incidenceId, int userId);
    }
}
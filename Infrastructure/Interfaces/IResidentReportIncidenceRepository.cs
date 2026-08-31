using Abril_Backend.Application.DTOs;
namespace Abril_Backend.Infrastructure.Interfaces
{
    public interface IResidentReportIncidenceRepository
    {
        Task<PagedResult<ResidentReportIncidenceDTO>> GetPaged(int page, int? projectId = null, int? stateId = null, List<int>? allowedProjectIds = null);
        Task<int?> GetProjectId(int residentReportIncidenceId);
        Task Create(ResidentReportIncidenceCreateDTO dto, List<string> uploadedUrls, int userId);
        Task CreateResponse(ResidentReportResponseCreateDTO dto, List<string> uploadedUrls, int userId);
        Task UpdateIncidenceState(UpdateIncidenceDTO incidenceId, int userId);
    }
}
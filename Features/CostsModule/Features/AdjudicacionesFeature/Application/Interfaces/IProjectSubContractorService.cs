using Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos;
using Abril_Backend.Application.DTOs;

namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Interfaces
{
    public interface IProjectSubContractorService
    {
        Task Create(ProjectSubContractorCreateDTO dto, int page);
        Task UpdateInfo(int projectSubContractorId, ProjectSubContractorUpdateInfoDTO dto, int userId);
        Task<ProjectSubContractorFormDataDTO> GetFormData();
        // restrictToOwnProjects = true (Oficina Técnica): solo adjudicaciones de los proyectos
        // donde el correo del usuario está registrado en staff_project_email.
        Task<PagedResult<ProjectSubContractorDTO>> GetPaged(ProjectSubContractorFilterDTO filter, int userId, bool restrictToOwnProjects);
        Task<ProjectSubContractorPagedWithFiltersDTO> GetPagedWithFilters(ProjectSubContractorFilterDTO filter, int userId, bool restrictToOwnProjects);
        Task<AdjudicacionDashboardDto> GetDashboard(ProjectSubContractorFilterDTO filter, bool includeFilterOptions, int userId, bool restrictToOwnProjects);
        Task SendNotification(SendAdjudicacionNotificationDto dto, int userId);
        /// <summary>Destinatarios (Para + CC, sin BCC) del correo del paso 1 — para la confirmación previa.</summary>
        Task<AdjudicacionRecipientsPreviewDto> GetNotificationRecipients(int projectSubContractorId);
        Task SaveDates(int projectSubContractorId, UpdateDatesDTO dto, int userId);
        Task<DocumentUploadResponseDto> UploadDocumentAsync(int projectSubContractorId, AdjudicacionDocumentType documentType, IFormFile file, int userId);
        Task<DocumentUploadResponseDto> GenerateDocumentAsync(int projectSubContractorId, AdjudicacionDocumentType documentType, int userId);
        Task UpdateDocumentStatusAsync(int projectSubContractorId, AdjudicacionDocumentType documentType, int? statusId, string? observation, int userId);
        Task SendAllLevantamientoEmailAsync(int projectSubContractorId, SendAllObservationsEmailDto dto, int userId);
        Task UpdateStatusAsync(int projectSubContractorId, int statusId, int userId);
        Task AdvanceToStep4Async(int projectSubContractorId, string graphAccessToken, int userId);
        Task SendScNotificationAsync(int projectSubContractorId, string graphAccessToken, IFormFile? file, int userId);
        Task SetArrivalOptionAsync(int projectSubContractorId, bool arrivedWithObservations, int userId);
        Task ConfirmStep5Async(int projectSubContractorId, bool arrivedWithObservations, string? arrivalObservation, string graphAccessToken, int userId);
        Task SendStep6NotificationAsync(int projectSubContractorId, int userId);
        Task UpdateStep6ChecksAsync(int projectSubContractorId, UpdateStep6ChecksDTO dto, int userId);
        Task SendStep5ObservationsEmailAsync(int projectSubContractorId, SendStep5ObservationsEmailDto dto, int userId);
        Task SendStep5LevantamientoEmailAsync(int projectSubContractorId, SendStep5LevantamientoEmailDto dto, int userId);
        Task SendStep8NotificationAsync(int projectSubContractorId, string graphAccessToken, int userId);
        Task<(byte[] Bytes, string FileUrl, string OriginalFileName)> GenerateContractPackageAsync(int projectSubContractorId, int userId);
        Task SendObservationEmailAsync(int projectSubContractorId, AdjudicacionDocumentType documentType, SendObservationEmailDto dto, int userId);
        Task SendAllObservationsEmailAsync(int projectSubContractorId, SendAllObservationsEmailDto dto, int userId);
        Task SendContractReviewEmailAsync(int projectSubContractorId, SendAllObservationsEmailDto dto, int userId);
    }
}
using Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos;
using Abril_Backend.Application.DTOs;

namespace Abril_Backend.Features.Costs.Adjudicaciones.Infrastructure.Interfaces
{
    public interface IProjectSubContractorRepository
    {
        Task<int> Create(ProjectSubContractorCreateDTO dto, int userId);
        /// <summary>
        /// Datos de ruta de OneDrive disponibles ANTES de crear la adjudicación (proyecto +
        /// carpeta 04_OBRAS + especialidad), para validar el alta sin haberla insertado.
        /// </summary>
        Task<AdjudicacionPathDataDto> GetCreatePathDataAsync(int projectId, int? workSpecialtyId);
        /// <summary>Soft delete de la adjudicación (state = false). Nada se borra físicamente.</summary>
        Task SoftDeleteAsync(int projectSubContractorId, int userId);
        Task UpdateInfo(int projectSubContractorId, ProjectSubContractorUpdateInfoDTO dto, int userId);
        Task SaveInitialFilesAsync(int projectSubContractorId, List<(string Url, string OriginalFileName, string? ItemId)> quotationFiles, List<(string Url, string OriginalFileName, string? ItemId)> comparativeFiles, int userId);
        /// <summary>Soft delete de cotizaciones / cuadros comparativos del paso 1, acotado a la adjudicación.</summary>
        Task RemoveInitialFilesAsync(int projectSubContractorId, List<int>? quotationFileIds, List<int>? comparativeFileIds, int userId);
        Task<List<ContractTypeSimpleDTO>> GetContractTypeFactory();
        Task<List<PaymentMethodSimpleDTO>> GetPaymentMethodFactory();
        Task<List<CurrencySimpleDTO>> GetCurrencyFactory();
        Task<List<WorkItemSimpleDTO>> GetWorkItemFactory();
        Task<List<WorkItemCategorySimpleDTO>> GetWorkItemCategoryFactory();
        Task<List<ContributorFactoryDTO>> GetCompanyFactory();
        Task<ProjectSubContractorFormDataDTO> GetFormDataAsync();
        Task<PagedResult<ProjectSubContractorDTO>> GetPaged(ProjectSubContractorFilterDTO filter);
        Task<ProjectSubContractorPagedWithFiltersDTO> GetPagedWithFiltersAsync(ProjectSubContractorFilterDTO filter);
        Task<AdjudicacionDashboardDto> GetDashboardAsync(ProjectSubContractorFilterDTO filter, bool includeFilterOptions);
        Task<AdjudicacionNotificationDataDto> GetNotificationData(int projectSubContractorId);
        Task UpdateStatusToSent(int projectSubContractorId, int userId);
        Task UpdateStatus(int projectSubContractorId, int statusId, int userId);
        Task SaveDates(int projectSubContractorId, UpdateDatesDTO dto, int userId);
        Task<AdjudicacionPathDataDto> GetPathDataAsync(int projectSubContractorId);
        /// <summary>Persiste el nombre asignado a la carpeta de la adjudicación en OneDrive ("ADJUDICACIÓN N° X").</summary>
        Task SetAdjudicacionFolderNameAsync(int projectSubContractorId, string folderName);
        /// <summary>Lee el nombre asignado a la carpeta de la adjudicación (null si aún no tiene).</summary>
        Task<string?> GetAdjudicacionFolderNameAsync(int projectSubContractorId);
        Task SaveDocumentAsync(int projectSubContractorId, AdjudicacionDocumentType documentType, string fileUrl, string originalFileName, int userId, string? sharepointItemId = null);
        Task UpdateDocumentStatusAsync(int projectSubContractorId, AdjudicacionDocumentType documentType, int? statusId, string? observation, int userId);
        Task<AdjudicacionSummarySheetDataDto> GetSummarySheetDataAsync(int projectSubContractorId);
        Task<ScNotificationDataDto> GetScNotificationDataAsync(int projectSubContractorId);
        Task SetArrivalOptionAsync(int projectSubContractorId, bool arrivedWithObservations, int userId);
        Task ConfirmStep5Async(int projectSubContractorId, bool arrivedWithObservations, string? arrivalObservation, int userId);
        Task<Step3ApprovalDataDto> GetStep3ApprovalDataAsync(int projectSubContractorId);
        Task<List<DocumentObservationDto>> GetStep3DocumentObservationsAsync(int projectSubContractorId);
        Task<List<DocumentObservationDto>> GetLevantamientoDocumentsAsync(int projectSubContractorId);
        Task<Step6NotificationDataDto> GetStep6NotificationDataAsync(int projectSubContractorId);
        Task UpdateStep6ChecksAsync(int projectSubContractorId, bool signedCostos, bool signedGerenteInmobiliario, bool signedGerenteGeneral, int userId);
        Task<Step5EmailDataDto> GetStep5EmailDataAsync(int projectSubContractorId);
        Task SaveArrivalObservationAsync(int projectSubContractorId, string? arrivalObservation, int userId);
        Task<Step8NotificationDataDto> GetStep8NotificationDataAsync(int projectSubContractorId);
        Task<ContractPackageUrlsDto> GetContractPackageUrlsAsync(int projectSubContractorId);
        Task<(string FileUrl, string OriginalFileName)?> GetPackageFileInfoAsync(int projectSubContractorId);
        Task<(string? FolderId, string? FolderName, int? SyncStatus)?> GetInstructivosFolderAsync(int projectSubContractorId);
    }
}
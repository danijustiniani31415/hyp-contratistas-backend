using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.CostsModule.Features.Configuration.AdjudicacionFolderFeature.Application.Dtos;

namespace Abril_Backend.Features.CostsModule.Features.Configuration.AdjudicacionFolderFeature.Infrastructure.Interfaces
{
    public interface IAdjudicacionFolderRepository
    {
        Task<PagedResult<AdjudicacionFolderDto>> GetPaged(AdjudicacionFolderFilterDto filter);
        Task<AdjudicacionFolderFormDataDto> GetFormData();
        Task<bool> ExistsForProjectAsync(int projectId, int folderTypeId);
        Task<bool> FolderTypeExistsAsync(int folderTypeId);
        Task<int?> GetProjectIdAsync(int projectAdjudicacionFolderId);
        Task Create(AdjudicacionFolderCreateDto dto, string driveId, string folderId, string? folderName, string? webUrl, int userId);
        Task Update(AdjudicacionFolderUpdateDto dto, string driveId, string folderId, string? folderName, string? webUrl, int userId);
        Task<bool> Delete(int projectAdjudicacionFolderId, int userId);
    }
}

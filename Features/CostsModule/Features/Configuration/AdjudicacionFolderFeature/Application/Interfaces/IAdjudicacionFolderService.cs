using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.CostsModule.Features.Configuration.AdjudicacionFolderFeature.Application.Dtos;

namespace Abril_Backend.Features.CostsModule.Features.Configuration.AdjudicacionFolderFeature.Application.Interfaces
{
    public interface IAdjudicacionFolderService
    {
        Task<PagedResult<AdjudicacionFolderDto>> GetPaged(AdjudicacionFolderFilterDto filter);
        Task<AdjudicacionFolderFormDataDto> GetFormData();
        /// <summary>Valida el link (tenant) y devuelve la carpeta resuelta + sus subcarpetas.</summary>
        Task<FolderBrowseDto> ResolveLink(string linkUrl);
        /// <summary>Lista las subcarpetas de una carpeta (navegación dentro del mismo drive).</summary>
        Task<List<FolderItemDto>> GetChildFolders(string driveId, string folderId);
        Task Create(AdjudicacionFolderCreateDto dto, int userId);
        Task Update(AdjudicacionFolderUpdateDto dto, int userId);
        Task<bool> Delete(int projectAdjudicacionFolderId, int userId);
    }
}

using System.Text.RegularExpressions;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemCategoryFeature.Application.Dtos;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemCategoryFeature.Application.Interfaces;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemCategoryFeature.Infrastructure.Interfaces;
using Abril_Backend.Shared.Services.SharePoint.Interfaces;
using Abril_Backend.Shared.Services.SharePoint.Options;
using Microsoft.Extensions.Options;

namespace Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemCategoryFeature.Application.Services
{
    public class WorkItemCategoryService : IWorkItemCategoryService
    {
        private readonly IWorkItemCategoryRepository _repository;
        private readonly IGraphSharePointService _graphSharePoint;
        private readonly OneDriveOptions _oneDriveOptions;
        private readonly SharePointSiteRef _site;

        private static readonly Regex _folderPrefixRegex = new(@"^\d+\.\s*", RegexOptions.Compiled);

        public WorkItemCategoryService(
            IWorkItemCategoryRepository repository,
            IGraphSharePointService graphSharePoint,
            IOptions<OneDriveOptions> oneDriveOptions,
            IConfiguration configuration)
        {
            _repository = repository;
            _graphSharePoint = graphSharePoint;
            _oneDriveOptions = oneDriveOptions.Value;
            _site = SharePointSiteRef.FromConfig(configuration, "CostosYPresupuestos");
        }

        public async Task<PagedResult<WorkItemCategoryDto>> GetPaged(WorkItemCategoryFilterDto filter)
        {
            if (filter.Page < 1) filter.Page = 1;
            return await _repository.GetPaged(filter);
        }

        public async Task Create(WorkItemCategoryCreateDto dto, int userId)
            => await _repository.Create(dto, userId);

        public async Task Update(WorkItemCategoryEditDto dto, int userId)
            => await _repository.Update(dto, userId);

        public async Task<bool> Delete(int workItemCategoryId, int userId)
            => await _repository.Delete(workItemCategoryId, userId);

        public async Task<List<WorkSpecialtyOptionDto>> GetActiveSpecialties()
            => await _repository.GetActiveSpecialties();

        public async Task UploadInstructivoAsync(int workItemCategoryId, IFormFile file, int userId)
        {
            if (file == null || file.Length == 0)
                throw new AbrilException("El archivo no puede estar vacío.");

            using var stream = file.OpenReadStream();
            var result = await _graphSharePoint.UploadToSharePointLibraryAsync(
                site:        _site,
                libraryName: "Adjudicaciones",
                folderPath:  "INSTRUCTIVOS",
                fileName:    file.FileName,
                fileStream:  stream,
                contentType: file.ContentType ?? "application/octet-stream")
                ?? throw new AbrilException("No se pudo subir el archivo al servidor.");

            var fileUrl = result.WebUrl
                ?? throw new AbrilException("No se pudo obtener la URL del archivo subido.");

            await _repository.UpdateManualInstructivo(workItemCategoryId, fileUrl, file.FileName, userId);
        }

        public async Task<WorkItemCategorySyncResultDto> SyncInstructivosAsync(int userId)
        {
            var config = _oneDriveOptions.AdjudicacionesFeature.Instructivos;

            var folders = await _graphSharePoint.GetFolderChildrenAsync(
                config.DriveId,
                config.FolderPath,
                excludedFolderNames: ["OBSOLETOS"]);

            // Mapa normalizado → folder: nombre sin prefijo numérico, en mayúsculas
            var folderMap = folders
                .Where(f => f.IsFolder)
                .ToDictionary(
                    f => _folderPrefixRegex.Replace(f.Name, "").Trim().ToUpperInvariant(),
                    f => f);

            var categories = await _repository.GetAllActive();

            // Claves de BD ya matcheadas — para saber qué carpetas sobran al final
            var matchedFolderKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var result = new WorkItemCategorySyncResultDto { Total = categories.Count };

            foreach (var category in categories)
            {
                var key = category.WorkItemCategoryDescription.Trim().ToUpperInvariant();

                if (folderMap.TryGetValue(key, out var folder))
                {
                    matchedFolderKeys.Add(key);

                    if (category.InstructivosFolderId != folder.Id ||
                        category.InstructivosSyncStatus != 1)
                    {
                        await _repository.UpdateInstructivosSync(
                            category.WorkItemCategoryId,
                            folder.Id,
                            folder.Name,
                            syncStatus: 1);
                    }
                    result.Matched++;
                }
                else
                {
                    // No pisar si ya fue asignado manualmente (status = 2)
                    if (category.InstructivosSyncStatus != 2)
                    {
                        await _repository.UpdateInstructivosSync(
                            category.WorkItemCategoryId,
                            folderId: null,
                            folderName: null,
                            syncStatus: 3);
                    }
                    result.Unmatched++;
                    result.UnmatchedDescriptions.Add(category.WorkItemCategoryDescription);
                }
            }

            // Carpetas de OneDrive que no tuvieron match → crear registro en BD
            foreach (var (normalizedKey, folder) in folderMap)
            {
                if (matchedFolderKeys.Contains(normalizedKey)) continue;

                var description = _folderPrefixRegex.Replace(folder.Name, "").Trim();
                await _repository.CreateWithSync(description, folder.Id, folder.Name, userId);
                result.Created++;
                result.CreatedDescriptions.Add(description);
                result.Total++;
            }

            return result;
        }
    }
}

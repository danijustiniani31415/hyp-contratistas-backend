using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemFeature.Application.Dtos;

namespace Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemFeature.Infrastructure.Interfaces
{
    public interface IWorkItemRepository
    {
        Task<PagedResult<WorkItemDto>> GetPaged(WorkItemFilterDto filter);
        Task Create(WorkItemCreateDto dto, int userId);
        Task Update(WorkItemEditDto dto, int userId);
        Task<bool> Delete(int workItemId, int userId);
        Task<List<WorkItemCategoryOptionDto>> GetActiveCategories();

        // ── Soporte para sincronización ────────────────────────────────────
        Task<List<AdjudicacionFolderRootDto>> GetActiveAdjudicacionFolderRoots();
        /// <summary>Partidas activas (state = true), para dedup en la sincronización.</summary>
        Task<List<ExistingWorkItemDto>> GetActivePartidas();
        /// <summary>Inserta las partidas omitiendo cualquier duplicado; devuelve las que sí se crearon.</summary>
        Task<List<string>> BulkCreate(IEnumerable<string> descriptions, int userId);
    }
}

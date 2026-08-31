using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemCategoryFeature.Application.Dtos;

namespace Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemCategoryFeature.Application.Interfaces
{
    public interface IWorkItemCategoryService
    {
        Task<PagedResult<WorkItemCategoryDto>> GetPaged(WorkItemCategoryFilterDto filter);
        Task Create(WorkItemCategoryCreateDto dto, int userId);
        Task Update(WorkItemCategoryEditDto dto, int userId);
        Task<bool> Delete(int workItemCategoryId, int userId);
        Task<List<WorkSpecialtyOptionDto>> GetActiveSpecialties();
        Task<WorkItemCategorySyncResultDto> SyncInstructivosAsync(int userId);
        Task UploadInstructivoAsync(int workItemCategoryId, IFormFile file, int userId);
    }
}

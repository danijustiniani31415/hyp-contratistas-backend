using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemCategoryFeature.Application.Dtos;
using Abril_Backend.Features.CostsModule.Shared.Models;

namespace Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemCategoryFeature.Infrastructure.Interfaces
{
    public interface IWorkItemCategoryRepository
    {
        Task<PagedResult<WorkItemCategoryDto>> GetPaged(WorkItemCategoryFilterDto filter);
        Task Create(WorkItemCategoryCreateDto dto, int userId);
        Task Update(WorkItemCategoryEditDto dto, int userId);
        Task<bool> Delete(int workItemCategoryId, int userId);
        Task<List<WorkItemCategory>> GetAllActive();
        Task<List<WorkSpecialtyOptionDto>> GetActiveSpecialties();
        Task UpdateInstructivosSync(int workItemCategoryId, string? folderId, string? folderName, int syncStatus);
        Task CreateWithSync(string description, string folderId, string folderName, int userId);
        Task UpdateManualInstructivo(int workItemCategoryId, string fileUrl, string fileName, int userId);
    }
}

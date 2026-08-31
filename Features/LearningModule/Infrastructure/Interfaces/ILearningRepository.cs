using Abril_Backend.Features.LearningModule.Application.Dtos;

namespace Abril_Backend.Features.LearningModule.Infrastructure.Interfaces
{
    public interface ILearningRepository
    {
        // Display
        Task<List<LearningCategoryDto>> GetLoginCategories();
        Task<List<LearningCategoryDto>> GetInicioCategories(int[] roleIds);

        // Admin
        Task<LearningAdminDataDto> GetAdminData();

        Task<int> CreateCategory(LearningCategoryCreateDto dto);
        Task EditCategory(int id, LearningCategoryEditDto dto);
        Task<bool> ToggleCategory(int id);
        Task DeleteCategory(int id);

        Task<int> CreateVideo(LearningVideoCreateDto dto);
        Task EditVideo(int id, LearningVideoEditDto dto);
        Task<bool> ToggleVideo(int id);
        Task DeleteVideo(int id);
    }
}

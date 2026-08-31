using Abril_Backend.Features.LearningModule.Application.Dtos;

namespace Abril_Backend.Features.LearningModule.Application.Interfaces
{
    public interface ILearningService
    {
        Task<List<LearningCategoryDto>> GetLoginCategories();
        Task<List<LearningCategoryDto>> GetInicioCategories(int[] roleIds);

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

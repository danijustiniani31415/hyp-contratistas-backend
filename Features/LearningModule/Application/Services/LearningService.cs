using Abril_Backend.Features.LearningModule.Application.Dtos;
using Abril_Backend.Features.LearningModule.Application.Interfaces;
using Abril_Backend.Features.LearningModule.Infrastructure.Interfaces;

namespace Abril_Backend.Features.LearningModule.Application.Services
{
    /// <summary>
    /// Orquesta el centro de aprendizaje (videos-guía). Envoltorio delgado sobre el
    /// repositorio: la validación de datos vive en el repositorio (patrón de las demás
    /// features de configuración de este backend).
    /// </summary>
    public class LearningService : ILearningService
    {
        private readonly ILearningRepository _repo;

        public LearningService(ILearningRepository repo)
        {
            _repo = repo;
        }

        public Task<List<LearningCategoryDto>> GetLoginCategories() => _repo.GetLoginCategories();
        public Task<List<LearningCategoryDto>> GetInicioCategories(int[] roleIds) => _repo.GetInicioCategories(roleIds);

        public Task<LearningAdminDataDto> GetAdminData() => _repo.GetAdminData();

        public Task<int> CreateCategory(LearningCategoryCreateDto dto) => _repo.CreateCategory(dto);
        public Task EditCategory(int id, LearningCategoryEditDto dto) => _repo.EditCategory(id, dto);
        public Task<bool> ToggleCategory(int id) => _repo.ToggleCategory(id);
        public Task DeleteCategory(int id) => _repo.DeleteCategory(id);

        public Task<int> CreateVideo(LearningVideoCreateDto dto) => _repo.CreateVideo(dto);
        public Task EditVideo(int id, LearningVideoEditDto dto) => _repo.EditVideo(id, dto);
        public Task<bool> ToggleVideo(int id) => _repo.ToggleVideo(id);
        public Task DeleteVideo(int id) => _repo.DeleteVideo(id);
    }
}

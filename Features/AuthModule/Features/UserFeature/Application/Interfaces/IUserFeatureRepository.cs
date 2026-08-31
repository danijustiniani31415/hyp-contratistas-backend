using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.AuthModule.UserFeature.Application.Dtos;
using UserModel = Abril_Backend.Infrastructure.Models.User;

namespace Abril_Backend.Features.AuthModule.UserFeature.Application.Interfaces
{
    public interface IUserFeatureRepository
    {
        Task<PagedResult<UserListItemDto>> GetPaged(int page, int pageSize, string? search = null, int? categoriaId = null);
        Task<List<UserCategoriaOptionDto>> GetCategoriaOptions();
        Task<List<AbrilWorkerOptionDto>> GetAbrilWorkersWithoutUser();
        Task<UserModel> Create(UserFeatureCreateDto dto);
        Task CreateAbrilWorkerUser(AbrilWorkerUserCreateDto dto, int createdUserId);
        Task CreateAbrilManualUser(string email, string? fullName, List<int> roleIds, int createdUserId);
        Task Update(int userId, UserFeatureUpdateDto dto, int updatedUserId);
        Task ToggleActive(int userId, int updatedUserId);
        Task Delete(int userId, int updatedUserId);
    }
}

using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.AuthModule.UserFeature.Application.Dtos;

namespace Abril_Backend.Features.AuthModule.UserFeature.Application.Interfaces
{
    public interface IUserFeatureService
    {
        Task<PagedResult<UserListItemDto>> GetPaged(int page, int pageSize, string? search = null, int? categoriaId = null);
        Task<UserListInitialDto> GetInitial(int page, int pageSize);
        Task<List<AbrilWorkerOptionDto>> GetAbrilWorkersWithoutUser();
        Task Create(UserFeatureCreateDto dto);
        Task CreateAbrilWorkerUser(AbrilWorkerUserCreateDto dto, int createdUserId);
        Task CreateAbrilManualUser(AbrilManualUserCreateDto dto, int createdUserId);
        Task Update(int userId, UserFeatureUpdateDto dto, int updatedUserId);
        Task ToggleActive(int userId, int updatedUserId);
        Task Delete(int userId, int updatedUserId);
    }
}

using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.AuthModule.Role.Application.Dtos;

namespace Abril_Backend.Features.AuthModule.Role.Application.Interfaces
{
    public interface IRoleFeatureService
    {
        Task<PagedResult<RoleDto>> GetPaged(int page, int pageSize, string? search = null);
        Task Create(RoleCreateDto dto, int userId);
        Task<List<FeatureDto>> GetAllFeatures();
        Task<List<int>> GetRoleFeatureIds(int roleId);
        Task UpdateRoleFeatures(int roleId, List<int> featureIds);
        Task UpdateRoleDescription(int roleId, RoleUpdateDescriptionDto dto, int userId);
    }
}

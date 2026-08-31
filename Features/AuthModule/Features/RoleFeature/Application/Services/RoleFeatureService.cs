using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AuthModule.Role.Application.Dtos;
using Abril_Backend.Features.AuthModule.Role.Application.Interfaces;
using Abril_Backend.Features.AuthModule.Role.Infrastructure.Interfaces;
using Abril_Backend.Shared.Realtime;

namespace Abril_Backend.Features.AuthModule.Role.Application.Services
{
    public class RoleFeatureService : IRoleFeatureService
    {
        private readonly IRoleFeatureRepository _repository;
        private readonly IRealtimeNotifier _notifier;

        public RoleFeatureService(IRoleFeatureRepository repository, IRealtimeNotifier notifier)
        {
            _repository = repository;
            _notifier = notifier;
        }

        public async Task<PagedResult<RoleDto>> GetPaged(int page, int pageSize, string? search = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            return await _repository.GetPaged(page, pageSize, search);
        }

        public async Task Create(RoleCreateDto dto, int userId)
        {
            await _repository.Create(dto, userId);
        }

        public async Task<List<FeatureDto>> GetAllFeatures()
        {
            return await _repository.GetAllFeatures();
        }

        public async Task<List<int>> GetRoleFeatureIds(int roleId)
        {
            return await _repository.GetRoleFeatureIds(roleId);
        }

        public async Task UpdateRoleFeatures(int roleId, List<int> featureIds)
        {
            await _repository.UpdateRoleFeatures(roleId, featureIds);

            // Avisar en tiempo real a todos los usuarios que tienen este rol para que
            // refresquen sus permisos al instante (los desconectados se enteran en su
            // próximo refresh de token).
            var userIds = await _repository.GetUserIdsByRole(roleId);
            await _notifier.NotifyRoleFeaturesChanged(userIds);
        }

        public async Task UpdateRoleDescription(int roleId, RoleUpdateDescriptionDto dto, int userId)
        {
            if (string.IsNullOrWhiteSpace(dto.RoleDescription))
                throw new AbrilException("La descripción del rol es obligatoria.");

            await _repository.UpdateRoleDescription(roleId, dto.RoleDescription, userId);
        }
    }
}

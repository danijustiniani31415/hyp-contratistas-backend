using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Application.Dtos;
using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Application.Interfaces;
using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Application.Services
{
    public class AreaScopeService : IAreaScopeService
    {
        private readonly IAreaScopeRepository _repo;
        public AreaScopeService(IAreaScopeRepository repo) => _repo = repo;

        public Task<List<AreaScopeTreeDto>> GetTreeAsync() => _repo.GetTreeAsync();
        public Task<List<AreaScopeWorkerDto>> GetWorkersAsync(int areaScopeId) => _repo.GetWorkersAsync(areaScopeId);
        public Task CreateBranchAsync(AreaScopeBranchDto dto) => _repo.CreateBranchAsync(dto);
        public Task UpdateParentAsync(int areaScopeId, int? newParentAreaScopeId) => _repo.UpdateParentAsync(areaScopeId, newParentAreaScopeId);
        public Task DeleteAsync(int areaScopeId) => _repo.DeleteAsync(areaScopeId);
    }
}

using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Application.Dtos;

namespace Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Infrastructure.Interfaces
{
    public interface IAreaScopeRepository
    {
        Task<List<AreaScopeTreeDto>> GetTreeAsync();
        Task<List<AreaScopeWorkerDto>> GetWorkersAsync(int areaScopeId);
        Task CreateBranchAsync(AreaScopeBranchDto dto);
        Task UpdateParentAsync(int areaScopeId, int? newParentAreaScopeId);
        Task DeleteAsync(int areaScopeId);
    }
}

using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.CostsModule.Features.Configuration.CostosPresupuestosEmailFeature.Application.Dtos;

namespace Abril_Backend.Features.CostsModule.Features.Configuration.CostosPresupuestosEmailFeature.Infrastructure.Interfaces
{
    public interface ICostosPresupuestosEmailRepository
    {
        Task<PagedResult<CostosPresupuestosEmailDto>> GetPaged(CostosPresupuestosEmailFilterDto filter);
        Task<List<string>> GetActiveEmails();
        Task Create(CostosPresupuestosEmailCreateDto dto, int userId);
        Task Update(CostosPresupuestosEmailEditDto dto, int userId);
        Task<bool> Delete(int id, int userId);
    }
}

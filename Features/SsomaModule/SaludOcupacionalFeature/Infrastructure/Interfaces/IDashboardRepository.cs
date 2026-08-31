using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Dashboard;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces
{
    public interface IDashboardRepository
    {
        Task<DashboardSaludOcupacionalDto> GetDashboard();
    }
}

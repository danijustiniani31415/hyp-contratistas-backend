using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Dashboard;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardSaludOcupacionalDto> GetDashboard();
    }
}

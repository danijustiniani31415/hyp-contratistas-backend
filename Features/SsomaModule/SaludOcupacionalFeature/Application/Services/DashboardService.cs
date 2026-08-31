using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Dashboard;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _repo;

        public DashboardService(IDashboardRepository repo)
        {
            _repo = repo;
        }

        public Task<DashboardSaludOcupacionalDto> GetDashboard() => _repo.GetDashboard();
    }
}

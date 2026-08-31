using Abril_Backend.Features.Habilitacion.Application.Dtos.Dashboard;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces
{
    public interface IDashboardHabRepository
    {
        Task<DashboardAdminDto> GetResumenAsync(int proyectoId);
    }
}

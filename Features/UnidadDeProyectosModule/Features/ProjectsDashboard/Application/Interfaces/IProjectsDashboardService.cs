using Abril_Backend.Features.UnidadDeProyectosModule.Features.ProjectsDashboard.Application.Dtos;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.ProjectsDashboard.Application.Interfaces
{
    public interface IProjectsDashboardService
    {
        Task<ProjectsDashboardFiltersResponseDto> GetFilters();
        Task<ProjectsDashboardResponseDto> GetDashboard(int? proyectoId, string? estado, int? responsableId, DateOnly? fechaDesde, DateOnly? fechaHasta);
        Task<ProyectoDetailDashboardDto> GetProyectoDetail(int proyectoId);
    }
}

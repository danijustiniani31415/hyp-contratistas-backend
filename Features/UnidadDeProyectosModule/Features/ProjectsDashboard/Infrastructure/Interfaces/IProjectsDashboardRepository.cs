using Abril_Backend.Features.UnidadDeProyectosModule.Features.ProjectsDashboard.Application.Dtos;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.ProjectsDashboard.Infrastructure.Interfaces
{
    public interface IProjectsDashboardRepository
    {
        Task<(List<ProyectoSimpleDto> Projects, List<string> Estados, List<ResponsableSimpleDto> Responsables)> GetFiltersDataFactory();
        Task<(List<ProyectoDetalleDto> Proyectos, List<ResponsableRankingDto> Ranking, List<HeatmapResponsableDto> Heatmap)> GetDashboardAsync(int? proyectoId, string? estado, int? responsableId, DateOnly today, DateOnly heatDesde, DateOnly heatHasta);
        Task<ProyectoDetailDashboardDto> GetProyectoDetailAsync(int proyectoId, DateOnly today);
    }
}

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.ProjectsDashboard.Application.Dtos
{
    public class ProjectsDashboardFiltersResponseDto
    {
        public List<ProyectoSimpleDto> Proyectos { get; set; } = new();
        public List<string> Estados { get; set; } = new();
        public List<ResponsableSimpleDto> Responsables { get; set; } = new();
    }

    public class ProyectoSimpleDto
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
    }

    public class ResponsableSimpleDto
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
    }
}

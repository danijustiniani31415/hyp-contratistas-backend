namespace Abril_Backend.Features.Evaluaciones.Application.Dtos
{
    public class SupervisorConAsignacionesDto
    {
        public int WorkerId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string? Subarea { get; set; }
        public List<ProyectoAsignadoDto> Proyectos { get; set; } = [];
    }

    public class ProyectoAsignadoDto
    {
        public int ProjectId { get; set; }
        public string? ProjectDescription { get; set; }
    }

    public class ActualizarAsignacionesDto
    {
        public List<int> ProjectIds { get; set; } = [];
    }
}

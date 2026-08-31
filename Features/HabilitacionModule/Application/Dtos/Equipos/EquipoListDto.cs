namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Equipos
{
    public class EquipoListDto
    {
        public int Id { get; set; }
        public int TipoEquipoId { get; set; }
        public string TipoNombre { get; set; } = string.Empty;
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public string? NSerie { get; set; }
        public string? Capacidad { get; set; }
        public string? PropietarioEmpresaNombre { get; set; }
        public int ProyectoId { get; set; }
        public string ProyectoNombre { get; set; } = string.Empty;
        public string EstadoHabilitacion { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }
}

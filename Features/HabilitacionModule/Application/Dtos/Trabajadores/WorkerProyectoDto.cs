namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Trabajadores
{
    public class WorkerProyectoDto
    {
        public int Id { get; set; }
        public int WorkerId { get; set; }
        public int ProyectoId { get; set; }
        public string? ProyectoNombre { get; set; }
        public int? EmpresaId { get; set; }
        public string? EmpresaNombre { get; set; }
        public DateOnly FechaInicio { get; set; }
        public DateOnly? FechaFin { get; set; }
        public bool InduccionCompletada { get; set; }
        public DateOnly? FechaInduccion { get; set; }
        public bool Activo { get; set; }
    }
}

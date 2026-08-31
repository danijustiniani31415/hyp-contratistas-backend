namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Trabajadores
{
    public class WorkerEventoDto
    {
        public int Id { get; set; }
        public string TipoEvento { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int? ProyectoAnteriorId { get; set; }
        public string? ProyectoAnteriorNombre { get; set; }
        public int? ProyectoNuevoId { get; set; }
        public string? ProyectoNuevoNombre { get; set; }
        public int? EmpresaAnteriorId { get; set; }
        public string? EmpresaAnteriorNombre { get; set; }
        public int? EmpresaNuevaId { get; set; }
        public string? EmpresaNuevaNombre { get; set; }
        public string? Datos { get; set; }
        public int? UsuarioId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

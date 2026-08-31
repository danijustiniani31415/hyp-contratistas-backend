namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Restringidos
{
    public class TrabajadorRestringidoListDto
    {
        public int Id { get; set; }
        public string? Dni { get; set; }
        public string? ApellidoNombre { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string? ProyectoOrigen { get; set; }
        public string? RestringidoPor { get; set; }
        public DateOnly? FechaRestriccion { get; set; }
        public string Tipo { get; set; } = "MANUAL";
        public bool Activo { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}

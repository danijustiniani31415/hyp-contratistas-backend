namespace Abril_Backend.Features.Habilitacion.Application.Dtos.SctrVidaley
{
    public class SctrTrabajadorEstadoDto
    {
        public int WorkerId { get; set; }
        public string ApellidoNombre { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public int? ObraOficinaStaffId { get; set; }
        public string? ObraOficina { get; set; }
        public int? SctrId { get; set; }
        public int? SctrHabId { get; set; }
        public string EstadoSctr { get; set; } = "Falta";
        public string EstadoVidaLey { get; set; } = "Falta";
        public string? EmpresaNombre { get; set; }
        public string? ProyectoNombre { get; set; }
        public DateTime? FechaVencimiento { get; set; }
    }
}

namespace Abril_Backend.Features.Habilitacion.Application.Dtos.SctrVidaley
{
    public class SctrWorkerDto
    {
        public int WorkerId { get; set; }
        public string ApellidoNombre { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public bool Aprobado { get; set; }
        public string Estado { get; set; } = "Falta";
        public int? SctrHabId { get; set; }
        public DateTime? FechaInicioCobertura { get; set; }
        public DateTime? FechaVencimiento { get; set; }
    }
}

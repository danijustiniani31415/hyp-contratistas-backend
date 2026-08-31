namespace Abril_Backend.Application.DTOs.ArquitecturaComercial
{
    public class GanttActividadDTO
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string? ProjectNombre { get; set; }
        public int? Orden { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Tipo { get; set; }
        public int? EtapaId { get; set; }
        public string? EtapaNombre { get; set; }
        public bool Activo { get; set; }
        public DateOnly? InicioProgramado { get; set; }
        public DateOnly? FinProgramado { get; set; }
        public DateOnly? InicioEfectivo { get; set; }
        public DateOnly? FinEfectivo { get; set; }
    }
}

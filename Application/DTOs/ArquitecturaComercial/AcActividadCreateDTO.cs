namespace Abril_Backend.Application.DTOs.ArquitecturaComercial
{
    public class AcActividadCreateDTO
    {
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = "CONSULTA";
        public int ProjectId { get; set; }
        public int? EtapaId { get; set; }
        public int? UserId { get; set; }
        public int? UserId2 { get; set; }
        public DateOnly? InicioProgramado { get; set; }
        public DateOnly? FinProgramado { get; set; }
        public string? Observaciones { get; set; }
        public int? CategoriaId { get; set; }
        public int? EspecialidadId { get; set; }
    }
}

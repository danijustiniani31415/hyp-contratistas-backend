namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Interconsulta
{
    public class InterconsultaCreateDto
    {
        public int? EmoId { get; set; }
        public int WorkerId { get; set; }
        public string Especialidad { get; set; } = string.Empty;
        public int? MedicoDerivaId { get; set; }
        public DateOnly FechaDerivacion { get; set; }
        public string? CentroAtencion { get; set; }
        public bool RequiereSeguimiento { get; set; }
        public string? UrlInforme { get; set; }
    }
}

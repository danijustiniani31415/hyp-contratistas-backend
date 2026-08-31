namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Interconsulta
{
    public class InterconsultaUpdateDto
    {
        public string Especialidad { get; set; } = string.Empty;
        public int? MedicoDerivaId { get; set; }
        public DateOnly FechaDerivacion { get; set; }
        public DateOnly? FechaAtencion { get; set; }
        public string? CentroAtencion { get; set; }
        public string? Diagnostico { get; set; }
        public string? Cie10 { get; set; }
        public string? Resultado { get; set; }
        public string? UrlInforme { get; set; }
        public bool RequiereSeguimiento { get; set; }
    }
}

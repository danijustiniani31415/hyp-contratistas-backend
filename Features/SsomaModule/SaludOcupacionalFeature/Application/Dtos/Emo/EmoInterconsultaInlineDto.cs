namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Emo
{
    public class InterconsultaInlineDto
    {
        public string Especialidad { get; set; } = string.Empty;
        public string? CentroAtencion { get; set; }
        public string? Diagnostico { get; set; }
        public string? Cie10 { get; set; }
        public int? MedicoDerivaId { get; set; }
        public bool RequiereSeguimiento { get; set; }
    }
}

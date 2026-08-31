namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Interconsulta
{
    public class InterconsultaResultadoPatchDto
    {
        public string Estado { get; set; } = string.Empty;
        public DateOnly? FechaAtencion { get; set; }
        public string? Diagnostico { get; set; }
        public string? Cie10 { get; set; }
        public string? Resultado { get; set; }
        public string? UrlInforme { get; set; }
        public bool? RequiereSeguimiento { get; set; }
    }
}

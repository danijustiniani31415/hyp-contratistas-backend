namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Interconsulta
{
    public class InterconsultaDetalleDto
    {
        public int Id { get; set; }
        public int? EmoId { get; set; }
        public int WorkerId { get; set; }
        public string? WorkerNombre { get; set; }
        public string? WorkerDni { get; set; }
        public string Especialidad { get; set; } = string.Empty;
        public string? Medico { get; set; }
        public DateOnly FechaDerivacion { get; set; }
        public DateOnly? FechaAtencion { get; set; }
        public string? CentroAtencion { get; set; }
        public string? Diagnostico { get; set; }
        public string? Cie10 { get; set; }
        public string? Resultado { get; set; }
        public string? UrlInforme { get; set; }
        public string Estado { get; set; } = string.Empty;
        public bool RequiereSeguimiento { get; set; }
        public int DiasPendiente { get; set; }
    }
}

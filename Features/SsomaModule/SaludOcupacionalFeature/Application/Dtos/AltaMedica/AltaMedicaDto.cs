namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.AltaMedica
{
    public class AltaMedicaDto
    {
        public int Id { get; set; }
        public int AccidenteId { get; set; }
        public int TipoId { get; set; }
        public string TipoNombre { get; set; } = string.Empty;
        public DateOnly FechaAlta { get; set; }
        public string? Medico { get; set; }
        public string? DiagnosticoFinal { get; set; }
        public bool TieneRestriccion { get; set; }
        public string? DescripcionRestriccion { get; set; }
        public DateOnly? FechaFinRestriccion { get; set; }
        public string? UrlCertificado { get; set; }
        public string? Observaciones { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class AltaMedicaCreateDto
    {
        public int TipoId { get; set; }
        public DateOnly FechaAlta { get; set; }
        public string? Medico { get; set; }
        public string? DiagnosticoFinal { get; set; }
        public bool TieneRestriccion { get; set; }
        public string? DescripcionRestriccion { get; set; }
        public DateOnly? FechaFinRestriccion { get; set; }
        public string? UrlCertificado { get; set; }
        public string? Observaciones { get; set; }
    }

    public class AltaMedicaUpdateDto
    {
        public int TipoId { get; set; }
        public DateOnly FechaAlta { get; set; }
        public string? Medico { get; set; }
        public string? DiagnosticoFinal { get; set; }
        public bool TieneRestriccion { get; set; }
        public string? DescripcionRestriccion { get; set; }
        public DateOnly? FechaFinRestriccion { get; set; }
        public string? UrlCertificado { get; set; }
        public string? Observaciones { get; set; }
    }
}

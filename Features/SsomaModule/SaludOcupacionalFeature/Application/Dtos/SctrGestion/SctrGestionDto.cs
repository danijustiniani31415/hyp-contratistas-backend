namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.SctrGestion
{
    public class SctrGestionDto
    {
        public int Id { get; set; }
        public Guid CasoSocialId { get; set; }
        public string? NumeroSiniestro { get; set; }
        public DateOnly? FechaReporteSctr { get; set; }
        public DateOnly? FechaAtencionSctr { get; set; }
        public string? Aseguradora { get; set; }
        public decimal? MontoCubierto { get; set; }
        public string? UrlHojaAtencion { get; set; }
        public string? UrlDocumentosAdicionales { get; set; }
        public int EstadoId { get; set; }
        public string EstadoNombre { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class SctrGestionCreateDto
    {
        public string? NumeroSiniestro { get; set; }
        public DateOnly? FechaReporteSctr { get; set; }
        public DateOnly? FechaAtencionSctr { get; set; }
        public string? Aseguradora { get; set; }
        public decimal? MontoCubierto { get; set; }
        public string? UrlHojaAtencion { get; set; }
        public string? UrlDocumentosAdicionales { get; set; }
        public int EstadoId { get; set; }
        public string? Observaciones { get; set; }
    }

    public class SctrGestionUpdateDto
    {
        public string? NumeroSiniestro { get; set; }
        public DateOnly? FechaReporteSctr { get; set; }
        public DateOnly? FechaAtencionSctr { get; set; }
        public string? Aseguradora { get; set; }
        public decimal? MontoCubierto { get; set; }
        public string? UrlHojaAtencion { get; set; }
        public string? UrlDocumentosAdicionales { get; set; }
        public int EstadoId { get; set; }
        public string? Observaciones { get; set; }
    }
}

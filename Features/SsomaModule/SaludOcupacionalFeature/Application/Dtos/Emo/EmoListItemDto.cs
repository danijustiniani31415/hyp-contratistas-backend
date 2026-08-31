namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Emo
{
    public class EmoListItemDto
    {
        public int Id { get; set; }
        public int WorkerId { get; set; }
        public string? WorkerNombre { get; set; }
        public string? WorkerDni { get; set; }
        public string? TipoEmo { get; set; }
        public string? Empresa { get; set; }
        public DateOnly FechaEmo { get; set; }
        public DateOnly? FechaVencimiento { get; set; }
        public string? Aptitud { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int? DiasParaVencer { get; set; }
        public string? UrlResultado { get; set; }
        public string? UrlAptitud { get; set; }
        public string? UrlEmoCompleto { get; set; }
    }
}

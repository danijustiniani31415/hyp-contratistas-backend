namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Emo
{
    public class EmoConvalidacionResumenDto
    {
        public int Id { get; set; }
        public int? EmpresaDestinoId { get; set; }
        public string? EmpresaDestinoNombre { get; set; }
        public DateOnly FechaConvalidacion { get; set; }
        public string Resultado { get; set; } = string.Empty;
        public DateOnly? FechaVencimiento { get; set; }
        public string? Observaciones { get; set; }
        public string? UrlDocumento { get; set; }
    }
}

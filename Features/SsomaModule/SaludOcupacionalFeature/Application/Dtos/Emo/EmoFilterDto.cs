namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Emo
{
    public class EmoFilterDto
    {
        public int? WorkerId { get; set; }
        public string? Estado { get; set; }
        public string? Aptitud { get; set; }
        public int? EmpresaId { get; set; }
        public int Page { get; set; } = 1;
    }
}

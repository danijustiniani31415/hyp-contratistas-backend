namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Emo
{
    public class EmoExamenDto
    {
        public int ExamenTipoId { get; set; }
        public string? ExamenNombre { get; set; }
        public string? Resultado { get; set; }
        public string? Valor { get; set; }
        public string? Unidad { get; set; }
        public string? Observacion { get; set; }
    }
}

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Emo
{
    public class EmoRestriccionDto
    {
        public int? RestriccionTipoId { get; set; }
        public string? RestriccionDescripcion { get; set; }
        public string? DescripcionLibre { get; set; }
        public bool Vigente { get; set; } = true;
    }
}

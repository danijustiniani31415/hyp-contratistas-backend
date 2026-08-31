namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Alerta
{
    public class EmoAutoProgramacionResultDto
    {
        public int Procesados { get; set; }
        public int YaTenianProgramacion { get; set; }
        public int Errores { get; set; }
        public List<string> Detalle { get; set; } = new();
    }
}

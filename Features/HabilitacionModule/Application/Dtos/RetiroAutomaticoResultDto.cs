namespace Abril_Backend.Features.Habilitacion.Application.Dtos
{
    public class RetiroAutomaticoResultDto
    {
        public int TotalRetirados { get; set; }
        public int TotalAvisados { get; set; }
        public List<string> Detalles { get; set; } = [];
    }
}

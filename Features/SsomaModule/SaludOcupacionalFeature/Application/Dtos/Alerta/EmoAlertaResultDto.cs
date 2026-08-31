namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Alerta
{
    public class EmoAlertaResultDto
    {
        public int TotalProcesados { get; set; }
        public int TotalEnviados { get; set; }
        public int TotalErrores { get; set; }
        public List<string> Detalles { get; set; } = new List<string>();
    }
}

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Alerta
{
    public class EmoResumenDiarioResultDto
    {
        public DateOnly Fecha { get; set; }
        public int Atendidos { get; set; }
        public int Aptos { get; set; }
        public int ConInterconsulta { get; set; }
        public int NoAptos { get; set; }
        public int NoSePresentaron { get; set; }
        public int Programados { get; set; }
        public bool EmailEnviado { get; set; }
    }
}

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Emo
{
    public class EmoUpdateDto
    {
        public int TipoEmoId { get; set; }
        public int? EmpresaOrigenId { get; set; }
        public DateOnly FechaEmo { get; set; }
        public int? ClinicaId { get; set; }
        public int? MedicoId { get; set; }
        public string? Aptitud { get; set; }
        public bool RequiereInterconsulta { get; set; }
        public string? NumeroInforme { get; set; }
        public string? UrlResultado { get; set; }
        public bool RequiereLecturaAbril { get; set; }
        public string? Notas { get; set; }
        public List<EmoExamenDto> Examenes { get; set; } = new();
        public List<EmoRestriccionDto> Restricciones { get; set; } = new();
    }
}

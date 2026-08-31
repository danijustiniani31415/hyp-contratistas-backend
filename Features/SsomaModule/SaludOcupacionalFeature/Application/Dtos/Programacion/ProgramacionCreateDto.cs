namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Programacion
{
    public class ProgramacionCreateDto
    {
        public int WorkerId { get; set; }
        public int? EmpresaId { get; set; }
        public int TipoEmoId { get; set; }
        public DateOnly FechaProgramada { get; set; }
        public TimeOnly? HoraProgramada { get; set; }
        public int? ClinicaId { get; set; }
        public int? MedicoId { get; set; }
        public string? Motivo { get; set; }
        public string? Notas { get; set; }
        public string? Origen { get; set; }
    }
}

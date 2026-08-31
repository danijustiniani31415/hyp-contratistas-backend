namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Programacion
{
    public class ProgramacionFilterDto
    {
        public DateOnly? Desde { get; set; }
        public DateOnly? Hasta { get; set; }
        public string? Estado { get; set; }
        public int? WorkerId { get; set; }
        public int? ClinicaId { get; set; }
        public int? AreaScopeId { get; set; }
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        // true = médico SSOMA ve todas (incluyendo con interconsulta pendiente)
        // false/null = clínica solo ve las que no tienen interconsulta pendiente
        public bool IncluirConInterconsulta { get; set; } = false;
    }
}

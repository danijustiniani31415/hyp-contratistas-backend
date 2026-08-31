namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.CitaMedica
{
    public class CitaMedicaListItemDto
    {
        public int Id { get; set; }
        public int AccidenteId { get; set; }
        public int TipoId { get; set; }
        public string TipoNombre { get; set; } = string.Empty;
        public DateOnly FechaCita { get; set; }
        public TimeOnly? HoraCita { get; set; }
        public string? Clinica { get; set; }
        public string? Medico { get; set; }
        public string? Diagnostico { get; set; }
        public string? Indicaciones { get; set; }
        public DateOnly? ProximaCita { get; set; }
        public string? UrlEvidencia { get; set; }
        public string? Observaciones { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class CitaMedicaCreateDto
    {
        public int TipoId { get; set; }
        public DateOnly FechaCita { get; set; }
        public TimeOnly? HoraCita { get; set; }
        public string? Clinica { get; set; }
        public string? Medico { get; set; }
        public string? Diagnostico { get; set; }
        public string? Indicaciones { get; set; }
        public DateOnly? ProximaCita { get; set; }
        public string? UrlEvidencia { get; set; }
        public string? Observaciones { get; set; }
    }

    public class CitaMedicaUpdateDto
    {
        public int TipoId { get; set; }
        public DateOnly FechaCita { get; set; }
        public TimeOnly? HoraCita { get; set; }
        public string? Clinica { get; set; }
        public string? Medico { get; set; }
        public string? Diagnostico { get; set; }
        public string? Indicaciones { get; set; }
        public DateOnly? ProximaCita { get; set; }
        public string? UrlEvidencia { get; set; }
        public string? Observaciones { get; set; }
    }
}

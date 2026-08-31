namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Emo
{
    public class EmoDetalleDto
    {
        public int Id { get; set; }
        public int WorkerId { get; set; }
        public string? WorkerNombre { get; set; }
        public string? WorkerDni { get; set; }
        public int? TipoEmoId { get; set; }
        public string? TipoEmoNombre { get; set; }
        public int? EmpresaOrigenId { get; set; }
        public string? EmpresaOrigenNombre { get; set; }
        public DateOnly FechaEmo { get; set; }
        public DateOnly? FechaVencimiento { get; set; }
        public DateOnly? FechaVencimientoCalculada { get; set; }
        public int? ClinicaId { get; set; }
        public string? ClinicaNombre { get; set; }
        public int? MedicoId { get; set; }
        public string? MedicoNombre { get; set; }
        public string? Aptitud { get; set; }
        public bool RequiereInterconsulta { get; set; }
        public string? NumeroInforme { get; set; }
        public string? UrlResultado { get; set; }
        public string? UrlAptitud { get; set; }
        public string? UrlEmoCompleto { get; set; }
        public bool RequiereLecturaAbril { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? Notas { get; set; }
        public bool Activo { get; set; }
        public int? DiasParaVencer { get; set; }
        public List<EmoExamenDetalleDto> Examenes { get; set; } = new();
        public List<EmoRestriccionDetalleDto> Restricciones { get; set; } = new();
        public List<EmoConvalidacionResumenDto> Convalidaciones { get; set; } = new();
        public EmoProgramacionDetalleDto? Programacion { get; set; }
        public EmoInterconsultaResumenDto? Interconsulta { get; set; }
    }

    public class EmoProgramacionDetalleDto
    {
        public int Id { get; set; }
        public DateOnly FechaProgramada { get; set; }
        public TimeOnly? HoraProgramada { get; set; }
        public TimeOnly? CheckInHora { get; set; }
        public string? ClinicaNombre { get; set; }
        public string? MedicoNombre { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? Origen { get; set; }
        public string? MotivoRechazo { get; set; }
    }

    public class EmoInterconsultaResumenDto
    {
        public int Id { get; set; }
        public string Especialidad { get; set; } = string.Empty;
        public string? MedicoDeriva { get; set; }
        public DateOnly FechaDerivacion { get; set; }
        public DateOnly? FechaAtencion { get; set; }
        public string? CentroAtencion { get; set; }
        public string? Diagnostico { get; set; }
        public string? Cie10 { get; set; }
        public string? Resultado { get; set; }
        public string Estado { get; set; } = string.Empty;
        public bool RequiereSeguimiento { get; set; }
        public string? UrlInforme { get; set; }
    }
}

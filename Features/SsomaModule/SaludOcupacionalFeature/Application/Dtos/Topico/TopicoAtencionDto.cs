namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Topico
{
    public class TopicoAtencionDto
    {
        public int Id { get; set; }
        public int WorkerId { get; set; }
        public string? WorkerNombre { get; set; }
        public string? WorkerDni { get; set; }
        public DateOnly Fecha { get; set; }
        public TimeOnly? Hora { get; set; }
        public int TipoAtencionId { get; set; }
        public string? TipoAtencionNombre { get; set; }
        public string? Motivo { get; set; }
        public string? Diagnostico { get; set; }
        public string? DiagnosticoCie10 { get; set; }
        public string? Tratamiento { get; set; }
        public string? Medicamentos { get; set; }
        public string? PresionArterial { get; set; }
        public decimal? Temperatura { get; set; }
        public int? FrecuenciaCardiaca { get; set; }
        public int? SaturacionOxigeno { get; set; }
        public decimal? Peso { get; set; }
        public bool DerivadoClinica { get; set; }
        public string? ClinicaDerivacion { get; set; }
        public bool GeneraDescanso { get; set; }
        public int? DescansoDias { get; set; }
        public bool GeneraAccidente { get; set; }
        public int? AccidenteId { get; set; }
        public int? ProyectoId { get; set; }
        public string? ProyectoNombre { get; set; }
        public int? EmpresaId { get; set; }
        public string? EmpresaNombre { get; set; }
        public string? Observaciones { get; set; }
        public int? RegistradoPorId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public class TopicoFiltrosDto
    {
        public DateOnly? FechaDesde { get; set; }
        public DateOnly? FechaHasta { get; set; }
        public int? WorkerId { get; set; }
        public int? TipoAtencionId { get; set; }
        public int? ProyectoId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class CrearTopicoAtencionDto
    {
        public int WorkerId { get; set; }
        public DateOnly Fecha { get; set; }
        public TimeOnly? Hora { get; set; }
        public int TipoAtencionId { get; set; }
        public string? Motivo { get; set; }
        public string? Diagnostico { get; set; }
        public string? DiagnosticoCie10 { get; set; }
        public string? Tratamiento { get; set; }
        public string? Medicamentos { get; set; }
        public string? PresionArterial { get; set; }
        public decimal? Temperatura { get; set; }
        public int? FrecuenciaCardiaca { get; set; }
        public int? SaturacionOxigeno { get; set; }
        public decimal? Peso { get; set; }
        public bool DerivadoClinica { get; set; }
        public string? ClinicaDerivacion { get; set; }
        public bool GeneraDescanso { get; set; }
        public int? DescansoDias { get; set; }
        public bool GeneraAccidente { get; set; }
        public int? ProyectoId { get; set; }
        public int? EmpresaId { get; set; }
        public string? Observaciones { get; set; }
        public IFormFile? ArchivoInforme { get; set; }
    }

    public class ActualizarTopicoAtencionDto
    {
        public DateOnly? Fecha { get; set; }
        public TimeOnly? Hora { get; set; }
        public int? TipoAtencionId { get; set; }
        public string? Motivo { get; set; }
        public string? Diagnostico { get; set; }
        public string? DiagnosticoCie10 { get; set; }
        public string? Tratamiento { get; set; }
        public string? Medicamentos { get; set; }
        public string? PresionArterial { get; set; }
        public decimal? Temperatura { get; set; }
        public int? FrecuenciaCardiaca { get; set; }
        public int? SaturacionOxigeno { get; set; }
        public decimal? Peso { get; set; }
        public bool? DerivadoClinica { get; set; }
        public string? ClinicaDerivacion { get; set; }
        public bool? GeneraDescanso { get; set; }
        public int? DescansoDias { get; set; }
        public bool? GeneraAccidente { get; set; }
        public int? ProyectoId { get; set; }
        public int? EmpresaId { get; set; }
        public string? Observaciones { get; set; }
        public IFormFile? ArchivoInforme { get; set; }
    }
}

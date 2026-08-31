namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Topico
{
    public class TopicoTipoAtencionDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }

    public class TopicoListItemDto
    {
        public int Id { get; set; }
        public int WorkerId { get; set; }
        public string? WorkerNombre { get; set; }
        public string? WorkerDni { get; set; }
        public string? EmpresaNombre { get; set; }
        public string? ProyectoNombre { get; set; }
        public DateOnly Fecha { get; set; }
        public TimeOnly Hora { get; set; }
        public int TipoAtencionId { get; set; }
        public string? TipoAtencionNombre { get; set; }
        public string? Motivo { get; set; }
        public string? Diagnostico { get; set; }
        public bool DerivadoClinica { get; set; }
        public bool GeneraDescanso { get; set; }
        public bool GeneraAccidente { get; set; }
        public bool SctrActivado { get; set; }
        public string? UrlInforme { get; set; }
        public int? DescansoGeneradoId { get; set; }
        public string Estado { get; set; } = "Abierta";
        public DateTimeOffset? FechaCierre { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class TopicoDetalleDto
    {
        public int Id { get; set; }
        public int WorkerId { get; set; }
        public string? WorkerNombre { get; set; }
        public string? WorkerDni { get; set; }
        public int? ProyectoId { get; set; }
        public string? ProyectoNombre { get; set; }
        public int? EmpresaId { get; set; }
        public string? EmpresaNombre { get; set; }
        public DateOnly Fecha { get; set; }
        public TimeOnly Hora { get; set; }
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
        public string? Observaciones { get; set; }
        public bool SctrActivado { get; set; }
        public string? TipoCasoSctr { get; set; }
        public string? UrlInforme { get; set; }
        public int? DescansoGeneradoId { get; set; }
        public string Estado { get; set; } = "Abierta";
        public int? CerradoPorId { get; set; }
        public DateTimeOffset? FechaCierre { get; set; }
        public int RegistradoPorId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public class TopicoCreateDto
    {
        public int WorkerId { get; set; }
        public DateOnly Fecha { get; set; }
        public TimeOnly Hora { get; set; }
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
        public bool SctrActivado { get; set; }
        public string? TipoCasoSctr { get; set; }
        // UrlInforme se asigna en el controller tras subir el archivo
        public string? UrlInforme { get; set; }
    }

    public class TopicoUpdateDto
    {
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
        public bool SctrActivado { get; set; }
        public string? TipoCasoSctr { get; set; }
        public string? UrlInforme { get; set; }
    }

    public class TopicoEvolucionDto
    {
        public int Id { get; set; }
        public int AtencionId { get; set; }
        public DateTimeOffset FechaEvolucion { get; set; }
        public string NotaEvolucion { get; set; } = string.Empty;
        public int? RegistradoPorId { get; set; }
        public string? RegistradoPorNombre { get; set; }
        public string? UrlEvidencia { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class TopicoEvolucionCreateDto
    {
        public string NotaEvolucion { get; set; } = string.Empty;
        // UrlEvidencia se asigna en controller tras subir el archivo
        public string? UrlEvidencia { get; set; }
    }

    public class TopicoFilterDto
    {
        public int? WorkerId { get; set; }
        public DateOnly? FechaDesde { get; set; }
        public DateOnly? FechaHasta { get; set; }
        public int? TipoAtencionId { get; set; }
        public int? EmpresaId { get; set; }
        public int? ProyectoId { get; set; }
        public string? Estado { get; set; }
        public int Page { get; set; } = 1;
    }
}

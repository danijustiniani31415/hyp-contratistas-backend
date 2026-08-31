namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Accidente
{
    public class AccidenteTrabajoDto
    {
        public int Id { get; set; }
        public int WorkerId { get; set; }
        public string? WorkerNombre { get; set; }
        public string? WorkerDni { get; set; }
        public DateOnly FechaAccidente { get; set; }
        public TimeOnly? HoraAccidente { get; set; }
        public int? ProyectoId { get; set; }
        public string? ProyectoNombre { get; set; }
        public int? EmpresaId { get; set; }
        public string? EmpresaNombre { get; set; }
        public string? LugarAccidente { get; set; }
        public string? TipoAccidente { get; set; }
        public string? Mecanismo { get; set; }
        public string? ParteCuerpoAfectada { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string? DescripcionLesion { get; set; }
        public bool RequiereHospitalizacion { get; set; }
        public string? HospitalNombre { get; set; }
        public int? AtencionTopicoId { get; set; }
        public int? DiasDescansoEstimados { get; set; }
        public int? DiasDescansoReales { get; set; }
        public string Estado { get; set; } = "Registrado";
        public DateOnly? FechaAlta { get; set; }
        public string? RestriccionesReintegro { get; set; }
        public bool NotificadoSunafil { get; set; }
        public DateOnly? FechaNotificacionSunafil { get; set; }
        public string? NumeroNotificacionSunafil { get; set; }
        public int? PasoId { get; set; }
        public string? UrlInforme { get; set; }
        public int RegistradoPorId { get; set; }
        public int? CerradoPorId { get; set; }
        public DateTimeOffset? FechaCierre { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public List<AccidenteSeguimientoDto> Seguimientos { get; set; } = new();
    }

    public class AccidenteSeguimientoDto
    {
        public int Id { get; set; }
        public int AccidenteId { get; set; }
        public DateOnly Fecha { get; set; }
        public string? Tipo { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public DateOnly? ProximaCita { get; set; }
        public int RegistradoPorId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class AccidenteFiltrosDto
    {
        public DateOnly? FechaDesde { get; set; }
        public DateOnly? FechaHasta { get; set; }
        public int? WorkerId { get; set; }
        public string? Estado { get; set; }
        public string? TipoAccidente { get; set; }
        public int? ProyectoId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class CrearAccidenteTrabajoDto
    {
        public int WorkerId { get; set; }
        public DateOnly FechaAccidente { get; set; }
        public TimeOnly? HoraAccidente { get; set; }
        public int? ProyectoId { get; set; }
        public int? EmpresaId { get; set; }
        public string? LugarAccidente { get; set; }
        public string? TipoAccidente { get; set; }
        public string? Mecanismo { get; set; }
        public string? ParteCuerpoAfectada { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string? DescripcionLesion { get; set; }
        public bool RequiereHospitalizacion { get; set; }
        public string? HospitalNombre { get; set; }
        public int? AtencionTopicoId { get; set; }
        public int? DiasDescansoEstimados { get; set; }
        public string? RestriccionesReintegro { get; set; }
        public string? UrlInforme { get; set; }
        public IFormFile? Documento { get; set; }
    }

    public class ActualizarAccidenteTrabajoDto
    {
        public DateOnly? FechaAccidente { get; set; }
        public TimeOnly? HoraAccidente { get; set; }
        public int? ProyectoId { get; set; }
        public int? EmpresaId { get; set; }
        public string? LugarAccidente { get; set; }
        public string? TipoAccidente { get; set; }
        public string? Mecanismo { get; set; }
        public string? ParteCuerpoAfectada { get; set; }
        public string? Descripcion { get; set; }
        public string? DescripcionLesion { get; set; }
        public bool? RequiereHospitalizacion { get; set; }
        public string? HospitalNombre { get; set; }
        public int? DiasDescansoEstimados { get; set; }
        public int? DiasDescansoReales { get; set; }
        public string? RestriccionesReintegro { get; set; }
        public bool? NotificadoSunafil { get; set; }
        public DateOnly? FechaNotificacionSunafil { get; set; }
        public string? NumeroNotificacionSunafil { get; set; }
        public IFormFile? Documento { get; set; }
    }

    public class CambiarEstadoAccidenteDto
    {
        public string Estado { get; set; } = string.Empty;
        public string? Observacion { get; set; }
    }

    public class CrearAccidenteSeguimientoDto
    {
        public DateOnly Fecha { get; set; }
        public string? Tipo { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public DateOnly? ProximaCita { get; set; }
    }
}

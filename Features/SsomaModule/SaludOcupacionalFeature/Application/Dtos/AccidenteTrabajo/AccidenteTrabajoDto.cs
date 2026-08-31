using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.AltaMedica;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.CitaMedica;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.EquipoPrestado;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.DescansoMedico;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.AccidenteTrabajo
{
    public class AccidenteTrabajoListItemDto
    {
        public int Id { get; set; }
        public int WorkerId { get; set; }
        public string? WorkerNombre { get; set; }
        public string? WorkerDni { get; set; }
        public string? EmpresaNombre { get; set; }
        public string? ProyectoNombre { get; set; }
        public DateOnly FechaAccidente { get; set; }
        public string? TipoAccidente { get; set; }
        public string? LugarAccidente { get; set; }
        public string Estado { get; set; } = string.Empty;
        public bool NotificadoSunafil { get; set; }
        public int TotalSeguimientos { get; set; }
        public int? FlashReportId { get; set; }
        public bool TieneAlta { get; set; }
        public bool RequiereReinduccion { get; set; }
        public bool ReinduccionCompletada { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class AccidenteTrabajoDetalleDto
    {
        public int Id { get; set; }
        public int WorkerId { get; set; }
        public string? WorkerNombre { get; set; }
        public string? WorkerDni { get; set; }
        public string? WorkerTelefono { get; set; }
        public int? ProyectoId { get; set; }
        public string? ProyectoNombre { get; set; }
        public int? EmpresaId { get; set; }
        public string? EmpresaNombre { get; set; }
        public DateOnly FechaAccidente { get; set; }
        public TimeOnly? HoraAccidente { get; set; }
        public string? LugarAccidente { get; set; }
        public string? TipoAccidente { get; set; }
        public string? Mecanismo { get; set; }
        public string? ParteCuerpoAfectada { get; set; }
        public int? AgenteRiesgoId { get; set; }
        public string? AgenteRiesgoNombre { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string? DescripcionLesion { get; set; }
        public string? DiagnosticoCie10 { get; set; }
        public bool RequiereHospitalizacion { get; set; }
        public string? HospitalNombre { get; set; }
        public int? AtencionTopicoId { get; set; }
        public int DiasDescansoEstimados { get; set; }
        public int? DiasDescansoReales { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateOnly? FechaAlta { get; set; }
        public string? RestriccionesReintegro { get; set; }
        public bool NotificadoSunafil { get; set; }
        public DateOnly? FechaNotificacionSunafil { get; set; }
        public string? NumeroNotificacionSunafil { get; set; }
        public string? UrlInforme { get; set; }
        public bool RequiereReinduccion { get; set; }
        public bool ReinduccionCompletada { get; set; }
        public DateOnly? FechaReinduccion { get; set; }
        public int? FlashReportId { get; set; }
        public bool TieneAlta { get; set; }
        public Guid? CasoSocialId { get; set; }
        public int RegistradoPorId { get; set; }
        public int? CerradoPorId { get; set; }
        public DateTimeOffset? FechaCierre { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public List<AccidenteSeguimientoDto> Seguimientos { get; set; } = [];
        public List<DescansoMedicoListItemDto> Descansos { get; set; } = [];
        public List<CitaMedicaListItemDto> Citas { get; set; } = [];
        public List<EquipoPrestadoListItemDto> Equipos { get; set; } = [];
        public AltaMedicaDto? AltaMedica { get; set; }
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

    public class AccidenteTrabajoCreateDto
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
        public int? AgenteRiesgoId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string? DescripcionLesion { get; set; }
        public string? DiagnosticoCie10 { get; set; }
        public bool RequiereHospitalizacion { get; set; }
        public string? HospitalNombre { get; set; }
        public int DiasDescansoEstimados { get; set; }
    }

    public class AccidenteTrabajoUpdateDto
    {
        public string? LugarAccidente { get; set; }
        public string? TipoAccidente { get; set; }
        public string? Mecanismo { get; set; }
        public string? ParteCuerpoAfectada { get; set; }
        public int? AgenteRiesgoId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string? DescripcionLesion { get; set; }
        public string? DiagnosticoCie10 { get; set; }
        public bool RequiereHospitalizacion { get; set; }
        public string? HospitalNombre { get; set; }
        public int DiasDescansoEstimados { get; set; }
        public int? DiasDescansoReales { get; set; }
        public bool NotificadoSunafil { get; set; }
        public DateOnly? FechaNotificacionSunafil { get; set; }
        public string? NumeroNotificacionSunafil { get; set; }
        public string? RestriccionesReintegro { get; set; }
    }

    public class AccidenteCerrarDto
    {
        public DateOnly FechaAlta { get; set; }
        public string? RestriccionesReintegro { get; set; }
        public int? DiasDescansoReales { get; set; }
    }

    public class AccidenteSeguimientoCreateDto
    {
        public DateOnly Fecha { get; set; }
        public string? Tipo { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public DateOnly? ProximaCita { get; set; }
    }

    public class AccidenteFilterDto
    {
        public int? WorkerId { get; set; }
        public string? Estado { get; set; }
        public string? TipoAccidente { get; set; }
        public int? EmpresaId { get; set; }
        public int? ProyectoId { get; set; }
        public DateOnly? FechaDesde { get; set; }
        public DateOnly? FechaHasta { get; set; }
        public int Page { get; set; } = 1;
    }
}

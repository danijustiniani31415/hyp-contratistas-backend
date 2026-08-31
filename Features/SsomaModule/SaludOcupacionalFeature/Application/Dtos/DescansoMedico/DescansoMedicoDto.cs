using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.SsomaModule.Shared;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.DescansoMedico
{
    public class DescansoMedicoListItemDto
    {
        public int Id { get; set; }
        public int CasoId { get; set; }
        public int WorkerId { get; set; }
        public string? WorkerNombre { get; set; }
        public string? WorkerDni { get; set; }
        public string? EmpresaNombre { get; set; }
        public int TipoId { get; set; }
        /// <summary>Nombre del tipo resuelto desde el catálogo (ss_descanso_tipo).</summary>
        public string Tipo { get; set; } = string.Empty;
        public DateOnly FechaInicio { get; set; }
        public DateOnly FechaFin { get; set; }
        public int Dias { get; set; }
        public string Estado { get; set; } = string.Empty;
        public bool ReportadoPorTrabajador { get; set; }
        public int? TopicoOrigenId { get; set; }
        public bool TrabajadorBloqueado { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class DescansoMedicoDetalleDto
    {
        public int Id { get; set; }
        public int CasoId { get; set; }
        public int WorkerId { get; set; }
        public string? WorkerNombre { get; set; }
        public string? WorkerDni { get; set; }
        public int? ProyectoId { get; set; }
        public int? EmpresaId { get; set; }
        public string? EmpresaNombre { get; set; }
        public int TipoId { get; set; }
        /// <summary>Nombre del tipo resuelto desde el catálogo (ss_descanso_tipo).</summary>
        public string Tipo { get; set; } = string.Empty;
        public DateOnly FechaInicio { get; set; }
        public DateOnly FechaFin { get; set; }
        public int Dias { get; set; }
        public string? Diagnostico { get; set; }
        /// <summary>LEGACY, ver DiagnosticoCie10Codigo.</summary>
        public string? DiagnosticoCie10 { get; set; }
        /// <summary>FK a cie10_catalogo. Solo el médico lo asigna al revisar.</summary>
        public string? DiagnosticoCie10Codigo { get; set; }
        public string? DiagnosticoCie10Descripcion { get; set; }
        public string? UrlCertificado { get; set; }
        public string? UrlDocumento { get; set; }
        /// <summary>Certificados médicos adjuntos (ss_descanso_medico_adjunto).</summary>
        public List<DescansoAdjuntoDto> Adjuntos { get; set; } = [];
        public string Estado { get; set; } = string.Empty;
        public string? MotivoRechazo { get; set; }
        public int? AprobadoPorId { get; set; }
        public DateTimeOffset? FechaAprobacion { get; set; }
        public int? AccidenteId { get; set; }
        public bool EsRecaida { get; set; }
        public bool NotificadoGth { get; set; }
        public bool NotificadoJefe { get; set; }
        public bool ReportadoPorTrabajador { get; set; }
        public string? Observaciones { get; set; }
        public int? TopicoOrigenId { get; set; }
        public int? ProrrogaDelId { get; set; }
        public DateOnly? FechaAlta { get; set; }
        public int? AltaPorId { get; set; }
        public string? AltaObservaciones { get; set; }
        public int RegistradoPorId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public class DescansoAdjuntoDto
    {
        /// <summary>
        /// Id del adjunto (ss_descanso_medico_adjunto). El frontend pide el archivo por este id
        /// al endpoint de descarga en vez de apuntar al link de SharePoint, que solo abre para
        /// quien ya tiene sesión de Microsoft 365 en el navegador.
        /// </summary>
        public int Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? Nombre { get; set; }
    }

    /// <summary>Datos mínimos de un adjunto para servir su contenido desde el backend.</summary>
    public class DescansoAdjuntoArchivoDto
    {
        public string Url { get; set; } = string.Empty;
        public string? NombreArchivo { get; set; }
        public string? DriveId { get; set; }
        public string? ItemId { get; set; }
    }

    public class DarAltaDto
    {
        public string? Observaciones { get; set; }
    }

    public class DescansoSeguimientoDto
    {
        public int Id { get; set; }
        public int DescansoId { get; set; }
        public int CasoId { get; set; }
        public DateTimeOffset FechaSeguimiento { get; set; }
        /// <summary>LEGACY, ver TipoId.</summary>
        public string Tipo { get; set; } = string.Empty;
        public int? TipoId { get; set; }
        public string? TipoNombre { get; set; }
        public string? RealizadoPorRol { get; set; }
        public int? RealizadoPorId { get; set; }
        /// <summary>Null si Confidencial=true y quien pide no tiene permiso de ver detalle
        /// clínico — ver DescansoMedicoRepository.GetSeguimientosPorCaso.</summary>
        public string? Nota { get; set; }
        public DateOnly? ProximaCita { get; set; }
        public string? UrlEvidencia { get; set; }
        public string? DiagnosticoCie10Codigo { get; set; }
        public string? DiagnosticoCie10Descripcion { get; set; }
        public string? PuestoTrabajoSnapshot { get; set; }
        public bool Confidencial { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class DescansoSeguimientoCreateDto
    {
        /// <summary>Sobre cuál descanso puntual del caso se hace la nota — si no se envía, se
        /// asume el descanso más reciente del caso.</summary>
        public int? DescansoId { get; set; }
        /// <summary>Sin uso — el único que registra seguimiento es el médico, así que no hace
        /// falta clasificar "quién" lo hizo. Se conserva nullable solo por compatibilidad con
        /// datos históricos (ver SsDescansoSeguimiento.TipoId), no se pide más en el formulario.</summary>
        public int? TipoId { get; set; }
        public string? Nota { get; set; }
        public DateOnly? ProximaCita { get; set; }
        // UrlEvidencia se asigna en controller tras subir el archivo
        public string? UrlEvidencia { get; set; }
        public string? DiagnosticoCie10Codigo { get; set; }
        public bool Confidencial { get; set; } = true;
    }

    // ── Caso clínico ─────────────────────────────────────────────────────────

    public class CasoDetalleDto
    {
        public int Id { get; set; }
        public int WorkerId { get; set; }
        public string? WorkerNombre { get; set; }
        public string? WorkerDni { get; set; }
        public DateOnly FechaApertura { get; set; }
        /// <summary>"Abierto" | "Cerrado".</summary>
        public string Estado { get; set; } = string.Empty;
        public DateOnly? FechaCierre { get; set; }
        public int? AltaPorId { get; set; }
        public string? AltaObservaciones { get; set; }
        public DateOnly? FechaReapertura { get; set; }
        public List<DescansoMedicoListItemDto> Descansos { get; set; } = [];
        public List<DescansoSeguimientoDto> Seguimientos { get; set; } = [];
    }

    public class ReabrirCasoDto
    {
        public string? Observaciones { get; set; }
    }

    public class SeguimientoTipoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    public class Cie10Dto
    {
        public string Codigo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
    }

    public class AsignarCie10Request
    {
        public string? Codigo { get; set; }
    }

    /// <summary>Un caso candidato para vincular un descanso suelto (el que crea el trabajador
    /// al subir desde Mi Salud, que nace como caso propio de un solo descanso).</summary>
    public class CasoCandidatoDto
    {
        public int Id { get; set; }
        public DateOnly FechaApertura { get; set; }
        /// <summary>Fechas/tipo del primer descanso del caso, para que el médico reconozca cuál
        /// es sin tener que abrir cada uno.</summary>
        public DateOnly PrimerDescansoInicio { get; set; }
        public DateOnly PrimerDescansoFin { get; set; }
        public string PrimerDescansoTipo { get; set; } = string.Empty;
    }

    public class VincularCasoRequest
    {
        public int CasoDestinoId { get; set; }
    }

    public class DescansoMedicoCreateDto
    {
        public int WorkerId { get; set; }
        /// <summary>Tipo del catálogo ss_descanso_tipo. Obligatorio.</summary>
        public int TipoId { get; set; }
        public DateOnly FechaInicio { get; set; }
        public DateOnly FechaFin { get; set; }
        public string? Diagnostico { get; set; }
        /// <summary>LEGACY, ver DiagnosticoCie10Codigo.</summary>
        public string? DiagnosticoCie10 { get; set; }
        /// <summary>FK a cie10_catalogo. No lo llena el trabajador — se asigna al aprobar/revisar.</summary>
        public string? DiagnosticoCie10Codigo { get; set; }
        public int? AccidenteId { get; set; }
        public bool EsRecaida { get; set; } = false;
        public int? TopicoOrigenId { get; set; }
        /// <summary>"Añadir más descanso" sobre un caso abierto: id del descanso que se extiende.
        /// Si se envía, el nuevo descanso hereda el CasoId de ese descanso.</summary>
        public int? ProrrogaDelId { get; set; }
        /// <summary>Solo para el flujo de reapertura: registrar un descanso nuevo directamente
        /// sobre un caso ya reabierto (sin que sea "prórroga" de un descanso puntual anterior).</summary>
        public int? CasoId { get; set; }
        public int? ProyectoId { get; set; }
        public int? EmpresaId { get; set; }
        /// <summary>Certificados médicos. Se suben en el controller y se guardan como adjuntos.</summary>
        public List<IFormFile>? Documentos { get; set; }
    }

    public class DescansoMedicoUpdateDto
    {
        public int TipoId { get; set; }
        public DateOnly FechaInicio { get; set; }
        public DateOnly FechaFin { get; set; }
        public string? Diagnostico { get; set; }
        /// <summary>LEGACY, ver DiagnosticoCie10Codigo.</summary>
        public string? DiagnosticoCie10 { get; set; }
        /// <summary>FK a cie10_catalogo — lo asigna el médico al revisar el caso.</summary>
        public string? DiagnosticoCie10Codigo { get; set; }
    }

    public class DescansoAprobarDto
    {
        public string? Observaciones { get; set; }
    }

    public class DescansoRechazarDto
    {
        public string MotivoRechazo { get; set; } = string.Empty;
    }

    public class DescansoMedicoFilterDto
    {
        public int? WorkerId { get; set; }
        public string? Estado { get; set; }
        public int? TipoId { get; set; }
        public int? EmpresaId { get; set; }
        public DateOnly? FechaDesde { get; set; }
        public DateOnly? FechaHasta { get; set; }
        public int Page { get; set; } = 1;
    }

    /// <summary>
    /// Carga inicial de la pantalla de Descansos: catálogo de tipos (para el filtro y el
    /// formulario) + primera página de la tabla, en una sola petición. Los cambios de
    /// filtro/página después usan el endpoint de solo-tabla.
    /// </summary>
    public class DescansosInicioDto
    {
        public List<DescansoTipoDto> Tipos { get; set; } = [];
        public PagedResult<DescansoMedicoListItemDto> Descansos { get; set; } = new();
    }
}

namespace Abril_Backend.Features.SsomaModule.AccidentesIncidentesFeature.Application.Dtos;

// ── Catálogos para inicializar el formulario ──────────────────────────────────

public class CatalogoItemDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Codigo { get; set; }
}

public class FlashProyectoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Abreviatura { get; set; }
    public string? EmailCoordSsoma { get; set; }
}

public class ContratistaCatalogoDto
{
    public int Id { get; set; }
    public string RazonSocial { get; set; } = string.Empty;
    public string? Ruc { get; set; }
}

public class TrabajadorCatalogoDto
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Documento { get; set; }
    public string? Cargo { get; set; }
    public int? Edad { get; set; }
    public int? AniosExperiencia { get; set; }
    public int? ContributorId { get; set; }
}

public class ProyectoContratistaDto
{
    public int ProyectoId { get; set; }
    public int ContributorId { get; set; }
}

public class FlashReportInicializarDto
{
    public List<FlashProyectoDto> Proyectos { get; set; } = [];
    public List<CatalogoItemDto> Tipos { get; set; } = [];
    public List<CatalogoItemDto> EtapasProyecto { get; set; } = [];
    public List<CatalogoItemDto> PartesAfectadas { get; set; } = [];
    public List<CatalogoItemDto> EmpresasAbril { get; set; } = [];
    public List<CatalogoItemDto> Partidas { get; set; } = [];
    public List<ContratistaCatalogoDto> Contratistas { get; set; } = [];
    public List<TrabajadorCatalogoDto> Trabajadores { get; set; } = [];
    public List<ProyectoContratistaDto> ProyectoContratistas { get; set; } = [];
}

// ── Trabajadores afectados ────────────────────────────────────────────────────

public class TrabajadorAfectadoDto
{
    public int Id { get; set; }
    public int? WorkerId { get; set; }
    public string TrabajadorNombre { get; set; } = string.Empty;
    public string? PuestoTrabajo { get; set; }
    public int? Edad { get; set; }
    public int? AniosExperiencia { get; set; }
    public string? CelularTrabajador { get; set; }
    public int? ParteAfectadaId { get; set; }
    public string? ParteAfectadaNombre { get; set; }
}

public class TrabajadorAfectadoRequest
{
    public int? WorkerId { get; set; }
    public string TrabajadorNombre { get; set; } = string.Empty;
    public string? PuestoTrabajo { get; set; }
    public int? Edad { get; set; }
    public int? AniosExperiencia { get; set; }
    public string? CelularTrabajador { get; set; }
    public int? ParteAfectadaId { get; set; }
}

// ── Lista ─────────────────────────────────────────────────────────────────────

public class FlashReportListItemDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string ProyectoNombre { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string TipoNombre { get; set; } = string.Empty;
    public string TipoCodigo { get; set; } = string.Empty;
    public string AreaOrigen { get; set; } = "Produccion";
    public string? TrabajadorNombre { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int? AccidenteTrabajoId { get; set; }
    public bool Enviado { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public int? ConsecuenciaRealPersonal { get; set; }
    public int? ConsecuenciaPotencialPersonal { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int DiasPerdidos { get; set; }

    // Estado del accidente vinculado en Salud Ocupacional (ss_accidente_trabajo):
    // null = no aplica (no es un accidente con seguimiento médico, ej. incidente/NC/alerta)
    // false = tiene seguimiento pero aún no cerró con alta médica
    // true = cerrado con alta médica
    public bool? CerradoConAltaMedica { get; set; }
}

// ── Detalle ───────────────────────────────────────────────────────────────────

public class DescansoDto
{
    public int Id { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string? Observacion { get; set; }
    public int DiasDescanso => (FechaFin - FechaInicio).Days + 1;
}

public class FlashReportDetalleDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public int ProyectoId { get; set; }
    public string ProyectoNombre { get; set; } = string.Empty;
    public string? ProyectoAbreviatura { get; set; }
    public int TipoId { get; set; }
    public string TipoNombre { get; set; } = string.Empty;
    public string TipoCodigo { get; set; } = string.Empty;
    public string AreaOrigen { get; set; } = "Produccion";
    public DateTime Fecha { get; set; }
    public TimeSpan? Hora { get; set; }
    public string LugarExacto { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;

    public int? EmpresaAbrilId { get; set; }
    public string? EmpresaAbrilNombre { get; set; }
    public int? ContributorId { get; set; }
    public string? ContributorNombre { get; set; }
    public string? JefeInmediatoNombre { get; set; }

    public int? EtapaProyectoId { get; set; }
    public string? EtapaProyectoNombre { get; set; }
    public int? PartidaId { get; set; }
    public string? PartidaNombre { get; set; }

    public int? WorkerId { get; set; }
    public string? TrabajadorNombre { get; set; }
    public string? PuestoTrabajo { get; set; }
    public int? Edad { get; set; }
    public int? AniosExperiencia { get; set; }
    public string? CelularTrabajador { get; set; }
    public int? ParteAfectadaId { get; set; }
    public string? ParteAfectadaNombre { get; set; }

    public string? Turno { get; set; }
    public string? TipoContacto { get; set; }
    public bool DanioProcesoFlag { get; set; }
    public string? AtencionMedica { get; set; }
    public string? CentroAtencion { get; set; }

    public string? DanoProceso { get; set; }
    public int? ConsecuenciaRealPersonal { get; set; }
    public int? ConsecuenciaPotencialPersonal { get; set; }
    public string? AccionesInmediatas { get; set; }

    public int? ElaboradoPorId { get; set; }
    public string? ElaboradoPorNombre { get; set; }
    public string? ElaboradoPorCargo { get; set; }
    public string? ElaboradoPorEmail { get; set; }
    public string? ElaboradoPorTelefono { get; set; }

    public string? UrlFoto1 { get; set; }
    public string? UrlFoto2 { get; set; }

    public bool Enviado { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public string? UrlPdfSharepoint { get; set; }

    public List<DescansoDto> Descansos { get; set; } = [];
    public List<TrabajadorAfectadoDto> Trabajadores { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

// ── Requests ──────────────────────────────────────────────────────────────────

public class DescansoRequest
{
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string? Observacion { get; set; }
}

public class CrearFlashReportRequest
{
    public int ProyectoId { get; set; }
    public int TipoId { get; set; }
    public string AreaOrigen { get; set; } = "Produccion"; // Produccion | PostVenta | ArquitecturaComercial
    public DateTime Fecha { get; set; }
    public string? Hora { get; set; }   // "HH:mm"
    public string LugarExacto { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;

    public int? EmpresaAbrilId { get; set; }
    public int? ContributorId { get; set; }
    public string? JefeInmediatoNombre { get; set; }

    public int? EtapaProyectoId { get; set; }
    public int? PartidaId { get; set; }

    public int? WorkerId { get; set; }
    public string? TrabajadorNombre { get; set; }
    public string? PuestoTrabajo { get; set; }
    public int? Edad { get; set; }
    public int? AniosExperiencia { get; set; }
    public string? CelularTrabajador { get; set; }
    public int? ParteAfectadaId { get; set; }

    public string? Turno { get; set; }
    public string? TipoContacto { get; set; }
    public bool DanioProcesoFlag { get; set; }
    public string? AtencionMedica { get; set; }
    public string? CentroAtencion { get; set; }

    public string? DanoProceso { get; set; }
    public int? ConsecuenciaRealPersonal { get; set; }
    public int? ConsecuenciaPotencialPersonal { get; set; }
    public string? AccionesInmediatas { get; set; }

    public string? ElaboradoPorNombre { get; set; }
    public string? ElaboradoPorCargo { get; set; }
    public string? ElaboradoPorEmail { get; set; }
    public string? ElaboradoPorTelefono { get; set; }

    // base64 o null
    public string? Foto1Base64 { get; set; }
    public string? Foto2Base64 { get; set; }

    public List<DescansoRequest> Descansos { get; set; } = [];
    public List<TrabajadorAfectadoRequest> Trabajadores { get; set; } = [];
}

public class ActualizarFlashReportRequest : CrearFlashReportRequest { }

public class ActualizarSeveridadRequest
{
    public int? ConsecuenciaRealPersonal { get; set; }
    public int? ConsecuenciaPotencialPersonal { get; set; }
}

// ── Aliases legacy (no romper código existente) ───────────────────────────────

public class AccidenteIncidenteListItemDto : FlashReportListItemDto { }
public class AccidenteIncidenteDetalleDto : FlashReportDetalleDto { }
public class CrearAccidenteIncidenteRequest : CrearFlashReportRequest { }
public class ActualizarAccidenteIncidenteRequest : ActualizarFlashReportRequest { }

// ── Entregables ───────────────────────────────────────────────────────────────

public class EntregableResponsableDto
{
    public int Id { get; set; }
    public int? WorkerId { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class EntregableArchivoDto
{
    public int Id { get; set; }
    public string UrlArchivo { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class EntregableDto
{
    public int Id { get; set; }
    public int TipoId { get; set; }
    public string TipoNombre { get; set; } = string.Empty;
    public int Orden { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateOnly? FechaLimite { get; set; }
    public string? Observacion { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<EntregableResponsableDto> Responsables { get; set; } = [];
    public List<EntregableArchivoDto> Archivos { get; set; } = [];
}

public class ActualizarEntregableRequest
{
    public string Estado { get; set; } = string.Empty;
    public DateOnly? FechaLimite { get; set; }
    public string? Observacion { get; set; }
    public List<string> Responsables { get; set; } = []; // nombres libres
    public List<int> ResponsableWorkerIds { get; set; } = [];
}

// ── RM-050 ────────────────────────────────────────────────────────────────────

public class AccionCorrectivaDto
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public string? ResponsableNombre { get; set; }
    public int? ResponsableWorkerId { get; set; }
    public DateOnly? FechaCompromiso { get; set; }
    public DateOnly? FechaCumplimiento { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public string? EvidenciaUrl { get; set; }
}

public class Rm050Dto
{
    public int? Id { get; set; }
    public string? DescripcionDetallada { get; set; }
    public string? Mecanismo { get; set; }
    public string? AgenteCausante { get; set; }
    public string? ActosSubestandar { get; set; }
    public string? CondicionesSubestandar { get; set; }
    public string? FactoresPersonales { get; set; }
    public string? FactoresTrabajo { get; set; }
    public int? DiasPerdidos { get; set; }
    public string? TipoAccidente { get; set; }
    public string? GravedadAccidente { get; set; }
    public int? NroTrabajadoresAfectados { get; set; }
    public string? Testigos { get; set; }
    public string? ElaboradoPorNombre { get; set; }
    public string? ElaboradoPorCargo { get; set; }
    public DateOnly? ElaboradoPorFecha { get; set; }
    public string? AprobadoPorNombre { get; set; }
    public string? AprobadoPorCargo { get; set; }
    public string Estado { get; set; } = "Borrador";
    public DateTime? UpdatedAt { get; set; }
    public List<AccionCorrectivaDto> AccionesCorrectivas { get; set; } = [];
}

public class GuardarAccionCorrectivaRequest
{
    public int? Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public string? ResponsableNombre { get; set; }
    public int? ResponsableWorkerId { get; set; }
    public DateOnly? FechaCompromiso { get; set; }
    public DateOnly? FechaCumplimiento { get; set; }
    public string Estado { get; set; } = "Pendiente";
}

public class GuardarRm050Request
{
    public string? DescripcionDetallada { get; set; }
    public string? Mecanismo { get; set; }
    public string? AgenteCausante { get; set; }
    public string? ActosSubestandar { get; set; }
    public string? CondicionesSubestandar { get; set; }
    public string? FactoresPersonales { get; set; }
    public string? FactoresTrabajo { get; set; }
    public int? DiasPerdidos { get; set; }
    public string? TipoAccidente { get; set; }
    public string? GravedadAccidente { get; set; }
    public int? NroTrabajadoresAfectados { get; set; }
    public string? Testigos { get; set; }
    public string? ElaboradoPorNombre { get; set; }
    public string? ElaboradoPorCargo { get; set; }
    public DateOnly? ElaboradoPorFecha { get; set; }
    public string? AprobadoPorNombre { get; set; }
    public string? AprobadoPorCargo { get; set; }
    public List<GuardarAccionCorrectivaRequest> AccionesCorrectivas { get; set; } = [];
}

// ── SubirDocumento (legacy, mantener por compatibilidad) ──────────────────────
public class SubirDocumentoRequest
{
    public string NombreArchivo { get; set; } = string.Empty;
    public string TipoArchivo { get; set; } = string.Empty;
    public string ContenidoBase64 { get; set; } = string.Empty;
}

public class DocumentoAdjuntoDto
{
    public int Id { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string TipoArchivo { get; set; } = string.Empty;
    public long TamanioBytes { get; set; }
    public string UrlSharepoint { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AccionCorrectivaVencidaDto
{
    public int AccionId { get; set; }
    public int AccidenteId { get; set; }
    public string CodigoAccidente { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public string? ResponsableNombre { get; set; }
    public DateOnly? FechaCompromiso { get; set; }
    public int DiasVencida { get; set; }
    public string Estado { get; set; } = string.Empty;
}

public class CrearLeccionDesdeAccionRequest
{
    public int AccionCorrectivaId { get; set; }
    public int ProyectoId { get; set; }
    public int AreaId { get; set; }
    public string? ImpactDescription { get; set; }
}

namespace Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Application.Dtos;

// ── Catálogos (init) ───────────────────────────────────────────────────────

public class AmonestacionInitDto
{
    public List<TipoSancionDto> TiposSancion { get; set; } = new();
    public List<InfraccionTipoDto> InfraccionesTipo { get; set; } = new();
    public List<AmonCatalogoDto> RacInfracciones { get; set; } = new();   // para penalización (monto)
    public List<AmonCatalogoDto> Proyectos { get; set; } = new();
    public List<AmonPartidaDto> Partidas { get; set; } = new();
    public decimal UitActual { get; set; }
}

public class TipoSancionDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string NivelGravedad { get; set; } = "";
    public bool GeneraSuspension { get; set; }
}

public class InfraccionTipoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
}

public class AmonCatalogoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public decimal? MontoFijo { get; set; }
    public decimal? FactorUit { get; set; }
}

public class AmonPartidaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
}

// ── Crear ──────────────────────────────────────────────────────────────────

public class AmonestacionCreateRequest
{
    public int ProyectoId { get; set; }
    public string Fecha { get; set; } = "";          // "yyyy-MM-dd"
    public int WorkerId { get; set; }
    public int? PartidaId { get; set; }
    public int TipoSancionId { get; set; }
    public int InfraccionTipoId { get; set; }
    public string Descripcion { get; set; } = "";
    public bool AplicaPenalizacion { get; set; }
    public int? SancionInfraccionId { get; set; }
    public int PuntosInfraccion { get; set; }
    public int? DiasSuspension { get; set; }
    public string? FechaInicioSuspension { get; set; }  // "yyyy-MM-dd"
    public string? FechaFinSuspension { get; set; }
    // Fotos como base64
    public List<AmonFotoUploadDto> Fotos { get; set; } = new();
    // "Borrador" | "Registrada" (default)
    public string Estado { get; set; } = "Registrada";
}

public class AmonFotoUploadDto
{
    public string Base64 { get; set; } = "";
    public string NombreArchivo { get; set; } = "";
}

public class AmonestacionCreadaDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
}

// ── Lista ──────────────────────────────────────────────────────────────────

public class AmonestacionListQuery
{
    public int? ProyectoId { get; set; }
    public int? WorkerId { get; set; }
    public int? TipoSancionId { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public string? WorkerSearch { get; set; }   // DNI o nombre
    public string? EmpresaNombre { get; set; }
    public string? Estado { get; set; }   // "Borrador" | "Registrada" | "Cerrada"
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Forzado por el controller cuando el usuario logueado es CONTRATISTA:
    /// acota el listado a los trabajadores vinculados actualmente a su empresa.
    /// No debe ser seteable desde el query string del cliente.
    /// </summary>
    public int? EmpresaIdContratista { get; set; }
}

public class AmonestacionPagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}

public class AmonestacionListItemDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string ProyectoNombre { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string WorkerNombre { get; set; } = "";
    public string WorkerDni { get; set; } = "";
    public string EmpresaNombre { get; set; } = "";
    public string TipoSancionNombre { get; set; } = "";
    public string NivelGravedad { get; set; } = "";
    public string InfraccionTipoNombre { get; set; } = "";
    /// <summary>Motivo/falta real capturada en texto libre — no confundir con InfraccionTipoNombre (solo la categoría).</summary>
    public string Descripcion { get; set; } = "";
    public int PuntosInfraccion { get; set; }
    public bool AplicaPenalizacion { get; set; }
    public decimal MontoCalculado { get; set; }
    public string Estado { get; set; } = "Registrada";
}

// ── Editar (acceso restringido — corregir errores de captura) ──────────────

public class AmonestacionEditRequest
{
    public int? TipoSancionId { get; set; }
    public int? InfraccionTipoId { get; set; }
    public string? Descripcion { get; set; }
    public int? PuntosInfraccion { get; set; }
    public bool? AplicaPenalizacion { get; set; }
    public int? SancionInfraccionId { get; set; }
    public int? DiasSuspension { get; set; }
    public string? FechaInicioSuspension { get; set; }
    public string? FechaFinSuspension { get; set; }
    /// <summary>Por qué se corrige — queda en el log de auditoría (WorkerEvento), obligatorio.</summary>
    public string MotivoEdicion { get; set; } = "";
}

// ── Detalle ────────────────────────────────────────────────────────────────

public class AmonestacionDetalleDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public int ProyectoId { get; set; }
    public string ProyectoNombre { get; set; } = "";
    public DateTime Fecha { get; set; }
    public int WorkerId { get; set; }
    public string WorkerNombre { get; set; } = "";
    public string WorkerDni { get; set; } = "";
    public string? WorkerCargo { get; set; }
    public string? WorkerCategoria { get; set; }
    public int? WorkerEdad { get; set; }
    public int? EmpresaId { get; set; }
    public string EmpresaNombre { get; set; } = "";
    public bool EsEmpresaAbril { get; set; }
    public string? EmpresaLogoUrl { get; set; }
    public int? PartidaId { get; set; }
    public string? PartidaNombre { get; set; }
    public int TipoSancionId { get; set; }
    public string TipoSancionNombre { get; set; } = "";
    public string NivelGravedad { get; set; } = "";
    public bool GeneraSuspension { get; set; }
    public int InfraccionTipoId { get; set; }
    public string InfraccionTipoNombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public bool AplicaPenalizacion { get; set; }
    public int? SancionInfraccionId { get; set; }
    public string? SancionInfraccionNombre { get; set; }
    public decimal MontoCalculado { get; set; }
    public int PuntosInfraccion { get; set; }
    public int PuntosAcumulados { get; set; }
    public bool Inhabilitado { get; set; }
    public int? DiasSuspension { get; set; }
    public DateOnly? FechaInicioSuspension { get; set; }
    public DateOnly? FechaFinSuspension { get; set; }
    public int? PersonaReportaId { get; set; }
    public string? PersonaReportaNombre { get; set; }
    public string? PdfUrl { get; set; }
    public string Estado { get; set; } = "Registrada";
    public string? DocumentoFirmadoUrl { get; set; }
    public DateTime? FechaCierre { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<AmonFotoDto> Fotos { get; set; } = new();
}

// ── Cierre ─────────────────────────────────────────────────────────────────

public class AmonestacionCerrarRequest
{
    public string DocumentoFirmadoBase64 { get; set; } = "";
    public string NombreArchivo { get; set; } = "documento-firmado.jpg";
}

public class AmonFotoDto
{
    public int Id { get; set; }
    public string Url { get; set; } = "";
    public string? NombreArchivo { get; set; }
    public int Orden { get; set; }
    public string? Base64Data { get; set; }
}

// ── Dashboard ──────────────────────────────────────────────────────────────

public class AmonestacionDashboardDto
{
    public int TotalAmonestaciones { get; set; }
    public int TrabajadoresConMas5Puntos { get; set; }
    public int TrabajadoresInhabilitados { get; set; }
    public int AmonestacionesMesActual { get; set; }
    public int BorradorPendientes { get; set; }
    public int PendientesCierre { get; set; }
    public int AmonestacionesRegistradas { get; set; }
    public int AmonestacionesCerradas { get; set; }
    public List<AmonPorTipoDto> PorTipoSancion { get; set; } = new();
    public List<AmonMatrizProyectoDto> MatrizProyecto { get; set; } = new();
    public List<AmonTendenciaMesDto> TendenciaMeses { get; set; } = new();
    public List<AmonUltimoSancionadoDto> UltimosSancionados { get; set; } = new();
}

public class AmonPorTipoDto
{
    public string TipoNombre { get; set; } = "";
    public int Total { get; set; }
}

// Fila de la matriz: un proyecto con su total y desglose por tipo
public class AmonMatrizProyectoDto
{
    public string ProyectoNombre { get; set; } = "";
    public int Total { get; set; }
    public List<AmonCeldaTipoDto> PorTipo { get; set; } = new();
}

public class AmonCeldaTipoDto
{
    public string TipoNombre { get; set; } = "";
    public int Total { get; set; }
}

// Tendencia: mes fijo (1–12 del año actual) con total y desglose por proyecto
public class AmonTendenciaMesDto
{
    public int Mes { get; set; }
    public int Total { get; set; }
    public List<AmonCeldaTipoDto> PorTipo { get; set; } = new();
    public List<AmonCeldaTipoDto> PorProyecto { get; set; } = new();
}

// Últimos trabajadores sancionados
public class AmonUltimoSancionadoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string WorkerNombre { get; set; } = "";
    public string WorkerDni { get; set; } = "";
    public string EmpresaNombre { get; set; } = "";
    public string ProyectoNombre { get; set; } = "";
    public string TipoSancionNombre { get; set; } = "";
    public string NivelGravedad { get; set; } = "";
    public int PuntosInfraccion { get; set; }
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = "";
}

// Mantenemos alias para compatibilidad con puntaje worker historial
public class AmonTendenciaDto
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public int Total { get; set; }
}

// ── Puntaje del trabajador ─────────────────────────────────────────────────

public class WorkerPuntajeDto
{
    public int WorkerId { get; set; }
    public string Nombre { get; set; } = "";
    public string Dni { get; set; } = "";
    public int? EmpresaId { get; set; }
    public string EmpresaNombre { get; set; } = "";
    public int PuntosAcumulados { get; set; }
    public bool Inhabilitado { get; set; }
    public List<AmonestacionListItemDto> Historial { get; set; } = new();
}

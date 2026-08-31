namespace Abril_Backend.Features.SsomaModule.CharlasFeature.Application.Dtos;

// ── Existing Response DTOs ────────────────────────────────────────────────────
public record ProyectoDto(int ProyectoId, string Nombre);
public record StaffDto(int WorkerId, string NombreCompleto, string Cargo, string? Categoria);
public record CharlaResumenDto(int Id, DateTime Fecha, string Titulo, string Tema, decimal DuracionHoras, int TotalAsistentes, List<int> AsistentesIds);
public record AsistenciaDetailDto(int WorkerId, string NombreCompleto, bool Asistio);
public record ArchivoItemDto(int Id, string Url, string Nombre);
public record CapacitacionDto(int? Id, int WorkerId, string NombreCompleto, DateTime? Fecha, string? Tema, string? EvidenciaUrl, string? EvidenciaNombre, string Estado, List<ArchivoItemDto> Archivos);
public record ResumenDto(int TotalCharlas, int TotalAsistencias, int CapsTotal, int CapsFalta, int CapsEnviado, int CapsAprobado, int CapsRechazado);

// ── Existing Request DTOs ─────────────────────────────────────────────────────
public record CrearCharlaDto(DateTime Fecha, string Titulo, string Tema, decimal DuracionHoras, int ProyectoId);
public record GuardarAsistenciaDto(List<int> WorkerIds);
public record CambiarEstadoDto(string Estado);

// ── NEW: Tab 1 — Dashboard Asistencia Supervisores ────────────────────────────
public record DashSupervisoresRowDto(
    int CharlaId,
    string Titulo,
    DateTime Fecha,
    int? SupervisorId,
    string SupervisorNombre,
    int TotalAsistentes,
    int TotalAsistio
);

// ── NEW: Tab 2 — Comparativo Programadas vs Realizadas ───────────────────────
public record ComparativoMesDto(int Mes, string MesNombre, int Programadas, int Realizadas);

// ── NEW: Tab 3 — Crear nueva charla ──────────────────────────────────────────
public record NuevaCharlaCreateDto(
    int? ProgramaId,
    int ProyectoId,
    string Titulo,
    string? Tema,
    string? Descripcion,
    DateTime Fecha,
    decimal DuracionHoras,
    int? SupervisorId,
    List<int> WorkerIds
);

// ── NEW: Tab 3 — Editar charla (cabecera + asistencia) ───────────────────────
public record EditarCharlaDto(
    string Titulo,
    string? Tema,
    DateTime Fecha,
    List<int> WorkerIds
);

// ── NEW: Tab 3 — Galería charlas proyecto ────────────────────────────────────
public record CharlaGaleriaItemDto(
    int Id,
    string Titulo,
    string Tipo,
    DateTime Fecha,
    int TotalAsistentes,
    int TotalAsistio
);

// ── NEW: Dashboard Por Persona (matriz semanal) ───────────────────────────────
public record DashDiaSemanaDto(int NumDia, string Nombre, DateTime Fecha);
public record DashPersonaAsistDiaDto(int NumDia, bool? Asistio); // null = no hubo charla ese día
public record DashPersonalItemDto(
    int WorkerId,
    string Nombre,
    string Cargo,
    List<DashPersonaAsistDiaDto> Dias,
    int CharlasAsistidas,
    int CharlasTotales,
    int CapsSemana,
    int CapsAprobMes,
    int CapsAcumMes
);
public record DashPersonalResultDto(List<DashDiaSemanaDto> Dias, List<DashPersonalItemDto> Staff);

// ── NEW: Dashboard Por Proyecto ───────────────────────────────────────────────
public record DashProyectoItemDto(
    int ProyectoId,
    string Nombre,
    int TotalStaff,
    int CharlasDictadas,
    int TotalAsistencias,
    int TotalPosiblesAsistencias,
    int CapsEnviadasSemana,
    int CapsAprobMes
);

// ── NEW: Tab 4 — Lista paginada ───────────────────────────────────────────────
public record CharlaListItemDto(
    int Id,
    string Titulo,
    string? Tema,
    DateTime Fecha,
    int? SupervisorId,
    string SupervisorNombre,
    string Estado,
    string? EvidenciaNombre,
    int TotalAsistentes
);
public record CharlaListResultDto(List<CharlaListItemDto> Items, int Total);

// ── NEW: Tab 4 — Detalle modal ────────────────────────────────────────────────
public record CharlaDetalleDto(
    int Id,
    string Titulo,
    string? Tema,
    string? Descripcion,
    DateTime Fecha,
    decimal DuracionHoras,
    int? SupervisorId,
    string SupervisorNombre,
    string Estado,
    string? EvidenciaUrl,
    string? EvidenciaNombre,
    int TotalAsistentes,
    List<AsistenciaDetailDto> Asistencias,
    DateTime? EvidenciaSubidaEn
);

// ── NEW: Tab 4 — Acciones ─────────────────────────────────────────────────────
public record RechazarCharlaDto(string Motivo);

// ── NEW: Supervisor search ────────────────────────────────────────────────────
public record UsuarioDto(int Id, string NombreCompleto, string? Email);

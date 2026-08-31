using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.SsomaModule.AccidentesIncidentesFeature.Infrastructure.Models;

// ── Tablas de referencia ─────────────────────────────────────────────────────

public class SsomaFlashTipo
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;   // AC, IN, NC, AL
    public string Nombre { get; set; } = string.Empty;   // Accidente, Incidente, No Conformidad, Alerta
    public int Orden { get; set; }
}

public class SsomaFlashEtapaProyecto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class SsomaFlashParteAfectada
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class SsomaEmpresaAbril
{
    public int Id { get; set; }
    public string RazonSocial { get; set; } = string.Empty;
    public string? Ruc { get; set; }
    public bool Activa { get; set; } = true;
}

public class SsomaFlashPartida
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

// ── Entidad principal ────────────────────────────────────────────────────────

public class SsomaAccidenteIncidente
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;           // GAR-AC-01
    public int ProyectoId { get; set; }
    public int TipoId { get; set; }

    // Área organizacional de origen del evento (Producción/construcción, Post Venta, Arquitectura Comercial).
    // Independiente del proyecto: un mismo proyecto puede tener eventos de más de un área
    // (ej. post venta en un proyecto ya culminado, o arq. comercial en la sala de ventas antes de obra).
    public string AreaOrigen { get; set; } = "Produccion"; // Produccion | PostVenta | ArquitecturaComercial

    public DateTime Fecha { get; set; }
    public TimeSpan? Hora { get; set; }
    public string LugarExacto { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Estado { get; set; } = "Borrador";

    // Empresa involucrada (Abril o contratista)
    public int? EmpresaAbrilId { get; set; }
    public int? ContributorId { get; set; }

    public string? JefeInmediatoNombre { get; set; }

    // Etapa y partida
    public int? EtapaProyectoId { get; set; }
    public int? PartidaId { get; set; }

    // Trabajador afectado
    public int? WorkerId { get; set; }
    public string? TrabajadorNombre { get; set; }
    public string? PuestoTrabajo { get; set; }
    public int? Edad { get; set; }
    public int? AniosExperiencia { get; set; }
    public string? CelularTrabajador { get; set; }
    public int? ParteAfectadaId { get; set; }

    // Campos nuevos Entrega 1
    public string? Turno { get; set; }                    // Mañana | Tarde | Noche
    public string? TipoContacto { get; set; }             // Golpe por objeto | Caída mismo nivel | etc.
    public bool DanioProcesoFlag { get; set; } = false;  // checkbox ¿hubo daño?
    public string? AtencionMedica { get; set; }           // Ninguna | Tópico | Clínica | Hospital
    public string? CentroAtencion { get; set; }           // nombre del centro

    // Daños y consecuencias
    public string? DanoProceso { get; set; }
    public int? ConsecuenciaRealPersonal { get; set; }    // 1-6
    public int? ConsecuenciaPotencialPersonal { get; set; }

    // Descripción y acciones
    public string? AccionesInmediatas { get; set; }

    // Elaborado por
    public int? ElaboradoPorId { get; set; }
    public string? ElaboradoPorNombre { get; set; }
    public string? ElaboradoPorCargo { get; set; }
    public string? ElaboradoPorEmail { get; set; }
    public string? ElaboradoPorTelefono { get; set; }

    // Fotos
    public string? UrlFoto1 { get; set; }
    public string? UrlFoto2 { get; set; }

    // Envío
    public bool Enviado { get; set; } = false;
    public DateTime? FechaEnvio { get; set; }
    public string? UrlPdfSharepoint { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navegación
    public Project? Proyecto { get; set; }
    public SsomaFlashTipo? Tipo { get; set; }
    public SsomaEmpresaAbril? EmpresaAbril { get; set; }
    public SsomaFlashEtapaProyecto? EtapaProyecto { get; set; }
    public SsomaFlashPartida? Partida { get; set; }
    public SsomaFlashParteAfectada? ParteAfectada { get; set; }
    public ICollection<SsomaFlashDescanso> Descansos { get; set; } = [];
    public ICollection<SsomaAccidenteTrabajador> Trabajadores { get; set; } = [];
}

// ── Trabajadores afectados asociados al flash ────────────────────────────────

public class SsomaAccidenteTrabajador
{
    public int Id { get; set; }
    public int AccidenteIncidenteId { get; set; }
    public int? WorkerId { get; set; }
    public string TrabajadorNombre { get; set; } = string.Empty;
    public string? PuestoTrabajo { get; set; }
    public int? Edad { get; set; }
    public int? AniosExperiencia { get; set; }
    public string? CelularTrabajador { get; set; }
    public int? ParteAfectadaId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public SsomaAccidenteIncidente? AccidenteIncidente { get; set; }
}

// ── Descansos médicos asociados al flash ─────────────────────────────────────

public class SsomaFlashDescanso
{
    public int Id { get; set; }
    public int AccidenteIncidenteId { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string? Observacion { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SsomaAccidenteIncidente? AccidenteIncidente { get; set; }
}

// ── Entregables ──────────────────────────────────────────────────────────────

public class SsomaEntregableTipo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
    // TODOS | ACCIDENTE | INCIDENTE_ALTO_RIESGO
    public string AplicaA { get; set; } = "TODOS";
}

public class SsomaEntregable
{
    public int Id { get; set; }
    public int AccidenteIncidenteId { get; set; }
    public int TipoId { get; set; }
    public string Estado { get; set; } = "Pendiente"; // Pendiente, Presentado, Observado, Aprobado, No aplica
    public DateOnly? FechaLimite { get; set; }
    public string? UrlArchivo { get; set; }
    public string? NombreArchivo { get; set; }
    public string? Observacion { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public SsomaEntregableTipo? Tipo { get; set; }
    public ICollection<SsomaEntregableResponsable> Responsables { get; set; } = [];
    public ICollection<SsomaEntregableArchivo> Archivos { get; set; } = [];
}

public class SsomaEntregableResponsable
{
    public int Id { get; set; }
    public int EntregableId { get; set; }
    public int? WorkerId { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

// Un entregable (fila del catálogo, ej. "ATS") puede recibir varios archivos a lo
// largo del tiempo — ej. subir un ATS y luego otro ATS adicional — por eso es
// una colección y no un único UrlArchivo/NombreArchivo como antes.
public class SsomaEntregableArchivo
{
    public int Id { get; set; }
    public int EntregableId { get; set; }
    public string UrlArchivo { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ── Investigación RM-050 ──────────────────────────────────────────────────────

[Table("ss_investigacion_rm050")]
public class SsomaInvestigacionRm050
{
    [Column("id")]                       public int Id { get; set; }
    [Column("accidente_incidente_id")]   public int AccidenteIncidenteId { get; set; }
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
    public string? ArbolCausasUrl { get; set; }
    public string? ElaboradoPorNombre { get; set; }
    public string? ElaboradoPorCargo { get; set; }
    public DateOnly? ElaboradoPorFecha { get; set; }
    public string? AprobadoPorNombre { get; set; }
    public string? AprobadoPorCargo { get; set; }
    public string Estado { get; set; } = "Borrador";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<SsomaAccionCorrectiva> AccionesCorrectivas { get; set; } = [];
}

[Table("ss_accion_correctiva")]
public class SsomaAccionCorrectiva
{
    [Column("id")]                    public int Id { get; set; }
    [Column("investigacion_id")]      public int InvestigacionId { get; set; }
    [Column("descripcion")]           public string Descripcion { get; set; } = string.Empty;
    [Column("tipo")]                  public string? Tipo { get; set; }
    [Column("responsable_nombre")]    public string? ResponsableNombre { get; set; }
    [Column("responsable_worker_id")] public int? ResponsableWorkerId { get; set; }
    [Column("fecha_compromiso")]      public DateOnly? FechaCompromiso { get; set; }
    [Column("fecha_cumplimiento")]    public DateOnly? FechaCumplimiento { get; set; }
    [Column("estado")]                public string Estado { get; set; } = "Pendiente";
    [Column("evidencia_url")]         public string? EvidenciaUrl { get; set; }
    [Column("created_at")]            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ── Documentos adjuntos (compatibilidad hacia atrás) ─────────────────────────

public class SsomaAccidenteDocumento
{
    public int Id { get; set; }
    public int AccidenteId { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string TipoArchivo { get; set; } = string.Empty;
    public long TamanioBytes { get; set; }
    public string UrlSharepoint { get; set; } = string.Empty;
    public int? UsuarioId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SsomaAccidenteIncidente? Accidente { get; set; }
}

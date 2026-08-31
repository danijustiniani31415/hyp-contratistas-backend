namespace Abril_Backend.Features.Ssoma.Rac.Entities;

// ──────────────────────────────────────────────────────────
// CATÁLOGOS
// ──────────────────────────────────────────────────────────

/// <summary>ssoma_rac_categoria</summary>
public class SsomaRacCategoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Tipo { get; set; } = "";   // ACTO | CONDICION
    public string? Ambito { get; set; }
    public bool Activo { get; set; } = true;
    public int Orden { get; set; }
}

/// <summary>ssoma_rac_infraccion</summary>
public class SsomaRacInfraccion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public decimal? FactorUit { get; set; }
    public decimal? MontoFijo { get; set; }
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
}

/// <summary>ssoma_uit_anio</summary>
public class SsomaUitAnio
{
    public int Id { get; set; }
    public int Anio { get; set; }
    public decimal Valor { get; set; }
    public bool Activo { get; set; } = true;
}

// ──────────────────────────────────────────────────────────
// RAC PRINCIPAL
// ──────────────────────────────────────────────────────────

/// <summary>ssoma_rac</summary>
public class SsomaRac
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";

    // FK a project (project_id)
    public int ProyectoId { get; set; }

    public string Tipo { get; set; } = "";         // ACTO | CONDICION
    public int CategoriaId { get; set; }
    public string Severidad { get; set; } = "";    // BAJO | MEDIO | ALTO | CRITICO

    // Reportante
    public bool EsAnonimoReportante { get; set; }
    public int? ReportanteId { get; set; }         // FK app_user (user_id)
    public string? ReportanteNombre { get; set; }
    public string? ReportanteCargo { get; set; }
    public int? EmpresaReportanteId { get; set; }  // FK contributor (contributor_id)

    // Observado
    public bool EsAnonimoObservado { get; set; }
    public int? ObservadoWorkerId { get; set; }    // FK workers (id)
    public int? EmpresaReportadaId { get; set; }   // FK contributor (contributor_id)

    // Ubicación
    public string? ProyectoPiso { get; set; }
    public string? LugarDescripcion { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }

    // Descripción
    public string Descripcion { get; set; } = "";
    public string? PlanAccion { get; set; }
    public DateTime FechaReporte { get; set; }
    public DateTime? PlazoLevantamiento { get; set; }

    // Estado
    public string Estado { get; set; } = "Abierto";   // Abierto | Cerrado
    public DateTime? FechaCierre { get; set; }
    public string? CierreDescripcion { get; set; }
    public int? CerradoPorId { get; set; }             // FK app_user (user_id)

    // Penalidad
    public bool AplicaPenalidad { get; set; }

    // Documentos
    public string? FirmaReportanteUrl { get; set; }
    public string? FirmaReportanteSpId { get; set; }
    public string? PdfUrl { get; set; }
    public string? PdfSpId { get; set; }

    // Auditoría
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navegación interna
    public SsomaRacCategoria Categoria { get; set; } = null!;
    public List<SsomaRacFoto> Fotos { get; set; } = new();
    public SsomaRacPenalidad? Penalidad { get; set; }
}

// ──────────────────────────────────────────────────────────
// FOTOS
// ──────────────────────────────────────────────────────────

/// <summary>ssoma_rac_foto</summary>
public class SsomaRacFoto
{
    public int Id { get; set; }
    public int RacId { get; set; }
    public string Url { get; set; } = "";
    public string? SpId { get; set; }
    public string Tipo { get; set; } = "Hallazgo";    // Hallazgo | Cierre
    public string? NombreArchivo { get; set; }
    public int Orden { get; set; }
    public int? SubidoPor { get; set; }                // FK app_user (user_id)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navegación interna
    public SsomaRac Rac { get; set; } = null!;
}

// ──────────────────────────────────────────────────────────
// PENALIDAD
// ──────────────────────────────────────────────────────────

/// <summary>ssoma_rac_penalidad</summary>
public class SsomaRacPenalidad
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public int RacId { get; set; }
    public int? EmpresaId { get; set; }                // FK contributor (contributor_id)
    public int? ProyectoId { get; set; }               // FK project (project_id)
    public int? InfraccionId { get; set; }             // FK ssoma_rac_infraccion
    public decimal MontoCalculado { get; set; }
    public decimal UitReferencia { get; set; }
    public string? DescripcionOcurrido { get; set; }

    // Estado
    public string Estado { get; set; } = "EnEvaluacion"; // EnEvaluacion | DescargoPresentado | Aplicada | Anulada

    // Descargo
    public string? DescargoTexto { get; set; }
    public string? DocumentoUrl { get; set; }
    public DateTime? DescargoFecha { get; set; }
    public int? DescargoUsuarioId { get; set; }        // FK app_user (user_id)

    // Resolución
    public string? ResolucionTexto { get; set; }
    public string? ResolucionTipo { get; set; }        // Aplicada | Anulada
    public int? ResueltoPorId { get; set; }            // FK app_user (user_id)
    public DateTime? ResueltaEn { get; set; }
    public string? PdfResolucionUrl { get; set; }

    // Auditoría
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navegación interna
    public SsomaRac Rac { get; set; } = null!;
    public SsomaRacInfraccion? Infraccion { get; set; }
}

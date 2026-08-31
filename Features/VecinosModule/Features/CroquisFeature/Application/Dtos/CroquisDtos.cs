using Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Application.Dtos;

namespace Abril_Backend.Features.VecinosModule.Features.CroquisFeature.Application.Dtos
{
    /// <summary>Un proyecto con su croquis asignado (si tiene), para la pantalla de asignación.</summary>
    public class ProjectCroquisItemDto
    {
        public int ProjectId { get; set; }
        public string ProjectDescription { get; set; } = null!;

        public int? ProjectCroquisId { get; set; }
        public string? ImageUrl { get; set; }
        public string? OriginalFileName { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
    }

    /// <summary>Un lote dibujado sobre el croquis. Puntos en coordenadas relativas (0–1).</summary>
    public class CroquisLoteDto
    {
        public int? ProjectCroquisLoteId { get; set; }
        public string NumeroLote { get; set; } = null!;
        public List<List<double>> Puntos { get; set; } = new();
    }

    /// <summary>Cuerpo para guardar el conjunto completo de lotes de un croquis.</summary>
    public class SaveCroquisLotesDto
    {
        public List<CroquisLoteDto> Lotes { get; set; } = new();
    }

    // ── Vista de Gestión (croquis-céntrica) ──────────────────────────────────

    /// <summary>Respuesta de la vista de gestión: croquis + catálogos para el formulario de alta.</summary>
    public class CroquisGestionResponseDto
    {
        public List<CroquisGestionDto> Croquis { get; set; } = new();
        public List<ProjectOptionDto> Projects { get; set; } = new();
        public List<CatalogOptionDto> Colindancias { get; set; } = new();
        public List<CatalogOptionDto> TiposConstruccion { get; set; } = new();
        public List<CatalogOptionDto> Usos { get; set; } = new();
        public List<CatalogOptionDto> RelacionTipos { get; set; } = new();
    }

    /// <summary>Un croquis registrado con sus lotes y los vecinos del proyecto (para asignar).</summary>
    public class CroquisGestionDto
    {
        public int ProjectId { get; set; }
        public string ProjectDescription { get; set; } = null!;
        public int ProjectCroquisId { get; set; }
        public string ImageUrl { get; set; } = null!;
        /// <summary>Total de solicitudes de todos los vecinos del proyecto.</summary>
        public int SolicitudesCount { get; set; }
        /// <summary>Total de compromisos de todos los vecinos del proyecto.</summary>
        public int CompromisosCount { get; set; }
        /// <summary>Compromisos del proyecto en estado "Pendiente".</summary>
        public int CompromisosPendientes { get; set; }
        /// <summary>Compromisos del proyecto en estado "En proceso".</summary>
        public int CompromisosEnProceso { get; set; }
        /// <summary>Compromisos del proyecto en estado "Culminado".</summary>
        public int CompromisosCulminados { get; set; }
        /// <summary>Compromisos con fecha límite por municipalidad/fiscalización en estado "Pendiente".</summary>
        public int CompromisosLimitePendientes { get; set; }
        /// <summary>Compromisos con fecha límite por municipalidad/fiscalización en estado "En proceso".</summary>
        public int CompromisosLimiteEnProceso { get; set; }
        /// <summary>Compromisos con fecha límite por municipalidad/fiscalización en estado "Culminado".</summary>
        public int CompromisosLimiteCulminados { get; set; }
        /// <summary>Solicitudes aprobadas (Aceptada) de todos los vecinos del proyecto.</summary>
        public int SolicitudesAprobadas { get; set; }
        /// <summary>Solicitudes evaluables del proyecto (Aceptada + Por responder, sin Denegada).</summary>
        public int SolicitudesEvaluables { get; set; }
        /// <summary>Entregables aprobados de todos los vecinos del proyecto.</summary>
        public int EntregablesAprobados { get; set; }
        /// <summary>Entregables evaluables (Falta + Enviado + Aprobado, sin "No aplica").</summary>
        public int EntregablesEvaluables { get; set; }
        /// <summary>Requisitos subidos de todos los vecinos del proyecto.</summary>
        public int RequisitosSubidos { get; set; }
        /// <summary>Requisitos evaluables del proyecto (Subido + No subido, sin "No aplica").</summary>
        public int RequisitosEvaluables { get; set; }
        /// <summary>Cantidad de lotes/edificios (polígonos) del proyecto.</summary>
        public int LotesCount { get; set; }
        /// <summary>Cantidad de vecinos/departamentos del proyecto.</summary>
        public int VecinosCount { get; set; }
        public List<CroquisGestionLoteDto> Lotes { get; set; } = new();
    }

    /// <summary>Lote (polígono) dentro de la vista de Gestión, con sus vecinos/departamentos y KPIs.</summary>
    public class CroquisGestionLoteDto
    {
        public int ProjectCroquisLoteId { get; set; }
        public string NumeroLote { get; set; } = null!;
        public List<List<double>> Puntos { get; set; } = new();
        /// <summary>Lote/edificio que representa el polígono (null si aún no tiene lote registrado).</summary>
        public int? VecinoLoteId { get; set; }
        public string? Direccion { get; set; }
        public string? Observaciones { get; set; }
        // ── KPIs a nivel de lote (agregados de sus vecinos) ──
        public int VecinosCount { get; set; }
        public int SolicitudesCount { get; set; }
        public int CompromisosCount { get; set; }
        public int SolicitudesAprobadas { get; set; }
        public int SolicitudesEvaluables { get; set; }
        public int EntregablesAprobados { get; set; }
        public int EntregablesEvaluables { get; set; }
        public int RequisitosSubidos { get; set; }
        public int RequisitosEvaluables { get; set; }
        /// <summary>Vecinos/departamentos registrados en este lote.</summary>
        public List<VecinoListItemDto> Vecinos { get; set; } = new();
    }
}

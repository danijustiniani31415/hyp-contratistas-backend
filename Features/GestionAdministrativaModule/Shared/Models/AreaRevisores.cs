using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.GestionAdministrativa.Shared.Models
{
    /// <summary>
    /// Revisores de salidas por área: cada nodo area_scope (áreas de tipo "Área Estándar",
    /// configuradas solo en el primer nodo estándar de cada rama) puede tener n posibles
    /// revisores ordenados por prioridad (1 = mayor prioridad). Se usa como segundo paso
    /// al resolver el revisor de una solicitud de salida: si el solicitante no tiene
    /// revisores propios en workers_revisores, se buscan los revisores del área a la que
    /// pertenece (workers.area_scope_id, subiendo por el árbol si su nodo no tiene
    /// revisores); si tampoco hay, se hace fallback al área de GTH (area_scope.email).
    /// </summary>
    [Table("area_revisores")]
    public class AreaRevisores
    {
        [Column("area_revisores_id")]
        public int AreaRevisoresId { get; set; }

        /// <summary>Nodo del árbol de áreas (area_scope.area_scope_id) al que pertenecen los revisores.</summary>
        [Column("area_scope_id")]
        public int AreaScopeId { get; set; }

        /// <summary>
        /// Proyecto (project.project_id) cuando el área está marcada como "filtrar por proyecto"
        /// (ga_salidas_area_config.filtra_por_proyecto). NULL = revisor a nivel de área (comportamiento
        /// por defecto/histórico); con valor = revisor específico de ese proyecto dentro del área.
        /// </summary>
        [Column("project_id")]
        public int? ProjectId { get; set; }

        /// <summary>Trabajador (workers.id) que revisa/aprueba las salidas de los trabajadores del área.</summary>
        [Column("revisor_id")]
        public int RevisorId { get; set; }

        /// <summary>1 = primero en ser considerado; a mayor número, menor prioridad.</summary>
        [Column("orden_prioridad")]
        public int OrdenPrioridad { get; set; } = 1;

        /// <summary>Si false, el revisor no se considera (ej. ausencia temporal del revisor).</summary>
        [Column("active")]
        public bool Active { get; set; } = true;

        /// <summary>Soft delete: false = eliminado (se conserva para auditoría).</summary>
        [Column("state")]
        public bool State { get; set; } = true;

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}

using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Infrastructure.Models
{
    /// <summary>
    /// Árbol de áreas. Cada fila referencia un area_item (vocabulario plano) y
    /// opcionalmente un area_scope padre. El mismo area_item puede aparecer en
    /// múltiples nodos del árbol (ej: "Oficina Técnica" bajo varias áreas estándar).
    /// </summary>
    [Table("area_scope")]
    public class AreaScope
    {
        [Column("area_scope_id")]
        public int AreaScopeId { get; set; }

        [Column("area_item_id")]
        public int AreaItemId { get; set; }

        [Column("area_scope_parent_id")]
        public int? AreaScopeParentId { get; set; }

        [Column("display_order")]
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Correo del área (nullable). Hoy solo lo usa "Gestión del Talento Humano":
        /// es el correo al que se envían las solicitudes de salida cuando un trabajador
        /// no tiene ningún revisor válido en workers_revisores (fallback GTH).
        /// </summary>
        [Column("email")]
        public string? Email { get; set; }

        [Column("active")]
        public bool Active { get; set; }

        /// <summary>Soft-delete flag. false = registro eliminado (se mantiene para auditoría).</summary>
        [Column("state")]
        public bool State { get; set; } = true;

        public AreaItem? AreaItem { get; set; }
        public AreaScope? Parent { get; set; }
    }
}

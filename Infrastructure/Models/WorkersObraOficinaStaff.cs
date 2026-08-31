using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Infrastructure.Models
{
    /// <summary>
    /// Catálogo normalizado de la ubicación laboral del trabajador: Obra /
    /// Staff (Oficina Técnica) / Oficina Central. Reemplaza al texto plano
    /// <c>workers.obra_oficina</c> (eliminado) y al último nodo del árbol
    /// <c>area_scope</c> de tipo "Área Obra_Oficina" (también eliminado):
    /// ahora la diferenciación vive en <see cref="Worker.ObraOficinaStaffId"/>
    /// y no se deduce del área.
    ///
    /// Los IDs son fijos — ver <see cref="Shared.Constants.ObraOficinaStaffIds"/>.
    /// </summary>
    [Table("workers_obra_oficina_staff")]
    public class WorkersObraOficinaStaff
    {
        [Column("workers_obra_oficina_staff_id")]
        public int WorkersObraOficinaStaffId { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("display_order")]
        public int DisplayOrder { get; set; }

        /// <summary>Habilitar/inhabilitar en filtros y desplegables.</summary>
        [Column("active")]
        public bool Active { get; set; } = true;

        /// <summary>Soft-delete: false = eliminado (se conserva para histórico).</summary>
        [Column("state")]
        public bool State { get; set; } = true;
    }
}

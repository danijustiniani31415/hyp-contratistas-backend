using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.GestionAdministrativa.Shared.Models
{
    /// <summary>
    /// Override manual de visibilidad de gestión de salidas: define, por trabajador,
    /// qué nodo <c>area_scope</c> puede "ver" (es decir, ver las solicitudes de los
    /// trabajadores que pertenecen a ese nodo). Si <see cref="IncluyeDescendientes"/>
    /// es true, también ve los nodos hijos (todo el subárbol). Si un trabajador no tiene
    /// ninguna fila viva, su visibilidad se resuelve por el algoritmo de jerarquía
    /// (SalidaVisibilityResolver: GTH ve todo, Gerente ve su gerencia, Administración de
    /// Obra ve las áreas con personal de Obra/Staff).
    /// </summary>
    [Table("ga_salida_visibilidad_area")]
    public class GaSalidaVisibilidadArea
    {
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Trabajador (workers.id) al que se le concede la visibilidad.</summary>
        [Column("worker_id")]
        public int WorkerId { get; set; }

        /// <summary>Nodo del árbol de áreas que puede ver (area_scope.area_scope_id).</summary>
        [Column("area_scope_id")]
        public int AreaScopeId { get; set; }

        /// <summary>Si true, también ve todos los nodos descendientes de este nodo.</summary>
        [Column("incluye_descendientes")]
        public bool IncluyeDescendientes { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTimeOffset? UpdatedAt { get; set; }

        /// <summary>Soft delete: false = eliminado (se conserva para histórico).</summary>
        [Column("state")]
        public bool State { get; set; } = true;
    }
}

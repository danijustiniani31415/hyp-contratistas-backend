using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.GestionAdministrativa.Shared.Models
{
    /// <summary>
    /// Configuración por área (nodo area_scope) para el módulo de salidas. Hoy solo lleva el flag
    /// <see cref="FiltraPorProyecto"/>: si está activo, el área se "subdivide por proyecto" y sus
    /// revisores se asignan por proyecto (area_revisores.project_id) en vez de a nivel de área.
    /// Tabla desacoplada de la matriz base (no se tocan columnas de area_scope).
    /// </summary>
    [Table("ga_salidas_area_config")]
    public class GaSalidasAreaConfig
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("area_scope_id")]
        public int AreaScopeId { get; set; }

        /// <summary>Si true, el área se filtra por proyecto (se muestran subfilas por proyecto). Default false.</summary>
        [Column("filtra_por_proyecto")]
        public bool FiltraPorProyecto { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTimeOffset? UpdatedAt { get; set; }

        [Column("active")]
        public bool Active { get; set; } = true;

        /// <summary>Soft delete: false = eliminado (se conserva para auditoría).</summary>
        [Column("state")]
        public bool State { get; set; } = true;
    }
}

using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Shared.Models
{
    /// <summary>
    /// Catálogo de funcionalidades que listan proyectos filtrando por project.active.
    /// Los IDs son fijos y se espejan como constantes en
    /// <c>Shared/Constants/ProyectoFiltroFuncionalidades.cs</c> (mismo patrón que Roles).
    /// </summary>
    [Table("proyecto_filtro_funcionalidad")]
    public class ProyectoFiltroFuncionalidad
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("codigo")]
        public string Codigo { get; set; } = string.Empty;

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}

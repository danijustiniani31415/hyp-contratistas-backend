using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Shared.Models
{
    /// <summary>
    /// Tabla filtro única de proyectos por funcionalidad (patrón lesson_area):
    /// project es la tabla matriz/madre; una fila (project_id, funcionalidad_id) con
    /// active = false oculta el proyecto SOLO en esa funcionalidad. Fila ausente =
    /// proyecto visible. project.active sigue siendo el interruptor maestro global.
    /// </summary>
    [Table("proyecto_filtro")]
    public class ProyectoFiltro
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("project_id")]
        public int ProjectId { get; set; }

        [Column("funcionalidad_id")]
        public int FuncionalidadId { get; set; }

        [Column("active")]
        public bool Active { get; set; } = true;

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}

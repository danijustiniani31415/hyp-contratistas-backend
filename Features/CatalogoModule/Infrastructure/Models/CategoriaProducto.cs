using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.CatalogoModule.Infrastructure.Models
{
    /// <summary>Ver CONTEXT_LOGISTICA.md sección 6 — Catálogo Maestro (Fase 1, punto 2).</summary>
    [Table("lb_categoria_producto")]
    public class CategoriaProducto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        /// <summary>EPP, MATERIAL, HERRAMIENTA o EQUIPO.</summary>
        public string Tipo { get; set; } = null!;

        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}

using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.CatalogoModule.Infrastructure.Models
{
    /// <summary>
    /// stock_minimo/stock_maximo NO viven acá — se mueven a lb_stock (Almacén/Kardex, sección 7),
    /// porque el umbral es por almacén, no global. Ver CONTEXT_LOGISTICA.md sección 6.
    /// </summary>
    [Table("lb_producto")]
    public class Producto
    {
        public long Id { get; set; }
        public string? Codigo { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
        public int CategoriaId { get; set; }
        /// <summary>UND, PAR, KG, GAL, etc.</summary>
        public string UnidadMedida { get; set; } = null!;
        /// <summary>true para EPP con talla (guantes, botas, etc.).</summary>
        public bool RequiereTalla { get; set; }
        /// <summary>true para herramientas/equipos en préstamo (no se consumen, vuelven).</summary>
        public bool EsRetornable { get; set; }
        public bool Activo { get; set; } = true;
        public DateTimeOffset CreadoEn { get; set; }

        [ForeignKey(nameof(CategoriaId))]
        public CategoriaProducto? Categoria { get; set; }
    }
}

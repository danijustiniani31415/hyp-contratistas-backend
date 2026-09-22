using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.CatalogoModule.Infrastructure.Models
{
    /// <summary>
    /// Catálogo fijo de tallas por tipo (ROPA, CALZADO, GUANTES) — mismo patrón de "atributo de
    /// variante" que Odoo/SAP: el producto es un solo maestro (Producto.TipoTalla indica cuál
    /// catálogo de tallas usar) y la talla real se registra en lb_stock/lb_movimiento/lb_pedido_item,
    /// no como filas de producto separadas. Ver CONTEXT_LOGISTICA.md sección 6.
    /// </summary>
    [Table("lb_talla")]
    public class Talla
    {
        public int Id { get; set; }
        public string TipoTalla { get; set; } = null!;
        public string Valor { get; set; } = null!;
        public int Orden { get; set; }
    }
}

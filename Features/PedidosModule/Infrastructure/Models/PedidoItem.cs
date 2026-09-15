using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.CatalogoModule.Infrastructure.Models;

namespace Abril_Backend.Features.PedidosModule.Infrastructure.Models
{
    [Table("lb_pedido_item")]
    public class PedidoItem
    {
        public long Id { get; set; }
        public long PedidoId { get; set; }
        public long ProductoId { get; set; }
        public string Talla { get; set; } = "";
        public decimal CantidadSolicitada { get; set; }
        /// <summary>Null hasta que se entrega. Hoy siempre = CantidadSolicitada (se valida stock
        /// completo antes de entregar) — separado de CantidadSolicitada para permitir entrega
        /// parcial más adelante sin otra migración.</summary>
        public decimal? CantidadEntregada { get; set; }

        [ForeignKey(nameof(PedidoId))]
        public Pedido? Pedido { get; set; }
        [ForeignKey(nameof(ProductoId))]
        public Producto? Producto { get; set; }
    }
}

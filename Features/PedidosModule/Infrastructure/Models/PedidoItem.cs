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
        public string Color { get; set; } = "";
        /// <summary>Nota puntual de este ítem (ej. "cualquier marca", "urgente") — independiente
        /// de Pedido.Observacion, que es para todo el pedido.</summary>
        public string? Observacion { get; set; }
        public decimal CantidadSolicitada { get; set; }
        /// <summary>Null hasta que se entrega. Hoy siempre = CantidadSolicitada (se valida stock
        /// completo antes de entregar) — separado de CantidadSolicitada para permitir entrega
        /// parcial más adelante sin otra migración.</summary>
        public decimal? CantidadEntregada { get; set; }

        /// <summary>
        /// Trazabilidad cuando el ítem no sale directo de stock (Entregar) sino que hay que
        /// comprarlo y mandarlo desde Lima — ver ComprasService.GenerarDesdePedidos y
        /// GuiaRemisionService. Cada una se va acumulando en su propia etapa, independiente de
        /// CantidadEntregada (que es solo para el camino "ya había stock").
        /// </summary>
        public decimal CantidadEnCompra { get; set; }
        public decimal CantidadRecibidaAlmacen { get; set; }
        public decimal CantidadDespachada { get; set; }
        public decimal CantidadConfirmadaMina { get; set; }

        [ForeignKey(nameof(PedidoId))]
        public Pedido? Pedido { get; set; }
        [ForeignKey(nameof(ProductoId))]
        public Producto? Producto { get; set; }
    }
}

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
        /// <summary>Null hasta que se entrega. Igual a CantidadAprobada (se valida stock completo
        /// antes de entregar) — separado de CantidadAprobada por si más adelante se permite
        /// entrega parcial además de aprobación parcial.</summary>
        public decimal? CantidadEntregada { get; set; }
        public int? PartidaId { get; set; }

        /// <summary>Cada ítem se resuelve individualmente en Visar/Aprobar — el Residente o
        /// Gerencia puede reducir la cantidad o rechazar un ítem puntual sin tumbar el resto del
        /// pedido. Null hasta que el ítem pasa por su primera decisión (Visar); de ahí en adelante
        /// siempre igual a CantidadSolicitada (si se aprobó completo) o menor (si se redujo).
        /// Ignorado si Estado = RECHAZADO.</summary>
        public decimal? CantidadAprobada { get; set; }
        public string Estado { get; set; } = "PENDIENTE";
        public string? MotivoRechazo { get; set; }

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
        [ForeignKey(nameof(PartidaId))]
        public Partida? Partida { get; set; }
    }
}

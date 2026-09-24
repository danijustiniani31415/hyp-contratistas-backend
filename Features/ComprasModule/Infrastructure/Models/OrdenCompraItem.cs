using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.CatalogoModule.Infrastructure.Models;
using Abril_Backend.Features.PedidosModule.Infrastructure.Models;

namespace Abril_Backend.Features.ComprasModule.Infrastructure.Models
{
    [Table("lb_orden_compra_item")]
    public class OrdenCompraItem
    {
        public long Id { get; set; }
        public long OrdenCompraId { get; set; }
        public long ProductoId { get; set; }
        public string Talla { get; set; } = "";
        public string Color { get; set; } = "";
        public decimal CantidadSolicitada { get; set; }
        public decimal CostoUnitario { get; set; }
        /// <summary>Se acumula con cada recepción parcial — 0 = nada recibido todavía.</summary>
        public decimal CantidadRecibida { get; set; }

        /// <summary>
        /// Null = compra libre (reposición general de almacén, como hasta ahora). Con valor =
        /// esta fila nace de un ítem de Pedido puntual — una OC puede tener varias filas del
        /// mismo producto, cada una ligada a un pedido distinto, para juntar varios pedidos al
        /// mismo proveedor sin perder de cuál viene cada cantidad.
        /// </summary>
        public long? PedidoItemId { get; set; }

        [ForeignKey(nameof(OrdenCompraId))]
        public OrdenCompra? OrdenCompra { get; set; }
        [ForeignKey(nameof(ProductoId))]
        public Producto? Producto { get; set; }
        [ForeignKey(nameof(PedidoItemId))]
        public PedidoItem? PedidoItem { get; set; }

        public ICollection<OrdenCompraRecepcion> Recepciones { get; set; } = new List<OrdenCompraRecepcion>();
    }
}

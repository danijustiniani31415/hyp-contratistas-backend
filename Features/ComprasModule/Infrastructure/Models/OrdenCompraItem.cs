using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.CatalogoModule.Infrastructure.Models;

namespace Abril_Backend.Features.ComprasModule.Infrastructure.Models
{
    [Table("lb_orden_compra_item")]
    public class OrdenCompraItem
    {
        public long Id { get; set; }
        public long OrdenCompraId { get; set; }
        public long ProductoId { get; set; }
        public string Talla { get; set; } = "";
        public decimal CantidadSolicitada { get; set; }
        public decimal CostoUnitario { get; set; }
        /// <summary>Se acumula con cada recepción parcial — 0 = nada recibido todavía.</summary>
        public decimal CantidadRecibida { get; set; }

        [ForeignKey(nameof(OrdenCompraId))]
        public OrdenCompra? OrdenCompra { get; set; }
        [ForeignKey(nameof(ProductoId))]
        public Producto? Producto { get; set; }

        public ICollection<OrdenCompraRecepcion> Recepciones { get; set; } = new List<OrdenCompraRecepcion>();
    }
}

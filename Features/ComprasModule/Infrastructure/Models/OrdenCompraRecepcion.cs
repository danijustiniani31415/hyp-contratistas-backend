using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.PersonasModule.Infrastructure.Models;

namespace Abril_Backend.Features.ComprasModule.Infrastructure.Models
{
    /// <summary>Bitácora de cada recepción parcial — un proveedor rara vez entrega todo junto.</summary>
    [Table("lb_orden_compra_recepcion")]
    public class OrdenCompraRecepcion
    {
        public long Id { get; set; }
        public long OrdenCompraItemId { get; set; }
        public decimal Cantidad { get; set; }
        public long RecibidoPorUsuarioSistemaId { get; set; }
        public DateTimeOffset CreadoEn { get; set; }

        [ForeignKey(nameof(OrdenCompraItemId))]
        public OrdenCompraItem? OrdenCompraItem { get; set; }
        [ForeignKey(nameof(RecibidoPorUsuarioSistemaId))]
        public UsuarioSistema? RecibidoPor { get; set; }
    }
}

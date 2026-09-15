using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.CatalogoModule.Infrastructure.Models;

namespace Abril_Backend.Features.EppModule.Infrastructure.Models
{
    [Table("lb_entrega_epp_item")]
    public class EntregaEppItem
    {
        public long Id { get; set; }
        public long EntregaId { get; set; }
        public long ProductoId { get; set; }
        public string Talla { get; set; } = "";
        public decimal Cantidad { get; set; }

        [ForeignKey(nameof(EntregaId))]
        public EntregaEpp? Entrega { get; set; }
        [ForeignKey(nameof(ProductoId))]
        public Producto? Producto { get; set; }
    }
}

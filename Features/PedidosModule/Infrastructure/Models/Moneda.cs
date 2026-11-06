using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.PedidosModule.Infrastructure.Models
{
    [Table("lb_moneda")]
    public class Moneda
    {
        public short Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
    }
}

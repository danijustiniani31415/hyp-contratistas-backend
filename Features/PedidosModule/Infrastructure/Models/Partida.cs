using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.PersonasModule.Infrastructure.Models;

namespace Abril_Backend.Features.PedidosModule.Infrastructure.Models
{
    [Table("lb_partida")]
    public class Partida
    {
        public int Id { get; set; }
        public int ProyectoId { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public bool Activo { get; set; } = true;
        public short? MonedaId { get; set; }
        public decimal? MontoPresupuestado { get; set; }

        [ForeignKey(nameof(ProyectoId))]
        public Proyecto? Proyecto { get; set; }
        [ForeignKey(nameof(MonedaId))]
        public Moneda? Moneda { get; set; }
    }
}

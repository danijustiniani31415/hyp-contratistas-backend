using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.PersonasModule.Infrastructure.Models;

namespace Abril_Backend.Features.PedidosModule.Infrastructure.Models
{
    [Table("lb_contrato")]
    public class Contrato
    {
        public int Id { get; set; }
        public int ProyectoId { get; set; }
        public string Codigo { get; set; } = null!;
        public string? Descripcion { get; set; }
        public DateOnly VigenteDesde { get; set; }
        public DateOnly? VigenteHasta { get; set; }
        public bool Activo { get; set; } = true;

        [ForeignKey(nameof(ProyectoId))]
        public Proyecto? Proyecto { get; set; }
    }
}

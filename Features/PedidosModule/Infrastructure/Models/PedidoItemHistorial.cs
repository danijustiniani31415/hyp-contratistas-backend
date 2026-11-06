using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.PersonasModule.Infrastructure.Models;

namespace Abril_Backend.Features.PedidosModule.Infrastructure.Models
{
    /// <summary>Bitácora inmutable de decisiones por ítem — nunca se edita ni se borra una fila.
    /// Una fila por cada vez que un ítem pasa por Visar o Aprobar, sin importar el resultado.</summary>
    [Table("lb_pedido_item_historial")]
    public class PedidoItemHistorial
    {
        public long Id { get; set; }
        public long PedidoItemId { get; set; }
        /// <summary>VISADO o APROBACION — la etapa en la que se tomó esta decisión.</summary>
        public string Etapa { get; set; } = null!;
        public long UsuarioSistemaId { get; set; }
        /// <summary>VISADO, APROBADO o RECHAZADO — a qué quedó el ítem tras esta decisión.</summary>
        public string EstadoResultante { get; set; } = null!;
        public decimal CantidadSolicitada { get; set; }
        public decimal? CantidadAprobada { get; set; }
        public string? MotivoRechazo { get; set; }
        public DateTimeOffset CreadoEn { get; set; }

        [ForeignKey(nameof(PedidoItemId))]
        public PedidoItem? PedidoItem { get; set; }
        [ForeignKey(nameof(UsuarioSistemaId))]
        public UsuarioSistema? UsuarioSistema { get; set; }
    }
}

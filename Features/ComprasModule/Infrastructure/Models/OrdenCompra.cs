using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.PersonasModule.Infrastructure.Models;

namespace Abril_Backend.Features.ComprasModule.Infrastructure.Models
{
    /// <summary>
    /// Fase 3, punto 7. A diferencia de Pedidos, sin paso de aprobación de Gerencia — crear
    /// órdenes de compra es la función propia del rol COMPRAS. Se recibe en partes: el estado
    /// (PENDIENTE/RECIBIDA_PARCIAL/RECIBIDA/CANCELADA) se calcula de los ítems, no se guarda.
    /// </summary>
    [Table("lb_orden_compra")]
    public class OrdenCompra
    {
        public long Id { get; set; }
        public string Codigo { get; set; } = null!;
        public int ProveedorId { get; set; }
        public int AlmacenId { get; set; }
        public long SolicitadoPorUsuarioSistemaId { get; set; }
        public string Estado { get; set; } = "PENDIENTE";
        public string? Observacion { get; set; }
        public DateTimeOffset CreadoEn { get; set; }

        [ForeignKey(nameof(ProveedorId))]
        public Proveedor? Proveedor { get; set; }
        [ForeignKey(nameof(AlmacenId))]
        public Almacen? Almacen { get; set; }
        [ForeignKey(nameof(SolicitadoPorUsuarioSistemaId))]
        public UsuarioSistema? SolicitadoPor { get; set; }

        public ICollection<OrdenCompraItem> Items { get; set; } = new List<OrdenCompraItem>();
    }
}

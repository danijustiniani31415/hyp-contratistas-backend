using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.ComprasModule.Infrastructure.Models
{
    /// <summary>Pedido de precios a varios proveedores por un mismo conjunto de ítems pendientes de compra.</summary>
    [Table("lb_cotizacion_solicitud")]
    public class CotizacionSolicitud
    {
        public long Id { get; set; }
        public string Codigo { get; set; } = null!;
        public int AlmacenId { get; set; }
        /// <summary>ABIERTA, ADJUDICADA o CANCELADA.</summary>
        public string Estado { get; set; } = "ABIERTA";
        public string? Observacion { get; set; }
        public string? MotivoAdjudicacion { get; set; }
        public string? OrdenesGeneradas { get; set; }
        public long CreadoPor { get; set; }
        public DateTimeOffset CreadoEn { get; set; }
        public long? AdjudicadaPor { get; set; }
        public DateTimeOffset? AdjudicadaEn { get; set; }
    }

    [Table("lb_cotizacion_item")]
    public class CotizacionItem
    {
        public long Id { get; set; }
        public long SolicitudId { get; set; }
        public long ProductoId { get; set; }
        public string Talla { get; set; } = "";
        public string Color { get; set; } = "";
        public decimal Cantidad { get; set; }
        public long? PedidoItemId { get; set; }
        public long? CotizacionAdjudicadaId { get; set; }
    }

    /// <summary>La respuesta de un proveedor: sus precios por ítem, plazo y condición de pago.</summary>
    [Table("lb_cotizacion")]
    public class Cotizacion
    {
        public long Id { get; set; }
        public long SolicitudId { get; set; }
        public int ProveedorId { get; set; }
        public int? PlazoDias { get; set; }
        public string? CondicionPago { get; set; }
        public string? Observacion { get; set; }
        public long CreadoPor { get; set; }
        public DateTimeOffset CreadoEn { get; set; }
    }

    [Table("lb_cotizacion_precio")]
    public class CotizacionPrecio
    {
        public long Id { get; set; }
        public long CotizacionId { get; set; }
        public long ItemId { get; set; }
        public decimal PrecioUnitario { get; set; }
    }
}

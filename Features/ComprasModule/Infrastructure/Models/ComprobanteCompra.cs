using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.PersonasModule.Infrastructure.Models;

namespace Abril_Backend.Features.ComprasModule.Infrastructure.Models
{
    /// <summary>
    /// Comprobante recibido de un proveedor (factura, boleta, nota de crédito…) leído con IA y
    /// confirmado por una persona. Es el documento contable; la orden de compra es el pedido.
    /// Un mismo (proveedor, tipo, serie, número) no se puede cargar dos veces.
    /// </summary>
    [Table("lb_comprobante_compra")]
    public class ComprobanteCompra
    {
        public long Id { get; set; }
        public int ProveedorId { get; set; }
        public int? AlmacenId { get; set; }
        /// <summary>FACTURA, BOLETA, NOTA_CREDITO, NOTA_DEBITO, RECIBO_HONORARIOS, TICKET, INVOICE_EXTRANJERO, OTRO.</summary>
        public string Tipo { get; set; } = null!;
        public string Serie { get; set; } = null!;
        public string Numero { get; set; } = null!;
        public DateOnly FechaEmision { get; set; }
        /// <summary>PEN o USD.</summary>
        public string Moneda { get; set; } = "PEN";
        public decimal? TipoCambio { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Igv { get; set; }
        public decimal Total { get; set; }
        public string? FormaPago { get; set; }
        public decimal? DetraccionMonto { get; set; }
        public string? OrdenCompraRef { get; set; }
        public string? GuiaRef { get; set; }
        public string? ArchivoUrl { get; set; }
        public string? Observacion { get; set; }
        public string? ModeloIa { get; set; }
        /// <summary>PENDIENTE_STOCK mientras falta aplicar ingresos al kardex; REGISTRADO cuando todo quedó aplicado.</summary>
        public string Estado { get; set; } = "REGISTRADO";
        public long CreadoPorUsuarioSistemaId { get; set; }
        public DateTimeOffset CreadoEn { get; set; }

        [ForeignKey(nameof(ProveedorId))]
        public Proveedor? Proveedor { get; set; }
        [ForeignKey(nameof(AlmacenId))]
        public Almacen? Almacen { get; set; }

        public ICollection<ComprobanteCompraItem> Items { get; set; } = new List<ComprobanteCompraItem>();
    }

    /// <summary>Copia del PDF/foto original. Vive en la base porque el servidor no tiene almacenamiento de archivos.
    /// ComprobanteCompraId es null mientras el comprobante está leído pero sin confirmar.</summary>
    [Table("lb_comprobante_compra_archivo")]
    public class ComprobanteCompraArchivo
    {
        public long Id { get; set; }
        public long? ComprobanteCompraId { get; set; }
        public string Nombre { get; set; } = null!;
        public string Mime { get; set; } = null!;
        public byte[] Contenido { get; set; } = null!;
        public DateTimeOffset CreadoEn { get; set; }
    }

    [Table("lb_comprobante_compra_item")]
    public class ComprobanteCompraItem
    {
        public long Id { get; set; }
        public long ComprobanteCompraId { get; set; }
        public int Linea { get; set; }
        public string Descripcion { get; set; } = null!;
        public decimal Cantidad { get; set; }
        public string? Unidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Total { get; set; }
        /// <summary>STOCK (ingresa al kardex), GASTO (servicio u otro gasto), ACTIVO (herramienta/equipo), RENDICION (pasaje/viático).</summary>
        public string Destino { get; set; } = "GASTO";
        public long? ProductoId { get; set; }
        public string Talla { get; set; } = "";
        public string Color { get; set; } = "";
        public string? CategoriaGasto { get; set; }
        public bool MovimientoRegistrado { get; set; }

        [ForeignKey(nameof(ComprobanteCompraId))]
        public ComprobanteCompra? Comprobante { get; set; }
    }
}

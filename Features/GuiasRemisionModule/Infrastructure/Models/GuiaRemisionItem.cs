using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.CatalogoModule.Infrastructure.Models;
using Abril_Backend.Features.PedidosModule.Infrastructure.Models;
using Abril_Backend.Features.PersonasModule.Infrastructure.Models;

namespace Abril_Backend.Features.GuiasRemisionModule.Infrastructure.Models
{
    [Table("lb_guia_remision_item")]
    public class GuiaRemisionItem
    {
        public long Id { get; set; }
        public long GuiaRemisionId { get; set; }
        /// <summary>Null = línea libre (sin inventario): solo describe el bien en la guía, no toca el kardex.</summary>
        public long? ProductoId { get; set; }
        /// <summary>Descripción de la línea libre (ej. "ROPA DE TRABAJO LAVADA - 12 piezas").</summary>
        public string? Descripcion { get; set; }
        /// <summary>Bien que no es de la empresa (ej. de un trabajador): se rotula como tal en la guía.</summary>
        public bool EsBienTercero { get; set; }
        public string Talla { get; set; } = "";
        public string Color { get; set; } = "";
        public decimal Cantidad { get; set; }
        /// <summary>Catálogo 03 SUNAT (unidad de medida) — NIU = unidad por defecto.</summary>
        public string UnidadMedida { get; set; } = "NIU";

        /// <summary>Null = despacho suelto (como hasta ahora). Con valor: este ítem despacha un
        /// ítem de Pedido puntual — varios pedidos pueden ir en una sola guía/viaje.</summary>
        public long? PedidoItemId { get; set; }

        /// <summary>
        /// Al crear la guía con AlmacenDestino ya se acredita el stock de destino de una (ver
        /// GuiaRemisionService.Crear) — estos campos son la confirmación real de que llegó, no un
        /// segundo movimiento de stock. Si CantidadConfirmada &lt; Cantidad, ConfirmarRecepcion
        /// registra el ajuste (SALIDA) que corrige el sobre-crédito inicial.
        /// </summary>
        public decimal? CantidadConfirmada { get; set; }
        public DateTimeOffset? ConfirmadoEn { get; set; }
        public long? ConfirmadoPorUsuarioSistemaId { get; set; }

        [ForeignKey(nameof(GuiaRemisionId))]
        public GuiaRemision? GuiaRemision { get; set; }
        [ForeignKey(nameof(ProductoId))]
        public Producto? Producto { get; set; }
        [ForeignKey(nameof(PedidoItemId))]
        public PedidoItem? PedidoItem { get; set; }
        [ForeignKey(nameof(ConfirmadoPorUsuarioSistemaId))]
        public UsuarioSistema? ConfirmadoPor { get; set; }
    }
}

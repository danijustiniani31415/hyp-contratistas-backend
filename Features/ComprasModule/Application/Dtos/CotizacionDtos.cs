namespace Abril_Backend.Features.ComprasModule.Application.Dtos
{
    public class CotizacionReposicionDto
    {
        public long ProductoId { get; set; }
        public string Talla { get; set; } = "";
        public string Color { get; set; } = "";
        public decimal Cantidad { get; set; }
    }

    public class CotizacionCrearDto
    {
        public int AlmacenId { get; set; }
        public string? Observacion { get; set; }
        /// <summary>Ítems de pedidos aprobados (la cantidad es lo que falta comprar de cada uno).</summary>
        public List<long> PedidoItemIds { get; set; } = new();
        /// <summary>Reposición sin pedido (stock bajo mínimo o compra libre).</summary>
        public List<CotizacionReposicionDto> Reposicion { get; set; } = new();
    }

    public class CotizacionPrecioDto
    {
        public long ItemId { get; set; }
        /// <summary>Vacío o 0 = este proveedor no cotizó este ítem.</summary>
        public decimal? Precio { get; set; }
    }

    public class CotizacionAgregarDto
    {
        public int ProveedorId { get; set; }
        public int? PlazoDias { get; set; }
        public string? CondicionPago { get; set; }
        public string? Observacion { get; set; }
        public List<CotizacionPrecioDto> Precios { get; set; } = new();
    }

    public class CotizacionSeleccionDto
    {
        public long ItemId { get; set; }
        public long CotizacionId { get; set; }
    }

    public class CotizacionAdjudicarDto
    {
        /// <summary>Obligatorio si se eligió algo que no es lo más barato, o si hay una sola cotización.</summary>
        public string? Motivo { get; set; }
        public List<CotizacionSeleccionDto> Selecciones { get; set; } = new();
    }

    public class CotizacionListItemDto
    {
        public long Id { get; set; }
        public string Codigo { get; set; } = "";
        public string AlmacenNombre { get; set; } = "";
        public string Estado { get; set; } = "";
        public int Items { get; set; }
        public int Cotizaciones { get; set; }
        public string? Resumen { get; set; }
        public DateTimeOffset CreadoEn { get; set; }
    }

    public class CotizacionItemDto
    {
        public long Id { get; set; }
        public string ProductoNombre { get; set; } = "";
        public string Talla { get; set; } = "";
        public string Color { get; set; } = "";
        public decimal Cantidad { get; set; }
        public string? PedidoCodigo { get; set; }
        public long? CotizacionAdjudicadaId { get; set; }
    }

    public class CotizacionRespuestaDto
    {
        public long Id { get; set; }
        public int ProveedorId { get; set; }
        public string ProveedorNombre { get; set; } = "";
        public int? PlazoDias { get; set; }
        public string? CondicionPago { get; set; }
        public string? Observacion { get; set; }
        /// <summary>Precio por id de ítem; falta la clave si no cotizó ese ítem.</summary>
        public Dictionary<long, decimal> Precios { get; set; } = new();
        /// <summary>Suma de precio × cantidad de lo que cotizó.</summary>
        public decimal Total { get; set; }
        /// <summary>true = cotizó todos los ítems.</summary>
        public bool Completa { get; set; }
    }

    public class CotizacionDetalleDto
    {
        public long Id { get; set; }
        public string Codigo { get; set; } = "";
        public int AlmacenId { get; set; }
        public string AlmacenNombre { get; set; } = "";
        public string Estado { get; set; } = "";
        public string? Observacion { get; set; }
        public string? MotivoAdjudicacion { get; set; }
        public string? OrdenesGeneradas { get; set; }
        public string CreadoPorNombre { get; set; } = "";
        public DateTimeOffset CreadoEn { get; set; }
        public List<CotizacionItemDto> Items { get; set; } = new();
        public List<CotizacionRespuestaDto> Cotizaciones { get; set; } = new();
    }
}

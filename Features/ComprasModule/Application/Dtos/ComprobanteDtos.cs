namespace Abril_Backend.Features.ComprasModule.Application.Dtos
{
    /// <summary>Línea tal como la lee la IA, sin interpretar: la persona la confirma o corrige.</summary>
    public class ComprobanteLeidoItemDto
    {
        public string Descripcion { get; set; } = "";
        public decimal Cantidad { get; set; }
        public string? Unidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Total { get; set; }
        /// <summary>BIEN, SERVICIO, PASAJE_VIATICO, ACTIVO u OTRO — solo una sugerencia de destino.</summary>
        public string Categoria { get; set; } = "OTRO";
    }

    public class ComprobanteLeidoDto
    {
        public string Tipo { get; set; } = "OTRO";
        public string? EmisorRuc { get; set; }
        public string? EmisorRazonSocial { get; set; }
        public string? ClienteRuc { get; set; }
        public string? ClienteRazonSocial { get; set; }
        public string? Serie { get; set; }
        public string? Numero { get; set; }
        /// <summary>yyyy-MM-dd.</summary>
        public string? FechaEmision { get; set; }
        public string? FechaVencimiento { get; set; }
        public string Moneda { get; set; } = "PEN";
        public string? FormaPago { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Igv { get; set; }
        public decimal Total { get; set; }
        public decimal? DetraccionMonto { get; set; }
        public decimal? DetraccionPorcentaje { get; set; }
        public string? OrdenCompraRef { get; set; }
        public string? GuiaRef { get; set; }
        public string? Observacion { get; set; }
        /// <summary>0 a 1: qué tan seguro está el modelo de haber leído bien todo el documento.</summary>
        public double Confianza { get; set; }
        public List<ComprobanteLeidoItemDto> Items { get; set; } = new();
    }

    /// <summary>Un aviso para la persona que confirma. Nivel: INFO, AVISO o ERROR.</summary>
    public class ComprobanteAdvertenciaDto
    {
        public string Nivel { get; set; } = "AVISO";
        public string Mensaje { get; set; } = "";
    }

    public class ComprobanteLecturaResultDto
    {
        public ComprobanteLeidoDto Lectura { get; set; } = null!;
        public List<ComprobanteAdvertenciaDto> Advertencias { get; set; } = new();
        public string ModeloUsado { get; set; } = "";
        public string? ArchivoUrl { get; set; }
        /// <summary>Proveedor ya registrado con el RUC del emisor, si existe.</summary>
        public int? ProveedorId { get; set; }
        /// <summary>Id del comprobante idéntico ya cargado, si es un duplicado.</summary>
        public long? DuplicadoDeId { get; set; }
    }

    public class ComprobanteGuardarItemDto
    {
        public string Descripcion { get; set; } = "";
        public decimal Cantidad { get; set; }
        public string? Unidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Total { get; set; }
        /// <summary>STOCK, GASTO, ACTIVO o RENDICION.</summary>
        public string Destino { get; set; } = "GASTO";
        public long? ProductoId { get; set; }
        public string Talla { get; set; } = "";
        public string Color { get; set; } = "";
        public string? CategoriaGasto { get; set; }
    }

    public class ComprobanteGuardarDto
    {
        /// <summary>Si es null se busca/crea el proveedor con ProveedorRuc y ProveedorRazonSocial.</summary>
        public int? ProveedorId { get; set; }
        public string? ProveedorRuc { get; set; }
        public string? ProveedorRazonSocial { get; set; }
        /// <summary>Obligatorio si alguna línea va a STOCK.</summary>
        public int? AlmacenId { get; set; }
        public string Tipo { get; set; } = "FACTURA";
        public string Serie { get; set; } = "";
        public string Numero { get; set; } = "";
        public DateOnly FechaEmision { get; set; }
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
        public List<ComprobanteGuardarItemDto> Items { get; set; } = new();
    }

    public class ComprobanteGuardadoDto
    {
        public long Id { get; set; }
        public string Estado { get; set; } = "";
        public int LineasAStock { get; set; }
        public int LineasGasto { get; set; }
        public List<string> Avisos { get; set; } = new();
    }
}

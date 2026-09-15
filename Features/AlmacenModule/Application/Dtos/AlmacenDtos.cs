namespace Abril_Backend.Features.AlmacenModule.Application.Dtos
{
    public class StockListItemDto
    {
        public long Id { get; set; }
        public int AlmacenId { get; set; }
        public string AlmacenNombre { get; set; } = null!;
        public long ProductoId { get; set; }
        public string ProductoNombre { get; set; } = null!;
        public string? ProductoCodigo { get; set; }
        public string UnidadMedida { get; set; } = null!;
        public string Talla { get; set; } = "";
        public decimal CantidadActual { get; set; }
        public decimal StockMinimo { get; set; }
        public decimal? StockMaximo { get; set; }
        public bool BajoMinimo { get; set; }
    }

    public class StockListResponseDto
    {
        public List<StockListItemDto> Data { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>Registra un ingreso o salida — el signo lo decide TipoMovimiento, no la cantidad (siempre positiva).</summary>
    public class RegistrarMovimientoDto
    {
        public int AlmacenId { get; set; }
        public long ProductoId { get; set; }
        public string Talla { get; set; } = "";
        /// <summary>INGRESO o SALIDA.</summary>
        public string TipoMovimiento { get; set; } = null!;
        public decimal Cantidad { get; set; }
        public decimal? CostoUnitario { get; set; }
        /// <summary>PEDIDO, COMPRA, ENTREGA_EPP, PRESTAMO_HERRAMIENTA — null para ajustes manuales.</summary>
        public string? ReferenciaTipo { get; set; }
        public long? ReferenciaId { get; set; }
    }

    public class AjustarUmbralesDto
    {
        public int AlmacenId { get; set; }
        public long ProductoId { get; set; }
        public string Talla { get; set; } = "";
        public decimal StockMinimo { get; set; }
        public decimal? StockMaximo { get; set; }
    }

    public class MovimientoListItemDto
    {
        public long Id { get; set; }
        public string AlmacenNombre { get; set; } = null!;
        public string ProductoNombre { get; set; } = null!;
        public string Talla { get; set; } = "";
        public string TipoMovimiento { get; set; } = null!;
        public decimal Cantidad { get; set; }
        public decimal? CostoUnitario { get; set; }
        public string? UsuarioNombre { get; set; }
        public DateTimeOffset CreadoEn { get; set; }
    }

    public class MovimientoListResponseDto
    {
        public List<MovimientoListItemDto> Data { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
    }
}

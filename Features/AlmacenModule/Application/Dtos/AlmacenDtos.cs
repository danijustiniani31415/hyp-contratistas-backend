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
        public string Color { get; set; } = "";
        public decimal CantidadActual { get; set; }
        public decimal CostoPromedio { get; set; }
        public decimal ValorTotal => CantidadActual * CostoPromedio;
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
        public string Color { get; set; } = "";
        /// <summary>INGRESO o SALIDA.</summary>
        public string TipoMovimiento { get; set; } = null!;
        public decimal Cantidad { get; set; }
        public decimal? CostoUnitario { get; set; }
        /// <summary>PEDIDO, COMPRA, ENTREGA_EPP, PRESTAMO_HERRAMIENTA — null para ajustes manuales.</summary>
        public string? ReferenciaTipo { get; set; }
        public long? ReferenciaId { get; set; }
    }

    /// <summary>Producto bajo su stock mínimo — sugerencia de reposición para el panel de
    /// Logística [DECIDIDO 2026-09-24]. Sin ítem de pedido detrás: nace del umbral, no de una
    /// solicitud puntual, así que la cantidad sugerida se puede editar libremente al generar la OC.</summary>
    public class ReposicionSugeridaDto
    {
        public int AlmacenId { get; set; }
        public string AlmacenNombre { get; set; } = null!;
        public long ProductoId { get; set; }
        public string ProductoNombre { get; set; } = null!;
        public string Talla { get; set; } = "";
        public string Color { get; set; } = "";
        public decimal CantidadActual { get; set; }
        public decimal StockMinimo { get; set; }
        public decimal? StockMaximo { get; set; }
        /// <summary>Sube hasta StockMaximo si está definido; si no, repone hasta el mínimo.</summary>
        public decimal CantidadSugerida { get; set; }
    }

    public class AjustarUmbralesDto
    {
        public int AlmacenId { get; set; }
        public long ProductoId { get; set; }
        public string Talla { get; set; } = "";
        public string Color { get; set; } = "";
        public decimal StockMinimo { get; set; }
        public decimal? StockMaximo { get; set; }
    }

    public class MovimientoListItemDto
    {
        public long Id { get; set; }
        public string AlmacenNombre { get; set; } = null!;
        public string ProductoNombre { get; set; } = null!;
        public string Talla { get; set; } = "";
        public string Color { get; set; } = "";
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

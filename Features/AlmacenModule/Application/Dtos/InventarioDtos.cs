namespace Abril_Backend.Features.AlmacenModule.Application.Dtos
{
    /// <summary>Una línea del kardex valorizado (método de promedio ponderado): entrada, salida y saldo después del movimiento.</summary>
    public class KardexLineaDto
    {
        public DateTimeOffset Fecha { get; set; }
        public string Tipo { get; set; } = "";
        public string? Referencia { get; set; }
        public decimal EntradaCantidad { get; set; }
        public decimal EntradaCostoUnitario { get; set; }
        public decimal EntradaTotal { get; set; }
        public decimal SalidaCantidad { get; set; }
        public decimal SalidaCostoUnitario { get; set; }
        public decimal SalidaTotal { get; set; }
        public decimal SaldoCantidad { get; set; }
        public decimal SaldoCostoUnitario { get; set; }
        public decimal SaldoTotal { get; set; }
    }

    public class KardexValorizadoDto
    {
        public string AlmacenNombre { get; set; } = "";
        public string ProductoNombre { get; set; } = "";
        public string UnidadMedida { get; set; } = "";
        public string Talla { get; set; } = "";
        public string Color { get; set; } = "";
        public decimal SaldoInicialCantidad { get; set; }
        public decimal SaldoInicialCostoUnitario { get; set; }
        public decimal SaldoInicialTotal { get; set; }
        public decimal TotalEntradas { get; set; }
        public decimal TotalSalidas { get; set; }
        public decimal SaldoFinalCantidad { get; set; }
        public decimal SaldoFinalTotal { get; set; }
        public List<KardexLineaDto> Lineas { get; set; } = new();
    }

    public class InventarioListDto
    {
        public long Id { get; set; }
        public int AlmacenId { get; set; }
        public string AlmacenNombre { get; set; } = "";
        public DateOnly Fecha { get; set; }
        public string Estado { get; set; } = "";
        public string? Observacion { get; set; }
        public int TotalItems { get; set; }
        public int Contados { get; set; }
        public int CantidadAjustes { get; set; }
        public DateTimeOffset CreadoEn { get; set; }
        /// <summary>Valor de lo que faltó y de lo que sobró (a costo promedio).</summary>
        public decimal ValorFaltante { get; set; }
        public decimal ValorSobrante { get; set; }
    }

    public class InventarioItemDto
    {
        public long Id { get; set; }
        public long ProductoId { get; set; }
        public string ProductoNombre { get; set; } = "";
        public string? ProductoCodigo { get; set; }
        public string UnidadMedida { get; set; } = "";
        public string Talla { get; set; } = "";
        public string Color { get; set; } = "";
        public decimal CantidadSistema { get; set; }
        public decimal? CantidadContada { get; set; }
        public decimal? CantidadAjuste { get; set; }
        public decimal CostoPromedio { get; set; }
        public string? Motivo { get; set; }
    }

    public class InventarioDetalleDto : InventarioListDto
    {
        public List<InventarioItemDto> Items { get; set; } = new();
        /// <summary>Monto configurado: con diferencias (faltante + sobrante) por encima de él, el cierre exige aprobación de gerencia.</summary>
        public decimal Umbral { get; set; }
        public bool SuperaUmbral { get; set; }
        /// <summary>Ítems con diferencia que todavía no tienen motivo.</summary>
        public int DiferenciasSinMotivo { get; set; }
        public string? SolicitadoPorNombre { get; set; }
        public DateTimeOffset? SolicitadoEn { get; set; }
        public string? DecididoPorNombre { get; set; }
        public DateTimeOffset? DecididoEn { get; set; }
        public string? MotivoDecision { get; set; }
    }

    public class InventarioDecisionDto
    {
        public string? Motivo { get; set; }
    }

    public class InventarioUmbralDto
    {
        public decimal Umbral { get; set; }
    }

    public class InventarioCrearDto
    {
        public int AlmacenId { get; set; }
        public string? Observacion { get; set; }
    }

    public class InventarioConteoItemDto
    {
        public long ItemId { get; set; }
        public decimal? CantidadContada { get; set; }
        public string? Motivo { get; set; }
    }

    public class InventarioAgregarItemDto
    {
        public long ProductoId { get; set; }
        public string Talla { get; set; } = "";
        public string Color { get; set; } = "";
    }
}

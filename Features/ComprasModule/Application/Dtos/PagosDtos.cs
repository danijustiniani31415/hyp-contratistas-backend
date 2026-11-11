namespace Abril_Backend.Features.ComprasModule.Application.Dtos
{
    /// <summary>Una cuenta por pagar: un comprobante de compra con lo pagado y el saldo.</summary>
    public class CuentaPorPagarDto
    {
        public long ComprobanteId { get; set; }
        public int ProveedorId { get; set; }
        public string ProveedorNombre { get; set; } = "";
        public string? ProveedorRuc { get; set; }
        public string Tipo { get; set; } = "";
        public string Documento { get; set; } = "";
        public DateOnly FechaEmision { get; set; }
        public string Moneda { get; set; } = "PEN";
        public decimal Total { get; set; }
        public decimal DetraccionMonto { get; set; }
        public decimal Pagado { get; set; }
        public decimal Saldo { get; set; }
        /// <summary>PENDIENTE, PARCIAL o PAGADO.</summary>
        public string EstadoPago { get; set; } = "PENDIENTE";
        public int DiasAntiguedad { get; set; }
    }

    public class PagoDto
    {
        public long Id { get; set; }
        public DateOnly FechaPago { get; set; }
        public decimal Monto { get; set; }
        public string Medio { get; set; } = "";
        public string? Referencia { get; set; }
        public string? Observacion { get; set; }
        public string Estado { get; set; } = "";
        public string RegistradoPorNombre { get; set; } = "";
        public DateTimeOffset RegistradoEn { get; set; }
        public string? MotivoAnulacion { get; set; }
    }

    public class CuentaPorPagarDetalleDto : CuentaPorPagarDto
    {
        public List<PagoDto> Pagos { get; set; } = new();
    }

    public class PagosResumenDto
    {
        /// <summary>Saldo pendiente por moneda (no se suman monedas distintas).</summary>
        public List<SaldoMonedaDto> PendientePorMoneda { get; set; } = new();
        public int DocumentosPendientes { get; set; }
        public int ProveedoresConDeuda { get; set; }
    }

    public class SaldoMonedaDto
    {
        public string Moneda { get; set; } = "PEN";
        public decimal Saldo { get; set; }
    }

    public class PagoRegistrarDto
    {
        public decimal Monto { get; set; }
        public DateOnly FechaPago { get; set; }
        /// <summary>TRANSFERENCIA, EFECTIVO, CHEQUE, DEPOSITO o YAPE_PLIN.</summary>
        public string Medio { get; set; } = "TRANSFERENCIA";
        public string? Referencia { get; set; }
        public string? Observacion { get; set; }
    }

    public class PagoAnularDto
    {
        public string Motivo { get; set; } = "";
    }
}

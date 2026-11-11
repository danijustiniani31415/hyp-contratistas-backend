using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.ComprasModule.Infrastructure.Models
{
    /// <summary>Pago (total o parcial) de un comprobante de compra. VIGENTE o ANULADO (un anulado no cuenta en el saldo).</summary>
    [Table("lb_pago_proveedor")]
    public class PagoProveedor
    {
        public long Id { get; set; }
        public long ComprobanteCompraId { get; set; }
        public DateOnly FechaPago { get; set; }
        public decimal Monto { get; set; }
        public string Medio { get; set; } = null!;
        public string? Referencia { get; set; }
        public string? Observacion { get; set; }
        public string Estado { get; set; } = "VIGENTE";
        public long RegistradoPor { get; set; }
        public DateTimeOffset RegistradoEn { get; set; }
        public long? AnuladoPor { get; set; }
        public DateTimeOffset? AnuladoEn { get; set; }
        public string? MotivoAnulacion { get; set; }
    }
}

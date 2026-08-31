namespace Abril_Backend.Features.AccountingModule.Features.InvoicesFeature.Infrastructure.Models
{
    /// <summary>Catálogo de motivos por los que se observa una factura (p. ej. "Falta sustentos").</summary>
    public class InvoiceObservationReason
    {
        public int InvoiceObservationReasonId { get; set; }
        public string InvoiceObservationReasonDescription { get; set; } = null!;
        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}

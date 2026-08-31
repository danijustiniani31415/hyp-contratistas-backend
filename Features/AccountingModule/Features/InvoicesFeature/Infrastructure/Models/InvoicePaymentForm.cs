namespace Abril_Backend.Features.AccountingModule.Features.InvoicesFeature.Infrastructure.Models
{
    /// <summary>Catálogo de formas de pago de una factura (p. ej. "A crédito", "Al contado").</summary>
    public class InvoicePaymentForm
    {
        public int InvoicePaymentFormId { get; set; }
        public string InvoicePaymentFormDescription { get; set; } = null!;
        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}

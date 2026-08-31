namespace Abril_Backend.Features.AccountingModule.Features.InvoicesFeature.Infrastructure.Models
{
    /// <summary>Catálogo de estados de una factura (Pendiente, Aprobado, Rechazado, Observado).</summary>
    public class InvoiceStatus
    {
        public int InvoiceStatusId { get; set; }
        public string InvoiceStatusDescription { get; set; } = null!;
        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}

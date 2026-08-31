namespace Abril_Backend.Features.AccountingModule.Features.InvoicesFeature.Infrastructure.Models
{
    /// <summary>Catálogo de tipo de documento de la factura (FACTURAS, RECIBOS POR HONORARIOS, etc.).</summary>
    public class InvoiceDocumentType
    {
        public int InvoiceDocumentTypeId { get; set; }
        public string Description { get; set; } = null!;
        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}

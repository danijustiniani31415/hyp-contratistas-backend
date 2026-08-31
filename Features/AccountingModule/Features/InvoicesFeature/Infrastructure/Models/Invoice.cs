using Abril_Backend.Features.CostsModule.Shared.Models;
using Abril_Backend.Features.AccountingModule.Features.Configuration.InvoiceFolderFeature.Infrastructure.Models;
using Abril_Backend.Features.Costs.Adjudicaciones.Infrastructure.Models;

namespace Abril_Backend.Features.AccountingModule.Features.InvoicesFeature.Infrastructure.Models
{
    /// <summary>Factura registrada en el módulo de Contabilidad.</summary>
    public class Invoice
    {
        public int InvoiceId { get; set; }
        public DateOnly IssueDate { get; set; }
        /// <summary>Serie del comprobante (4 caracteres, empieza con 'F'). Ej. "F001".</summary>
        public string Serie { get; set; } = null!;
        /// <summary>Correlativo del comprobante (numérico, hasta 8 dígitos). Ej. "00001234".</summary>
        public string Correlativo { get; set; } = null!;
        /// <summary>Número completo legacy (serie-correlativo). Se conserva por auditoría.</summary>
        public string? InvoiceNumber { get; set; }
        /// <summary>Proveedor (Socio de Negocio) tal cual viene del Excel. Fuente de verdad.</summary>
        public string? ProveedorName { get; set; }
        /// <summary>Razón social de Abril (nombre de la hoja del Excel). Fuente de verdad.</summary>
        public string? AbrilName { get; set; }
        /// <summary>N° Orden de Pago (Excel).</summary>
        public string? PaymentOrderNumber { get; set; }
        public int? InvoiceDocumentTypeId { get; set; }
        /// <summary>Monto "Se autoriza" (Excel).</summary>
        public decimal? AuthorizedAmount { get; set; }
        /// <summary>Observación del documento (Excel).</summary>
        public string? Observation { get; set; }
        /// <summary>Enlace opcional al proveedor en contributor (puede ser null si no se encontró).</summary>
        public int? ContributorId { get; set; }
        public string Description { get; set; } = null!;
        public int? InvoicePaymentFormId { get; set; }
        public decimal Total { get; set; }
        /// <summary>Moneda del total (reusa la tabla currency de Adjudicaciones).</summary>
        public int? CurrencyId { get; set; }
        /// <summary>URL del documento/factura (OneDrive/SharePoint o, en pruebas, Azure Blob).</summary>
        public string? DocumentUrl { get; set; }
        /// <summary>URL del documento firmado (PDF con la firma estampada). Se genera aparte del original.</summary>
        public string? SignedDocumentUrl { get; set; }
        /// <summary>Carpeta de OneDrive donde se guardó el documento.</summary>
        public int? InvoiceFolderId { get; set; }
        /// <summary>Razón social de Abril (contribuyente con es_abril = true) a la que pertenece la factura.</summary>
        public int? AbrilContributorId { get; set; }
        /// <summary>Estado de la factura (Pendiente, Aprobado, Rechazado, Observado).</summary>
        public int? InvoiceStatusId { get; set; }
        /// <summary>Motivo de observación. Solo se completa cuando la factura está Observada.</summary>
        public int? InvoiceObservationReasonId { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }

        public Contributor? Contributor { get; set; }
        public InvoicePaymentForm? InvoicePaymentForm { get; set; }
        public InvoiceFolder? InvoiceFolder { get; set; }
        public Contributor? AbrilContributor { get; set; }
        public Currency? Currency { get; set; }
        public InvoiceDocumentType? InvoiceDocumentType { get; set; }
        public InvoiceStatus? InvoiceStatus { get; set; }
        public InvoiceObservationReason? InvoiceObservationReason { get; set; }
    }
}

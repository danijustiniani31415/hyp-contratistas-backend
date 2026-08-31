using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.AccountingModule.Features.InvoicesFeature.Application.Dtos;

namespace Abril_Backend.Features.AccountingModule.Features.InvoicesFeature.Infrastructure.Interfaces
{
    public interface IInvoiceRepository
    {
        Task<List<InvoiceSupplierDto>> GetSuppliers();
        Task<List<InvoicePaymentFormDto>> GetPaymentForms();
        /// <summary>Razones sociales que maneja Abril (contribuyentes con es_abril = true).</summary>
        Task<List<InvoiceSupplierDto>> GetAbrilCompanies();
        /// <summary>Monedas activas (reusa la tabla currency de Adjudicaciones).</summary>
        Task<List<InvoiceCurrencyDto>> GetCurrencies();
        /// <summary>Motivos de observación activos (para el modal "Observar").</summary>
        Task<List<InvoiceObservationReasonDto>> GetObservationReasons();
        Task<PagedResult<InvoiceDto>> GetPaged(InvoiceFilterDto filter);
        Task<InvoiceDashboardDto> GetDashboard(InvoiceFilterDto filter);
        Task<List<InvoiceBlockGroupDto>> GetBlocks(InvoiceFilterDto filter);
        /// <summary>Detalle completo de una factura. Null si no existe.</summary>
        Task<InvoiceDetailDto?> GetDetail(int invoiceId);
        /// <summary>Actualiza una factura. documentUrl null = conservar el documento actual.</summary>
        Task Update(InvoiceUpdateDto dto, string? documentUrl, int invoiceFolderId, int userId);
        /// <summary>Asocia/actualiza el documento de una factura existente.</summary>
        Task AttachDocument(int invoiceId, string documentUrl, int userId);

        /// <summary>Asocia/actualiza el documento FIRMADO de una factura (sin tocar el original).</summary>
        Task AttachSignedDocument(int invoiceId, string signedDocumentUrl, int userId);

        /// <summary>
        /// Destino de subida vigente: id + driveId + folderId de la carpeta única configurada.
        /// Null si aún no se configuró ninguna carpeta.
        /// </summary>
        Task<(int Id, string DriveId, string FolderId)?> GetActiveFolderDestination();
        /// <summary>Nombre/razón social de un contribuyente. Null si no existe.</summary>
        Task<string?> GetContributorName(int contributorId);
        /// <summary>RUC + razón social de un contribuyente. Null si no existe.</summary>
        Task<(string? Ruc, string Name)?> GetContributorRucName(int contributorId);
        /// <summary>Mapa razón social normalizada → RUC de los contribuyentes (para nombrar documentos importados).</summary>
        Task<Dictionary<string, string>> GetContributorRucByNormalizedName();
        /// <summary>Nombre de una razón social de Abril (es_abril = true). Null si no existe o no es de Abril.</summary>
        Task<string?> GetAbrilContributorName(int abrilContributorId);

        /// <summary>Marca como "Aprobado" las facturas indicadas. Devuelve cuántas se actualizaron.</summary>
        Task<int> ApproveInvoices(List<int> invoiceIds, int userId);
        /// <summary>Marca como "Rechazado" las facturas indicadas. Devuelve cuántas se actualizaron.</summary>
        Task<int> RejectInvoices(List<int> invoiceIds, int userId);
        /// <summary>Marca como "Observado" (con motivo) las facturas indicadas. Devuelve cuántas se actualizaron.</summary>
        Task<int> ObserveInvoices(List<int> invoiceIds, int observationReasonId, int userId);

        Task Create(InvoiceCreateDto dto, string? documentUrl, int invoiceFolderId, int userId);
        Task<InvoiceImportResultDto> ImportInvoices(List<InvoiceImportRowDto> rows, Dictionary<int, string?> docUrlByIndex, int? invoiceFolderId, int userId);

        /// <summary>Crea un contribuyente/proveedor. Lanza AbrilException si el RUC ya existe.</summary>
        Task<InvoiceSupplierDto> CreateSupplier(InvoiceSupplierCreateDto dto, int userId);
    }
}

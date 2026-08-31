using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.AccountingModule.Features.InvoicesFeature.Application.Dtos;
using Abril_Backend.Shared.Services.Sunat.Dtos;
using Microsoft.AspNetCore.Http;

namespace Abril_Backend.Features.AccountingModule.Features.InvoicesFeature.Application.Interfaces
{
    public interface IInvoiceService
    {
        Task<InvoiceInitDto> GetInit(InvoiceFilterDto filter);
        Task<PagedResult<InvoiceDto>> GetPaged(InvoiceFilterDto filter);
        Task<InvoiceDashboardInitDto> GetDashboardInit(InvoiceFilterDto filter);
        Task<InvoiceDashboardDto> GetDashboard(InvoiceFilterDto filter);
        Task<List<InvoiceBlockGroupDto>> GetBlocks(InvoiceFilterDto filter);
        Task<InvoiceDetailDto> GetDetail(int invoiceId);
        Task Create(InvoiceCreateDto dto, int userId);
        Task Update(InvoiceUpdateDto dto, int userId);
        Task<InvoiceImportResultDto> Import(List<InvoiceImportRowDto> rows, IFormFileCollection files, int userId);
        /// <summary>Sube/asocia el documento de una factura existente. Devuelve la URL.</summary>
        Task<string?> UploadDocument(int invoiceId, IFormFile file, int userId);
        /// <summary>
        /// Genera un PDF firmado (firma estampada abajo a la derecha de cada página) a partir del
        /// documento de la factura, lo guarda junto al original y devuelve su URL. No toca el original.
        /// </summary>
        Task<string> Sign(int invoiceId, int userId);
        Task<InvoiceSupplierDto> CreateSupplier(InvoiceSupplierCreateDto dto, int userId);
        Task<SunatContributorDto?> GetByRuc(string ruc);

        /// <summary>Aprueba en bloque las facturas indicadas. Devuelve cuántas se actualizaron.</summary>
        Task<int> Approve(List<int> invoiceIds, int userId);
        /// <summary>Rechaza en bloque las facturas indicadas. Devuelve cuántas se actualizaron.</summary>
        Task<int> Reject(List<int> invoiceIds, int userId);
        /// <summary>Observa en bloque las facturas indicadas con un motivo. Devuelve cuántas se actualizaron.</summary>
        Task<int> Observe(List<int> invoiceIds, int observationReasonId, int userId);
    }
}

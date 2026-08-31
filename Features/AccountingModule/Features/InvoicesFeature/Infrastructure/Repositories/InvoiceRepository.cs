using Microsoft.EntityFrameworkCore;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Features.AccountingModule.Features.InvoicesFeature.Application.Dtos;
using Abril_Backend.Features.AccountingModule.Features.InvoicesFeature.Application.Helpers;
using Abril_Backend.Features.AccountingModule.Features.InvoicesFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.AccountingModule.Features.InvoicesFeature.Infrastructure.Models;
using Abril_Backend.Features.CostsModule.Shared.Models;

namespace Abril_Backend.Features.AccountingModule.Features.InvoicesFeature.Infrastructure.Repositories
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private const int PageSize = 10;
        private readonly IDbContextFactory<AppDbContext> _factory;

        public InvoiceRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<InvoiceSupplierDto>> GetSuppliers()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Contributor
                .Where(c => c.State && c.Active)
                .OrderBy(c => c.ContributorName)
                .Select(c => new InvoiceSupplierDto
                {
                    ContributorId = c.ContributorId,
                    ContributorRuc = c.ContributorRuc,
                    ContributorName = c.ContributorName
                })
                .ToListAsync();
        }

        public async Task<List<InvoiceSupplierDto>> GetAbrilCompanies()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Contributor
                .Where(c => c.State && c.Active && c.EsAbril)
                .OrderBy(c => c.ContributorName)
                .Select(c => new InvoiceSupplierDto
                {
                    ContributorId = c.ContributorId,
                    ContributorRuc = c.ContributorRuc,
                    ContributorName = c.ContributorName
                })
                .ToListAsync();
        }

        public async Task<List<InvoiceCurrencyDto>> GetCurrencies()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Currency
                .Where(c => c.State && c.Active)
                .OrderBy(c => c.CurrencyId)
                .Select(c => new InvoiceCurrencyDto
                {
                    CurrencyId = c.CurrencyId,
                    CurrencyCode = c.CurrencyCode,
                    CurrencyDescription = c.CurrencyDescription,
                    CurrencySymbol = c.CurrencySymbol
                })
                .ToListAsync();
        }

        public async Task<List<InvoiceObservationReasonDto>> GetObservationReasons()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.InvoiceObservationReason
                .Where(r => r.State && r.Active)
                .OrderBy(r => r.InvoiceObservationReasonId)
                .Select(r => new InvoiceObservationReasonDto
                {
                    InvoiceObservationReasonId = r.InvoiceObservationReasonId,
                    InvoiceObservationReasonDescription = r.InvoiceObservationReasonDescription
                })
                .ToListAsync();
        }

        // ── Acciones en bloque: aprobar / rechazar / observar ──────────────────
        public Task<int> ApproveInvoices(List<int> invoiceIds, int userId)
            => SetStatusByDescription(invoiceIds, "Aprobado", null, userId);

        public Task<int> RejectInvoices(List<int> invoiceIds, int userId)
            => SetStatusByDescription(invoiceIds, "Rechazado", null, userId);

        public Task<int> ObserveInvoices(List<int> invoiceIds, int observationReasonId, int userId)
            => SetStatusByDescription(invoiceIds, "Observado", observationReasonId, userId);

        /// <summary>
        /// Resuelve el id del estado por su descripción y lo aplica a las facturas indicadas.
        /// Al aprobar/rechazar limpia el motivo de observación; al observar lo asigna.
        /// </summary>
        private async Task<int> SetStatusByDescription(List<int> invoiceIds, string statusDescription, int? reasonId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var statusId = await ctx.InvoiceStatus
                .Where(s => s.State && s.InvoiceStatusDescription.ToLower() == statusDescription.ToLower())
                .Select(s => (int?)s.InvoiceStatusId)
                .FirstOrDefaultAsync()
                ?? throw new AbrilException($"No existe el estado '{statusDescription}' en el catálogo.");

            var invoices = await ctx.Invoice
                .Where(i => i.State && invoiceIds.Contains(i.InvoiceId))
                .ToListAsync();

            if (invoices.Count == 0)
                throw new AbrilException("No se encontraron facturas para actualizar.");

            var now = DateTimeOffset.UtcNow;
            foreach (var inv in invoices)
            {
                inv.InvoiceStatusId = statusId;
                inv.InvoiceObservationReasonId = reasonId;
                inv.UpdatedDateTime = now;
                inv.UpdatedUserId = userId;
            }

            await ctx.SaveChangesAsync();
            return invoices.Count;
        }

        public async Task<InvoiceDetailDto?> GetDetail(int invoiceId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Invoice
                .Where(i => i.InvoiceId == invoiceId && i.State)
                .Select(i => new InvoiceDetailDto
                {
                    InvoiceId = i.InvoiceId,
                    IssueDate = i.IssueDate,
                    Serie = i.Serie,
                    Correlativo = i.Correlativo,
                    ContributorId = i.ContributorId,
                    ContributorRuc = i.Contributor!.ContributorRuc ?? "",
                    ContributorName = i.ProveedorName ?? i.Contributor!.ContributorName,
                    AbrilContributorId = i.AbrilContributorId,
                    AbrilContributorName = i.AbrilName ?? i.AbrilContributor!.ContributorName,
                    AbrilContributorRuc = i.AbrilContributor!.ContributorRuc,
                    Description = i.Description,
                    InvoicePaymentFormId = i.InvoicePaymentFormId ?? 0,
                    InvoicePaymentFormDescription = i.InvoicePaymentForm!.InvoicePaymentFormDescription,
                    Total = i.Total,
                    CurrencyId = i.CurrencyId,
                    CurrencyCode = i.Currency!.CurrencyCode,
                    CurrencySymbol = i.Currency.CurrencySymbol,
                    InvoiceStatusId = i.InvoiceStatusId,
                    InvoiceStatusDescription = i.InvoiceStatus!.InvoiceStatusDescription,
                    InvoiceObservationReasonId = i.InvoiceObservationReasonId,
                    InvoiceObservationReasonDescription = i.InvoiceObservationReason!.InvoiceObservationReasonDescription,
                    InvoiceFolderId = i.InvoiceFolderId,
                    InvoiceFolderName = i.InvoiceFolder!.FolderName,
                    DocumentUrl = i.DocumentUrl,
                    SignedDocumentUrl = i.SignedDocumentUrl,
                    CreatedDateTime = i.CreatedDateTime.ToOffset(TimeSpan.FromHours(-5)).DateTime,
                    UpdatedDateTime = i.UpdatedDateTime.HasValue
                        ? i.UpdatedDateTime.Value.ToOffset(TimeSpan.FromHours(-5)).DateTime
                        : (DateTime?)null
                })
                .FirstOrDefaultAsync();
        }

        public async Task Update(InvoiceUpdateDto dto, string? documentUrl, int invoiceFolderId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var record = await ctx.Invoice.FirstOrDefaultAsync(i => i.InvoiceId == dto.InvoiceId && i.State)
                ?? throw new AbrilException("La factura no existe.");

            var serie = dto.Serie.Trim();
            var correlativo = dto.Correlativo.Trim();

            var duplicate = await ctx.Invoice.AnyAsync(i =>
                i.State &&
                i.InvoiceId != dto.InvoiceId &&
                i.ContributorId == dto.ContributorId &&
                i.Serie.ToLower() == serie.ToLower() &&
                i.Correlativo.ToLower() == correlativo.ToLower());
            if (duplicate)
                throw new AbrilException("Ya existe otra factura con esa serie y correlativo para el proveedor seleccionado.");

            record.IssueDate = dto.IssueDate;
            record.Serie = serie;
            record.Correlativo = correlativo;
            record.InvoiceNumber = $"{serie}-{correlativo}";
            record.ContributorId = dto.ContributorId;
            record.AbrilContributorId = dto.AbrilContributorId;
            record.InvoiceFolderId = invoiceFolderId;
            record.Description = dto.Description.Trim();
            record.InvoicePaymentFormId = dto.InvoicePaymentFormId;
            record.Total = dto.Total;
            record.CurrencyId = dto.CurrencyId;
            if (documentUrl != null) record.DocumentUrl = documentUrl;
            record.UpdatedDateTime = DateTimeOffset.UtcNow;
            record.UpdatedUserId = userId;

            await ctx.SaveChangesAsync();
        }

        public async Task AttachDocument(int invoiceId, string documentUrl, int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var record = await ctx.Invoice.FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && i.State)
                ?? throw new AbrilException("La factura no existe.");
            record.DocumentUrl = documentUrl;
            record.UpdatedDateTime = DateTimeOffset.UtcNow;
            record.UpdatedUserId = userId;
            await ctx.SaveChangesAsync();
        }

        public async Task AttachSignedDocument(int invoiceId, string signedDocumentUrl, int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var record = await ctx.Invoice.FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && i.State)
                ?? throw new AbrilException("La factura no existe.");
            record.SignedDocumentUrl = signedDocumentUrl;
            record.UpdatedDateTime = DateTimeOffset.UtcNow;
            record.UpdatedUserId = userId;
            await ctx.SaveChangesAsync();
        }

        public async Task<(int Id, string DriveId, string FolderId)?> GetActiveFolderDestination()
        {
            using var ctx = _factory.CreateDbContext();
            var f = await ctx.InvoiceFolder
                .Where(x => x.State && x.Active)
                .OrderBy(x => x.InvoiceFolderId)
                .Select(x => new { x.InvoiceFolderId, x.DriveId, x.FolderId })
                .FirstOrDefaultAsync();
            return f == null ? null : (f.InvoiceFolderId, f.DriveId, f.FolderId);
        }

        public async Task<string?> GetContributorName(int contributorId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Contributor
                .Where(c => c.ContributorId == contributorId && c.State)
                .Select(c => c.ContributorName)
                .FirstOrDefaultAsync();
        }

        public async Task<(string? Ruc, string Name)?> GetContributorRucName(int contributorId)
        {
            using var ctx = _factory.CreateDbContext();
            var row = await ctx.Contributor
                .Where(c => c.ContributorId == contributorId && c.State)
                .Select(c => new { c.ContributorRuc, c.ContributorName })
                .FirstOrDefaultAsync();
            return row == null ? null : (row.ContributorRuc, row.ContributorName);
        }

        public async Task<Dictionary<string, string>> GetContributorRucByNormalizedName()
        {
            using var ctx = _factory.CreateDbContext();
            var list = await ctx.Contributor
                .Where(c => c.State)
                .Select(c => new { c.ContributorName, c.ContributorRuc })
                .ToListAsync();

            var map = new Dictionary<string, string>();
            foreach (var c in list)
            {
                if (string.IsNullOrWhiteSpace(c.ContributorRuc)) continue;
                var key = InvoiceTextHelper.NormalizeName(c.ContributorName);
                if (!map.ContainsKey(key)) map[key] = c.ContributorRuc.Trim();
            }
            return map;
        }

        public async Task<string?> GetAbrilContributorName(int abrilContributorId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Contributor
                .Where(c => c.ContributorId == abrilContributorId && c.State && c.EsAbril)
                .Select(c => c.ContributorName)
                .FirstOrDefaultAsync();
        }

        public async Task<List<InvoicePaymentFormDto>> GetPaymentForms()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.InvoicePaymentForm
                .Where(p => p.State && p.Active)
                .OrderBy(p => p.InvoicePaymentFormId)
                .Select(p => new InvoicePaymentFormDto
                {
                    InvoicePaymentFormId = p.InvoicePaymentFormId,
                    InvoicePaymentFormDescription = p.InvoicePaymentFormDescription
                })
                .ToListAsync();
        }

        /// <summary>Aplica los mismos filtros usados en la tabla y el dashboard.</summary>
        private static IQueryable<Invoice> ApplyFilters(IQueryable<Invoice> query, InvoiceFilterDto filter)
        {
            if (filter.ContributorId.HasValue)
                query = query.Where(i => i.ContributorId == filter.ContributorId.Value);

            if (!string.IsNullOrWhiteSpace(filter.ContributorRuc))
            {
                var ruc = filter.ContributorRuc.Trim();
                query = query.Where(i => i.Contributor!.ContributorRuc.Contains(ruc));
            }

            if (filter.AbrilContributorId.HasValue)
                query = query.Where(i => i.AbrilContributorId == filter.AbrilContributorId.Value);

            if (!string.IsNullOrWhiteSpace(filter.AbrilContributorRuc))
            {
                var ruc = filter.AbrilContributorRuc.Trim();
                query = query.Where(i => i.AbrilContributor!.ContributorRuc.Contains(ruc));
            }

            if (filter.InvoicePaymentFormId.HasValue)
                query = query.Where(i => i.InvoicePaymentFormId == filter.InvoicePaymentFormId.Value);

            if (filter.CurrencyId.HasValue)
                query = query.Where(i => i.CurrencyId == filter.CurrencyId.Value);

            if (filter.TotalMin.HasValue)
                query = query.Where(i => i.Total >= filter.TotalMin.Value);

            if (filter.TotalMax.HasValue)
                query = query.Where(i => i.Total <= filter.TotalMax.Value);

            if (filter.IssueDateFrom.HasValue)
                query = query.Where(i => i.IssueDate >= filter.IssueDateFrom.Value);

            if (filter.IssueDateTo.HasValue)
                query = query.Where(i => i.IssueDate <= filter.IssueDateTo.Value);

            if (!string.IsNullOrWhiteSpace(filter.Serie))
            {
                var serie = filter.Serie.Trim().ToLower();
                query = query.Where(i => i.Serie.ToLower().Contains(serie));
            }

            if (!string.IsNullOrWhiteSpace(filter.Correlativo))
            {
                var corr = filter.Correlativo.Trim().ToLower();
                query = query.Where(i => i.Correlativo.ToLower().Contains(corr));
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var s = filter.Search.Trim().ToLower();
                query = query.Where(i =>
                    i.Serie.ToLower().Contains(s) ||
                    i.Correlativo.ToLower().Contains(s) ||
                    (i.ProveedorName != null && i.ProveedorName.ToLower().Contains(s)) ||
                    i.Contributor!.ContributorName.ToLower().Contains(s) ||
                    i.Contributor.ContributorRuc.Contains(s));
            }

            return query;
        }

        /// <summary>
        /// Aplica el orden de la tabla según <see cref="InvoiceFilterDto.SortBy"/> / <see cref="InvoiceFilterDto.SortDir"/>.
        /// Si no se indica columna (o es desconocida) se usa el orden original: más recientes primero (InvoiceId desc).
        /// El desempate siempre es por InvoiceId para que el orden sea estable entre páginas.
        /// </summary>
        private static IOrderedQueryable<Invoice> ApplyOrder(IQueryable<Invoice> query, InvoiceFilterDto filter)
        {
            var asc = !string.Equals(filter.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

            switch ((filter.SortBy ?? "").Trim().ToLower())
            {
                case "issuedate":
                    return asc
                        ? query.OrderBy(i => i.IssueDate).ThenByDescending(i => i.InvoiceId)
                        : query.OrderByDescending(i => i.IssueDate).ThenByDescending(i => i.InvoiceId);

                case "invoicenumber":
                    return asc
                        ? query.OrderBy(i => i.Serie).ThenBy(i => i.Correlativo).ThenByDescending(i => i.InvoiceId)
                        : query.OrderByDescending(i => i.Serie).ThenByDescending(i => i.Correlativo).ThenByDescending(i => i.InvoiceId);

                case "contributorname":
                    return asc
                        ? query.OrderBy(i => i.ProveedorName ?? i.Contributor!.ContributorName).ThenByDescending(i => i.InvoiceId)
                        : query.OrderByDescending(i => i.ProveedorName ?? i.Contributor!.ContributorName).ThenByDescending(i => i.InvoiceId);

                case "contributorruc":
                    return asc
                        ? query.OrderBy(i => i.Contributor!.ContributorRuc).ThenByDescending(i => i.InvoiceId)
                        : query.OrderByDescending(i => i.Contributor!.ContributorRuc).ThenByDescending(i => i.InvoiceId);

                case "abrilcontributorname":
                    return asc
                        ? query.OrderBy(i => i.AbrilName ?? i.AbrilContributor!.ContributorName).ThenByDescending(i => i.InvoiceId)
                        : query.OrderByDescending(i => i.AbrilName ?? i.AbrilContributor!.ContributorName).ThenByDescending(i => i.InvoiceId);

                case "description":
                    return asc
                        ? query.OrderBy(i => i.Description).ThenByDescending(i => i.InvoiceId)
                        : query.OrderByDescending(i => i.Description).ThenByDescending(i => i.InvoiceId);

                case "invoicepaymentformdescription":
                    return asc
                        ? query.OrderBy(i => i.InvoicePaymentForm!.InvoicePaymentFormDescription).ThenByDescending(i => i.InvoiceId)
                        : query.OrderByDescending(i => i.InvoicePaymentForm!.InvoicePaymentFormDescription).ThenByDescending(i => i.InvoiceId);

                case "total":
                    return asc
                        ? query.OrderBy(i => i.Total).ThenByDescending(i => i.InvoiceId)
                        : query.OrderByDescending(i => i.Total).ThenByDescending(i => i.InvoiceId);

                case "invoicestatusdescription":
                    return asc
                        ? query.OrderBy(i => i.InvoiceStatus!.InvoiceStatusDescription).ThenByDescending(i => i.InvoiceId)
                        : query.OrderByDescending(i => i.InvoiceStatus!.InvoiceStatusDescription).ThenByDescending(i => i.InvoiceId);

                default:
                    return query.OrderByDescending(i => i.InvoiceId);
            }
        }

        public async Task<List<InvoiceBlockGroupDto>> GetBlocks(InvoiceFilterDto filter)
        {
            using var ctx = _factory.CreateDbContext();
            var query = ApplyFilters(ctx.Invoice.Where(i => i.State), filter);

            var items = await query
                .OrderByDescending(i => i.InvoiceId)
                .Select(i => new InvoiceDto
                {
                    InvoiceId = i.InvoiceId,
                    IssueDate = i.IssueDate,
                    Serie = i.Serie,
                    Correlativo = i.Correlativo,
                    ContributorId = i.ContributorId,
                    ContributorRuc = i.Contributor!.ContributorRuc ?? "",
                    ContributorName = i.ProveedorName ?? i.Contributor!.ContributorName,
                    AbrilContributorId = i.AbrilContributorId,
                    AbrilContributorName = i.AbrilName ?? i.AbrilContributor!.ContributorName,
                    AbrilName = i.AbrilName,
                    Description = i.Description,
                    InvoicePaymentFormId = i.InvoicePaymentFormId ?? 0,
                    InvoicePaymentFormDescription = i.InvoicePaymentForm!.InvoicePaymentFormDescription,
                    Total = i.Total,
                    CurrencyId = i.CurrencyId,
                    CurrencyCode = i.Currency!.CurrencyCode,
                    CurrencySymbol = i.Currency.CurrencySymbol,
                    InvoiceStatusId = i.InvoiceStatusId,
                    InvoiceStatusDescription = i.InvoiceStatus!.InvoiceStatusDescription,
                    InvoiceObservationReasonId = i.InvoiceObservationReasonId,
                    InvoiceObservationReasonDescription = i.InvoiceObservationReason!.InvoiceObservationReasonDescription,
                    DocumentUrl = i.DocumentUrl,
                    SignedDocumentUrl = i.SignedDocumentUrl,
                    CreatedDateTime = i.CreatedDateTime.ToOffset(TimeSpan.FromHours(-5)).DateTime
                })
                .ToListAsync();

            return items
                .GroupBy(i => AbrilShortLabel(i.AbrilName, i.AbrilContributorName))
                .Select(g => new InvoiceBlockGroupDto
                {
                    AbrilName = g.Key,
                    Count = g.Count(),
                    Items = g.ToList()
                })
                .OrderBy(g => g.AbrilName)
                .ToList();
        }

        /// <summary>
        /// Nombre corto de la razón social de Abril para el encabezado de bloques.
        /// Si vino del Excel usa el nombre de la hoja (ya es corto y en mayúsculas, ej. "ALTAMURA").
        /// Si no, recorta sufijos societarios del nombre del contribuyente
        /// (ej. "Oporto Inmobiliaria S.A.C" → "OPORTO").
        /// </summary>
        private static string AbrilShortLabel(string? abrilName, string? contributorName)
        {
            if (!string.IsNullOrWhiteSpace(abrilName))
                return abrilName.Trim().ToUpper();

            var name = (contributorName ?? "Sin razón social").Trim();
            var stopWords = new[] { "INMOBILIARIA", "INVERSIONES", "GRUPO", "CORPORACION", "CORPORACIÓN", "INMOBILIARIO", "INMOBILIARIAS", "S.A.C", "S.A.C.", "SAC", "S.A", "E.I.R.L", "EIRL" };
            var tokens = name.ToUpper().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var kept = tokens.TakeWhile(t => !stopWords.Contains(t)).ToList();
            var result = kept.Count > 0 ? string.Join(" ", kept) : tokens.FirstOrDefault() ?? name.ToUpper();
            return result;
        }

        public async Task<InvoiceDashboardDto> GetDashboard(InvoiceFilterDto filter)
        {
            using var ctx = _factory.CreateDbContext();
            var query = ApplyFilters(ctx.Invoice.Where(i => i.State), filter);

            var totalCount = await query.CountAsync();

            var byCurrency = await query
                .GroupBy(i => new { i.CurrencyId, Code = i.Currency!.CurrencyCode, Symbol = i.Currency.CurrencySymbol })
                .Select(g => new InvoiceCurrencyTotalDto
                {
                    CurrencyCode = g.Key.Code ?? "—",
                    CurrencySymbol = g.Key.Symbol,
                    Total = g.Sum(x => x.Total),
                    Count = g.Count()
                })
                .ToListAsync();

            var byMonth = await query
                .GroupBy(i => new { i.IssueDate.Year, i.IssueDate.Month })
                .Select(g => new InvoiceChartItemDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Label = g.Key.Year + "-" + g.Key.Month,
                    Total = g.Sum(x => x.Total),
                    Count = g.Count()
                })
                .ToListAsync();

            var byPaymentForm = await query
                .GroupBy(i => i.InvoicePaymentForm!.InvoicePaymentFormDescription)
                .Select(g => new InvoiceChartItemDto { Label = g.Key, Total = g.Sum(x => x.Total), Count = g.Count() })
                .ToListAsync();

            var byAbril = await query
                .GroupBy(i => i.AbrilName ?? i.AbrilContributor!.ContributorName)
                .Select(g => new InvoiceChartItemDto { Label = g.Key ?? "—", Total = g.Sum(x => x.Total), Count = g.Count() })
                .ToListAsync();

            var topSuppliers = await query
                .GroupBy(i => i.ProveedorName ?? i.Contributor!.ContributorName)
                .Select(g => new InvoiceChartItemDto { Label = g.Key ?? "—", Total = g.Sum(x => x.Total), Count = g.Count() })
                .OrderByDescending(x => x.Total)
                .Take(10)
                .ToListAsync();

            return new InvoiceDashboardDto
            {
                TotalCount = totalCount,
                TotalsByCurrency = byCurrency.OrderByDescending(c => c.Total).ToList(),
                ByMonth = byMonth.OrderBy(m => m.Year).ThenBy(m => m.Month).ToList(),
                ByPaymentForm = byPaymentForm.OrderByDescending(p => p.Total).ToList(),
                ByAbril = byAbril.OrderByDescending(a => a.Total).ToList(),
                TopSuppliers = topSuppliers
            };
        }

        public async Task<PagedResult<InvoiceDto>> GetPaged(InvoiceFilterDto filter)
        {
            using var ctx = _factory.CreateDbContext();

            var query = ApplyFilters(ctx.Invoice.Where(i => i.State), filter);

            var totalRecords = await query.CountAsync();

            var data = await ApplyOrder(query, filter)
                .Skip((filter.Page - 1) * PageSize)
                .Take(PageSize)
                .Select(i => new InvoiceDto
                {
                    InvoiceId = i.InvoiceId,
                    IssueDate = i.IssueDate,
                    Serie = i.Serie,
                    Correlativo = i.Correlativo,
                    ContributorId = i.ContributorId,
                    ContributorRuc = i.Contributor!.ContributorRuc ?? "",
                    ContributorName = i.ProveedorName ?? i.Contributor!.ContributorName,
                    AbrilContributorId = i.AbrilContributorId,
                    AbrilContributorName = i.AbrilName ?? i.AbrilContributor!.ContributorName,
                    Description = i.Description,
                    InvoicePaymentFormId = i.InvoicePaymentFormId ?? 0,
                    InvoicePaymentFormDescription = i.InvoicePaymentForm!.InvoicePaymentFormDescription,
                    Total = i.Total,
                    CurrencyId = i.CurrencyId,
                    CurrencyCode = i.Currency!.CurrencyCode,
                    CurrencySymbol = i.Currency.CurrencySymbol,
                    InvoiceStatusId = i.InvoiceStatusId,
                    InvoiceStatusDescription = i.InvoiceStatus!.InvoiceStatusDescription,
                    InvoiceObservationReasonId = i.InvoiceObservationReasonId,
                    InvoiceObservationReasonDescription = i.InvoiceObservationReason!.InvoiceObservationReasonDescription,
                    DocumentUrl = i.DocumentUrl,
                    SignedDocumentUrl = i.SignedDocumentUrl,
                    CreatedDateTime = i.CreatedDateTime.ToOffset(TimeSpan.FromHours(-5)).DateTime
                })
                .ToListAsync();

            return new PagedResult<InvoiceDto>
            {
                Page = filter.Page,
                PageSize = PageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)PageSize),
                Data = data
            };
        }

        public async Task Create(InvoiceCreateDto dto, string? documentUrl, int invoiceFolderId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var serie = dto.Serie.Trim();
            var correlativo = dto.Correlativo.Trim();

            var exists = await ctx.Invoice.AnyAsync(i =>
                i.State &&
                i.ContributorId == dto.ContributorId &&
                i.Serie.ToLower() == serie.ToLower() &&
                i.Correlativo.ToLower() == correlativo.ToLower());
            if (exists)
                throw new AbrilException("Ya existe una factura con esa serie y correlativo para el proveedor seleccionado.");

            ctx.Invoice.Add(new Invoice
            {
                IssueDate = dto.IssueDate,
                Serie = serie,
                Correlativo = correlativo,
                InvoiceNumber = $"{serie}-{correlativo}",
                ContributorId = dto.ContributorId,
                Description = dto.Description.Trim(),
                InvoicePaymentFormId = dto.InvoicePaymentFormId,
                Total = dto.Total,
                CurrencyId = dto.CurrencyId,
                DocumentUrl = documentUrl,
                InvoiceFolderId = invoiceFolderId,
                AbrilContributorId = dto.AbrilContributorId,
                CreatedDateTime = DateTimeOffset.UtcNow,
                CreatedUserId = userId,
                Active = true,
                State = true
            });
            await ctx.SaveChangesAsync();
        }

        public async Task<InvoiceImportResultDto> ImportInvoices(
            List<InvoiceImportRowDto> rows, Dictionary<int, string?> docUrlByIndex, int? invoiceFolderId, int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var now = DateTimeOffset.UtcNow;

            // Catálogos en memoria (pocos roundtrips).
            var currencyMap = await ctx.Currency
                .Where(c => c.State && c.Active)
                .ToDictionaryAsync(c => c.CurrencyCode.ToUpper(), c => c.CurrencyId);

            var abrilList = await ctx.Contributor
                .Where(c => c.State && c.EsAbril)
                .Select(c => new { c.ContributorId, c.ContributorName })
                .ToListAsync();
            var abrilNorm = abrilList
                .Select(a => new { a.ContributorId, Norm = RemoveDiacritics(a.ContributorName).ToUpper() })
                .ToList();

            var contributorMap = new Dictionary<string, int>();
            foreach (var c in await ctx.Contributor.Where(c => c.State).Select(c => new { c.ContributorId, c.ContributorName }).ToListAsync())
            {
                var key = RemoveDiacritics(c.ContributorName).Trim().ToUpper();
                if (!contributorMap.ContainsKey(key)) contributorMap[key] = c.ContributorId;
            }

            // Tipos de documento: crear los que falten.
            var docTypeMap = await ctx.InvoiceDocumentType
                .Where(t => t.State)
                .ToDictionaryAsync(t => t.Description.ToUpper(), t => t.InvoiceDocumentTypeId);
            var newTypes = rows
                .Select(r => (r.DocumentType ?? "").Trim())
                .Where(d => d.Length > 0 && !docTypeMap.ContainsKey(d.ToUpper()))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var desc in newTypes)
            {
                var t = new InvoiceDocumentType { Description = desc, Active = true, State = true, CreatedDateTime = now, CreatedUserId = userId };
                ctx.InvoiceDocumentType.Add(t);
            }
            if (newTypes.Count > 0)
            {
                await ctx.SaveChangesAsync();
                docTypeMap = await ctx.InvoiceDocumentType.Where(t => t.State).ToDictionaryAsync(t => t.Description.ToUpper(), t => t.InvoiceDocumentTypeId);
            }

            int withFile = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var serie = (r.Serie ?? "").Trim();
                var correlativo = (r.Correlativo ?? "").Trim();
                var number = serie.Length > 0 ? $"{serie}-{correlativo}" : correlativo;

                int? currencyId = null;
                if (!string.IsNullOrWhiteSpace(r.CurrencyCode) && currencyMap.TryGetValue(r.CurrencyCode.Trim().ToUpper(), out var cid))
                    currencyId = cid;

                int? docTypeId = null;
                if (!string.IsNullOrWhiteSpace(r.DocumentType) && docTypeMap.TryGetValue(r.DocumentType.Trim().ToUpper(), out var dtid))
                    docTypeId = dtid;

                int? contributorId = null;
                if (!string.IsNullOrWhiteSpace(r.ProveedorName) &&
                    contributorMap.TryGetValue(RemoveDiacritics(r.ProveedorName).Trim().ToUpper(), out var coid))
                    contributorId = coid;

                int? abrilContributorId = null;
                if (!string.IsNullOrWhiteSpace(r.AbrilName))
                {
                    var token = RemoveDiacritics(r.AbrilName).Trim().ToUpper();
                    var match = abrilNorm.FirstOrDefault(a => a.Norm.Contains(token));
                    if (match != null) abrilContributorId = match.ContributorId;
                }

                DateOnly issueDate = default;
                if (!string.IsNullOrWhiteSpace(r.IssueDate) && DateOnly.TryParse(r.IssueDate, out var parsed))
                    issueDate = parsed;

                docUrlByIndex.TryGetValue(i, out var documentUrl);
                if (!string.IsNullOrWhiteSpace(documentUrl)) withFile++;

                ctx.Invoice.Add(new Invoice
                {
                    IssueDate = issueDate,
                    Serie = serie,
                    Correlativo = correlativo,
                    InvoiceNumber = number,
                    ProveedorName = r.ProveedorName?.Trim(),
                    AbrilName = r.AbrilName?.Trim(),
                    PaymentOrderNumber = r.PaymentOrderNumber?.Trim(),
                    InvoiceDocumentTypeId = docTypeId,
                    AuthorizedAmount = r.AuthorizedAmount,
                    Observation = r.Observation?.Trim(),
                    ContributorId = contributorId,
                    AbrilContributorId = abrilContributorId,
                    Description = (r.Description ?? "").Trim(),
                    InvoicePaymentFormId = null,
                    Total = r.Total,
                    CurrencyId = currencyId,
                    DocumentUrl = documentUrl,
                    InvoiceFolderId = invoiceFolderId,
                    CreatedDateTime = now,
                    CreatedUserId = userId,
                    Active = true,
                    State = true
                });
            }

            await ctx.SaveChangesAsync();

            return new InvoiceImportResultDto
            {
                Inserted = rows.Count,
                WithFile = withFile,
                WithoutFile = rows.Count - withFile
            };
        }

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var c in normalized)
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        public async Task<InvoiceSupplierDto> CreateSupplier(InvoiceSupplierCreateDto dto, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var ruc = dto.ContributorRuc.Trim();

            var existing = await ctx.Contributor.FirstOrDefaultAsync(c => c.ContributorRuc == ruc && c.State);
            if (existing != null)
                throw new AbrilException("Ya existe un proveedor registrado con ese RUC.");

            var contributor = new Contributor
            {
                ContributorRuc = ruc,
                ContributorName = dto.ContributorName.Trim(),
                ContributorAddress = dto.ContributorAddress?.Trim() ?? string.Empty,
                ContributorEconomicActivityDescription = dto.ContributorEconomicActivityDescription?.Trim(),
                ContributorDistrict = dto.ContributorDistrict?.Trim(),
                ContributorProvince = dto.ContributorProvince?.Trim(),
                ContributorDepartment = dto.ContributorDepartment?.Trim(),
                CreatedDateTime = DateTimeOffset.UtcNow,
                CreatedUserId = userId,
                Active = true,
                State = true
            };
            ctx.Contributor.Add(contributor);
            await ctx.SaveChangesAsync();

            return new InvoiceSupplierDto
            {
                ContributorId = contributor.ContributorId,
                ContributorRuc = contributor.ContributorRuc,
                ContributorName = contributor.ContributorName
            };
        }
    }
}

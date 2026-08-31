using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Features.AccountingModule.Features.Configuration.InvoiceFolderFeature.Application.Dtos;
using Abril_Backend.Features.AccountingModule.Features.Configuration.InvoiceFolderFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.AccountingModule.Features.Configuration.InvoiceFolderFeature.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.AccountingModule.Features.Configuration.InvoiceFolderFeature.Infrastructure.Repositories
{
    public class InvoiceFolderRepository : IInvoiceFolderRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public InvoiceFolderRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<InvoiceFolderDto?> GetSingleton()
        {
            using var ctx = _factory.CreateDbContext();

            return await ctx.InvoiceFolder
                .Where(f => f.State)
                .OrderBy(f => f.InvoiceFolderId)
                .Select(f => new InvoiceFolderDto
                {
                    InvoiceFolderId = f.InvoiceFolderId,
                    LinkUrl = f.LinkUrl,
                    DriveId = f.DriveId,
                    FolderId = f.FolderId,
                    FolderName = f.FolderName,
                    WebUrl = f.WebUrl,
                    Active = f.Active,
                    CreatedDateTime = f.CreatedDateTime.ToOffset(TimeSpan.FromHours(-5)).DateTime,
                    CreatedUserId = f.CreatedUserId
                })
                .FirstOrDefaultAsync();
        }

        public async Task Upsert(string linkUrl, string driveId, string folderId, string? folderName, string? webUrl, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var record = await ctx.InvoiceFolder
                .Where(f => f.State)
                .OrderBy(f => f.InvoiceFolderId)
                .FirstOrDefaultAsync();

            if (record == null)
            {
                ctx.InvoiceFolder.Add(new InvoiceFolder
                {
                    LinkUrl = linkUrl,
                    DriveId = driveId,
                    FolderId = folderId,
                    FolderName = folderName,
                    WebUrl = webUrl,
                    Active = true,
                    State = true,
                    CreatedDateTime = DateTimeOffset.UtcNow,
                    CreatedUserId = userId
                });
            }
            else
            {
                record.LinkUrl = linkUrl;
                record.DriveId = driveId;
                record.FolderId = folderId;
                record.FolderName = folderName;
                record.WebUrl = webUrl;
                record.Active = true;
                record.UpdatedDateTime = DateTimeOffset.UtcNow;
                record.UpdatedUserId = userId;
            }

            await ctx.SaveChangesAsync();
        }

        public async Task<(int Id, string DriveId, string FolderId)?> GetActiveDestination()
        {
            using var ctx = _factory.CreateDbContext();

            var f = await ctx.InvoiceFolder
                .Where(x => x.State && x.Active)
                .OrderBy(x => x.InvoiceFolderId)
                .Select(x => new { x.InvoiceFolderId, x.DriveId, x.FolderId })
                .FirstOrDefaultAsync();

            return f == null ? null : (f.InvoiceFolderId, f.DriveId, f.FolderId);
        }
    }
}

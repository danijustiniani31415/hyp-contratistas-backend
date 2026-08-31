using Abril_Backend.Features.GestionAdministrativa.CarpetaAdjuntos.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.CarpetaAdjuntos.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.Shared.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.GestionAdministrativa.CarpetaAdjuntos.Infrastructure.Repositories
{
    public class CarpetaAdjuntosRepository : ICarpetaAdjuntosRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public CarpetaAdjuntosRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<GaAdjuntoFolderDto?> GetSingleton()
        {
            using var ctx = _factory.CreateDbContext();

            return await ctx.GaAdjuntoFolder
                .Where(f => f.State)
                .OrderBy(f => f.GaAdjuntoFolderId)
                .Select(f => new GaAdjuntoFolderDto
                {
                    GaAdjuntoFolderId = f.GaAdjuntoFolderId,
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

            var record = await ctx.GaAdjuntoFolder
                .Where(f => f.State)
                .OrderBy(f => f.GaAdjuntoFolderId)
                .FirstOrDefaultAsync();

            if (record == null)
            {
                ctx.GaAdjuntoFolder.Add(new GaAdjuntoFolder
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
    }
}

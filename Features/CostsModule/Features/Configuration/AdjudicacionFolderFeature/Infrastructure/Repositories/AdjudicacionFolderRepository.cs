using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Features.CostsModule.Features.Configuration.AdjudicacionFolderFeature.Application.Dtos;
using Abril_Backend.Features.CostsModule.Features.Configuration.AdjudicacionFolderFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.CostsModule.Features.Configuration.AdjudicacionFolderFeature.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.CostsModule.Features.Configuration.AdjudicacionFolderFeature.Infrastructure.Repositories
{
    public class AdjudicacionFolderRepository : IAdjudicacionFolderRepository
    {
        private readonly AppDbContext _context;

        public AdjudicacionFolderRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<AdjudicacionFolderDto>> GetPaged(AdjudicacionFolderFilterDto filter)
        {
            const int pageSize = 10;

            var query = _context.ProjectAdjudicacionFolder.Where(x => x.State);

            if (filter.ProjectId.HasValue)
                query = query.Where(x => x.ProjectId == filter.ProjectId.Value);

            var totalRecords = await query.CountAsync();

            var data = await (
                from f in query
                join p in _context.Project on f.ProjectId equals p.ProjectId
                join t in _context.ProjectAdjudicacionFolderType on f.FolderTypeId equals t.ProjectAdjudicacionFolderTypeId
                orderby f.ProjectAdjudicacionFolderId descending
                select new AdjudicacionFolderDto
                {
                    ProjectAdjudicacionFolderId = f.ProjectAdjudicacionFolderId,
                    ProjectId = f.ProjectId,
                    ProjectDescription = p.ProjectDescription,
                    FolderTypeId = f.FolderTypeId,
                    FolderTypeDescription = t.ProjectAdjudicacionFolderTypeDescription,
                    LinkUrl = f.LinkUrl,
                    DriveId = f.DriveId,
                    FolderId = f.FolderId,
                    FolderName = f.FolderName,
                    WebUrl = f.WebUrl,
                    Active = f.Active,
                    CreatedDateTime = f.CreatedDateTime.ToOffset(TimeSpan.FromHours(-5)).DateTime,
                    CreatedUserId = f.CreatedUserId
                })
                .Skip((filter.Page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<AdjudicacionFolderDto>
            {
                Page = filter.Page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Data = data
            };
        }

        public async Task<AdjudicacionFolderFormDataDto> GetFormData()
        {
            var projects = await _context.Project
                .Where(x => x.State && x.Active)
                .OrderBy(x => x.ProjectDescription)
                .Select(x => new ProjectSimpleDto
                {
                    ProjectId = x.ProjectId,
                    ProjectDescription = x.ProjectDescription
                })
                .ToListAsync();

            var folderTypes = await _context.ProjectAdjudicacionFolderType
                .Where(x => x.State && x.Active)
                .OrderBy(x => x.ProjectAdjudicacionFolderTypeId)
                .Select(x => new FolderTypeSimpleDto
                {
                    FolderTypeId = x.ProjectAdjudicacionFolderTypeId,
                    FolderTypeDescription = x.ProjectAdjudicacionFolderTypeDescription
                })
                .ToListAsync();

            return new AdjudicacionFolderFormDataDto { Projects = projects, FolderTypes = folderTypes };
        }

        public async Task<bool> ExistsForProjectAsync(int projectId, int folderTypeId)
        {
            return await _context.ProjectAdjudicacionFolder
                .AnyAsync(x => x.ProjectId == projectId && x.FolderTypeId == folderTypeId && x.State);
        }

        public async Task<bool> FolderTypeExistsAsync(int folderTypeId)
        {
            return await _context.ProjectAdjudicacionFolderType
                .AnyAsync(x => x.ProjectAdjudicacionFolderTypeId == folderTypeId && x.State && x.Active);
        }

        public async Task<int?> GetProjectIdAsync(int projectAdjudicacionFolderId)
        {
            return await _context.ProjectAdjudicacionFolder
                .Where(x => x.ProjectAdjudicacionFolderId == projectAdjudicacionFolderId && x.State)
                .Select(x => (int?)x.ProjectId)
                .FirstOrDefaultAsync();
        }

        public async Task Create(AdjudicacionFolderCreateDto dto, string driveId, string folderId, string? folderName, string? webUrl, int userId)
        {
            var record = new ProjectAdjudicacionFolder
            {
                ProjectId = dto.ProjectId,
                FolderTypeId = dto.FolderTypeId,
                LinkUrl = dto.LinkUrl.Trim(),
                DriveId = driveId,
                FolderId = folderId,
                FolderName = folderName,
                WebUrl = webUrl,
                Active = true,
                State = true,
                CreatedDateTime = DateTimeOffset.UtcNow,
                CreatedUserId = userId
            };

            _context.ProjectAdjudicacionFolder.Add(record);
            await _context.SaveChangesAsync();
        }

        public async Task Update(AdjudicacionFolderUpdateDto dto, string driveId, string folderId, string? folderName, string? webUrl, int userId)
        {
            var record = await _context.ProjectAdjudicacionFolder
                .FirstOrDefaultAsync(x => x.ProjectAdjudicacionFolderId == dto.ProjectAdjudicacionFolderId && x.State)
                ?? throw new AbrilException("La carpeta no existe.");

            record.LinkUrl = dto.LinkUrl.Trim();
            record.DriveId = driveId;
            record.FolderId = folderId;
            record.FolderName = folderName;
            record.WebUrl = webUrl;
            record.Active = dto.Active;
            record.UpdatedDateTime = DateTimeOffset.UtcNow;
            record.UpdatedUserId = userId;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> Delete(int projectAdjudicacionFolderId, int userId)
        {
            var record = await _context.ProjectAdjudicacionFolder
                .FirstOrDefaultAsync(x => x.ProjectAdjudicacionFolderId == projectAdjudicacionFolderId && x.State);

            if (record == null) return false;

            record.State = false;
            record.Active = false;
            record.UpdatedDateTime = DateTimeOffset.UtcNow;
            record.UpdatedUserId = userId;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}

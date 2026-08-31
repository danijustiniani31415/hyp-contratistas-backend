using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Features.CostsModule.Features.Configuration.ProjectLinkFeature.Application.Dtos;
using Abril_Backend.Features.CostsModule.Features.Configuration.ProjectLinkFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.CostsModule.Features.Configuration.ProjectLinkFeature.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.CostsModule.Features.Configuration.ProjectLinkFeature.Infrastructure.Repositories
{
    public class ProjectLinkRepository : IProjectLinkRepository
    {
        private readonly AppDbContext _context;

        public ProjectLinkRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<ProjectLinkDto>> GetPaged(ProjectLinkFilterDto filter, List<int>? allowedProjectIds = null)
        {
            const int pageSize = 10;

            var query = _context.ProjectLink.Where(x => x.State);

            if (allowedProjectIds != null)
                query = query.Where(x => allowedProjectIds.Contains(x.ProjectId));

            if (filter.ProjectId.HasValue)
                query = query.Where(x => x.ProjectId == filter.ProjectId.Value);

            var totalRecords = await query.CountAsync();

            var data = await (
                from pl in query
                join p in _context.Project on pl.ProjectId equals p.ProjectId
                join t in _context.ProjectLinkType on pl.ProjectLinkTypeId equals t.ProjectLinkTypeId
                orderby pl.ProjectLinkId descending
                select new ProjectLinkDto
                {
                    ProjectLinkId = pl.ProjectLinkId,
                    ProjectId = pl.ProjectId,
                    ProjectDescription = p.ProjectDescription,
                    ProjectLinkTypeId = pl.ProjectLinkTypeId,
                    ProjectLinkTypeDescription = t.ProjectLinkTypeDescription,
                    LinkUrl = pl.LinkUrl,
                    Active = pl.Active,
                    CreatedDateTime = pl.CreatedDateTime.ToOffset(TimeSpan.FromHours(-5)).DateTime,
                    CreatedUserId = pl.CreatedUserId
                })
                .Skip((filter.Page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<ProjectLinkDto>
            {
                Page = filter.Page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Data = data
            };
        }

        public async Task<ProjectLinkFormDataDto> GetFormData(List<int>? allowedProjectIds = null)
        {
            var projectsQuery = _context.Project
                .Where(x => x.State && x.Active);

            if (allowedProjectIds != null)
                projectsQuery = projectsQuery.Where(x => allowedProjectIds.Contains(x.ProjectId));

            var projects = await projectsQuery
                .OrderBy(x => x.ProjectDescription)
                .Select(x => new ProjectSimpleDto
                {
                    ProjectId = x.ProjectId,
                    ProjectDescription = x.ProjectDescription
                })
                .ToListAsync();

            var types = await _context.ProjectLinkType
                .Where(x => x.State)
                .OrderBy(x => x.ProjectLinkTypeId)
                .Select(x => new ProjectLinkTypeDto
                {
                    ProjectLinkTypeId = x.ProjectLinkTypeId,
                    ProjectLinkTypeDescription = x.ProjectLinkTypeDescription
                })
                .ToListAsync();

            return new ProjectLinkFormDataDto
            {
                Projects = projects,
                Types = types
            };
        }

        public async Task<List<int>> GetUserProjectIdsAsync(int userId)
        {
            // Proyectos del usuario según los correos registrados en staff_project_email:
            // el correo registrado identifica al trabajador por workers.email_corporativo
            // (ahí se guarda el correo @abril.pe, que es el mismo de app_user.email).
            var userEmail = await _context.User
                .Where(u => u.UserId == userId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(userEmail))
                return new List<int>();

            var email = userEmail.Trim().ToLower();

            return await (
                from spe in _context.StaffProjectEmail
                join w in _context.Worker on spe.Email.ToLower() equals (w.EmailCorporativo ?? string.Empty).ToLower()
                where spe.State && spe.Active
                   && (w.EmailCorporativo ?? string.Empty).ToLower() == email
                select spe.ProjectId
            ).Distinct().ToListAsync();
        }

        public async Task<int?> GetProjectIdAsync(int projectLinkId)
        {
            return await _context.ProjectLink
                .Where(x => x.ProjectLinkId == projectLinkId && x.State)
                .Select(x => (int?)x.ProjectId)
                .FirstOrDefaultAsync();
        }

        public async Task<List<ProjectLink>> GetByProjectIdAsync(int projectId)
        {
            return await _context.ProjectLink
                .Where(x => x.ProjectId == projectId && x.State)
                .ToListAsync();
        }

        public async Task Create(ProjectLinkCreateDto dto, int userId)
        {
            var exists = await _context.ProjectLink
                .AnyAsync(x => x.ProjectId == dto.ProjectId
                             && x.ProjectLinkTypeId == dto.ProjectLinkTypeId
                             && x.State);

            if (exists)
                throw new AbrilException("Este proyecto ya tiene un link registrado para ese tipo.");

            var record = new ProjectLink
            {
                ProjectId = dto.ProjectId,
                ProjectLinkTypeId = dto.ProjectLinkTypeId,
                LinkUrl = dto.LinkUrl.Trim(),
                Active = true,
                State = true,
                CreatedDateTime = DateTimeOffset.UtcNow,
                CreatedUserId = userId
            };

            _context.ProjectLink.Add(record);
            await _context.SaveChangesAsync();
        }

        public async Task Update(ProjectLinkUpdateDto dto, int userId)
        {
            var record = await _context.ProjectLink
                .FirstOrDefaultAsync(x => x.ProjectLinkId == dto.ProjectLinkId && x.State);

            if (record == null)
                throw new AbrilException("El link no existe.");

            record.LinkUrl = dto.LinkUrl.Trim();
            record.Active = dto.Active;
            record.UpdatedDateTime = DateTimeOffset.UtcNow;
            record.UpdatedUserId = userId;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> Delete(int projectLinkId, int userId)
        {
            var record = await _context.ProjectLink
                .FirstOrDefaultAsync(x => x.ProjectLinkId == projectLinkId && x.State);

            if (record == null)
                return false;

            record.State = false;
            record.Active = false;
            record.UpdatedDateTime = DateTimeOffset.UtcNow;
            record.UpdatedUserId = userId;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}

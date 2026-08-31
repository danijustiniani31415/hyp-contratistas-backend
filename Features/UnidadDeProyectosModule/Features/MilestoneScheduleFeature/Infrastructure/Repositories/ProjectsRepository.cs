using Microsoft.EntityFrameworkCore;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Infrastructure.Repositories
{
    public class ProjectsRepository : IProjectsRepository
    {
        private readonly AppDbContext _context;

        public ProjectsRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<ProjectDTO>> GetPagedWithResidents(int page, int pageSize = 10, string? search = null)
        {
            var projectQuery = _context.Project
                .Where(p => p.Active && p.State && p.TieneUnidadDeProyectos
                    && (search == null || p.ProjectDescription.ToLower().Contains(search.ToLower())))
                .OrderByDescending(p => p.ProjectId);

            var totalRecords = await projectQuery.CountAsync();

            var projects = await projectQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.ProjectId,
                    p.ProjectDescription,
                    p.LevelDescription,
                    p.FotoUrl,
                    p.CreatedDateTime,
                    p.CreatedUserId,
                    p.UpdatedDateTime,
                    p.UpdatedUserId,
                    p.Active
                })
                .ToListAsync();

            var projectIds = projects.Select(p => p.ProjectId).ToList();

            var residents = await (
                from pr in _context.ProjectResident
                join u  in _context.User   on pr.UserId equals u.UserId
                join pe in _context.Person on u.UserId  equals pe.UserId
                where projectIds.Contains(pr.ProjectId) && pr.Active && pr.State
                select new { pr.ProjectId, pe.FullName }
            ).ToListAsync();

            var residentsByProject = residents
                .GroupBy(r => r.ProjectId)
                .ToDictionary(g => g.Key, g => g.Select(r => r.FullName).ToList());

            var data = projects.Select(p => new ProjectDTO
            {
                ProjectId          = p.ProjectId,
                ProjectDescription = p.ProjectDescription,
                LevelDescription   = p.LevelDescription,
                FotoUrl            = p.FotoUrl,
                ResidentFullNames  = residentsByProject.GetValueOrDefault(p.ProjectId, new()),
                CreatedDateTime    = p.CreatedDateTime,
                CreatedUserId      = p.CreatedUserId,
                UpdatedDateTime    = p.UpdatedDateTime,
                UpdatedUserId      = p.UpdatedUserId,
                Active             = p.Active
            }).ToList();

            return new PagedResult<ProjectDTO>
            {
                Page         = page,
                PageSize     = pageSize,
                TotalRecords = totalRecords,
                TotalPages   = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Data         = data
            };
        }

        public async Task UpdateFotoUrlAsync(int projectId, string? fotoUrl)
        {
            var project = await _context.Project
                .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.State);
            if (project == null)
                throw new AbrilException("Proyecto no encontrado.", 404);

            project.FotoUrl = fotoUrl;
            project.UpdatedDateTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}

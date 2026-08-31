using Abril_Backend.Shared.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Features.ConfigurationModule.Features.ProjectFeature.Application.Dtos;

namespace Abril_Backend.Infrastructure.Repositories {
    public class ProjectRepository : IProjectRepository {
        private readonly AppDbContext _context;
        private readonly IDbContextFactory<AppDbContext> _factory;

        public ProjectRepository(AppDbContext contexto, IDbContextFactory<AppDbContext> factory) {
            _context = contexto;
            _factory = factory;
        }

        public async Task<List<ProjectDTO>> GetAll()
        {
            var rows = await _context.Project
                .Where(item => item.Active && item.State)
                .OrderBy(item => item.ProjectDescription)
                .Select(item => new
                {
                    item.ProjectId,
                    item.ProjectDescription,
                    item.LevelDescription,
                    item.CreatedDateTime,
                    item.CreatedUserId,
                    item.UpdatedDateTime,
                    item.UpdatedUserId,
                    item.Active
                })
                .ToListAsync();

            return rows.Select(item => new ProjectDTO
            {
                ProjectId          = item.ProjectId,
                ProjectDescription = item.ProjectDescription,
                LevelDescription   = item.LevelDescription,
                CreatedDateTime    = item.CreatedDateTime,
                CreatedUserId      = item.CreatedUserId,
                UpdatedDateTime    = item.UpdatedDateTime,
                UpdatedUserId      = item.UpdatedUserId,
                Active             = item.Active
            }).ToList();
        }

        public async Task<List<ProjectSimpleDTO>> GetAllFactory()
        {
            using var ctx = _factory.CreateDbContext();
            var rows = await ctx.Project
                .Where(item => item.Active && item.State)
                .OrderBy(item => item.ProjectDescription)
                .Select(item => new
                {
                    item.ProjectId,
                    item.ProjectDescription
                })
                .ToListAsync();

            return rows.Select(item => new ProjectSimpleDTO
            {
                ProjectId          = item.ProjectId,
                ProjectDescription = item.ProjectDescription,
            }).ToList();
        }

        public async Task<string> GetProjectNameByProjectId(int projectId)
        {
            var projectName = await (
                from project in _context.Project
                where project.ProjectId == projectId
                      && project.Active && project.State
                select project.ProjectDescription
            ).FirstOrDefaultAsync();

            return projectName ?? string.Empty;
        }
    }
}

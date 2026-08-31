using Abril_Backend.Shared.Constants;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Infrastructure.Interfaces;

namespace Abril_Backend.Infrastructure.Repositories {
    public class ProjectResidentRepository : IProjectResidentRepository {
        private readonly AppDbContext _context;
        private readonly IDbContextFactory<AppDbContext> _factory;
        public ProjectResidentRepository(AppDbContext contexto, IDbContextFactory<AppDbContext> factory) {
            _context = contexto;
            _factory = factory;
        }

        public async Task<List<ProjectSimpleDTO>> GetProjectsDescription()
        {
            using var ctx = _factory.CreateDbContext();

            var registros = from project_resident in ctx.ProjectResident
                join project in ctx.Project on project_resident.ProjectId equals project.ProjectId
                where (project_resident.State == true) && (project_resident.Active == true) && (project.Active == true)
                    && !ctx.ProyectoFiltro.Any(f => f.ProjectId == project.ProjectId && f.FuncionalidadId == ProyectoFiltroFuncionalidades.Residentes && !f.Active)
                orderby project.ProjectDescription
                select new ProjectSimpleDTO
                {
                    ProjectId = project.ProjectId,
                    ProjectDescription = project.ProjectDescription ?? string.Empty
                };
            return await registros.ToListAsync();
        }

        public async Task<List<ProjectSimpleDTO>> GetProjectByResidentUserId(int userId)
        {
            var registros = from pj in _context.Project
                join up in _context.ProjectResident on pj.ProjectId equals up.ProjectId
                where (up.UserId == userId)
                && (pj.Active == true)
                && !_context.ProyectoFiltro.Any(f => f.ProjectId == pj.ProjectId && f.FuncionalidadId == ProyectoFiltroFuncionalidades.Residentes && !f.Active)
                select new ProjectSimpleDTO
                {
                    ProjectId = pj.ProjectId,
                    ProjectDescription = pj.ProjectDescription ?? string.Empty,
                };
            return await registros.ToListAsync();
        }

        public async Task<bool> IsUserAssignedToProject(int userId, int projectId)
        {
            return await _context.ProjectResident.AnyAsync(pr =>
                pr.UserId == userId
                && pr.ProjectId == projectId
                && pr.Active
                && pr.State);
        }

        public async Task<List<ProjectSimpleDTO>> GetActiveProjectsForResident(int userId)
        {
            var registros = from pj in _context.Project
                join pr in _context.ProjectResident on pj.ProjectId equals pr.ProjectId
                where pr.UserId == userId
                && pr.Active
                && pr.State
                && pj.Active
                && !_context.ProyectoFiltro.Any(f => f.ProjectId == pj.ProjectId && f.FuncionalidadId == ProyectoFiltroFuncionalidades.Residentes && !f.Active)
                select new ProjectSimpleDTO
                {
                    ProjectId = pj.ProjectId,
                    ProjectDescription = pj.ProjectDescription ?? string.Empty,
                };
            return await registros.ToListAsync();
        }
    }
}
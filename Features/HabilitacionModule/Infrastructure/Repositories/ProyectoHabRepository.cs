using Abril_Backend.Shared.Constants;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Proyectos;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Repositories
{
    public class ProyectoHabRepository : IProyectoHabRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public ProyectoHabRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<ProyectoSimpleDto>> GetActivosAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Project
                .Where(p => p.State && p.Active && !ctx.ProyectoFiltro.Any(f => f.ProjectId == p.ProjectId && f.FuncionalidadId == ProyectoFiltroFuncionalidades.Habilitacion && !f.Active))
                .OrderBy(p => p.ProjectDescription)
                .Select(p => new ProyectoSimpleDto
                {
                    Id = p.ProjectId,
                    Nombre = p.ProjectDescription
                })
                .ToListAsync();
        }

        public async Task<List<ProyectoSimpleDto>> GetActivosInduccionAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Project
                .Where(p => p.State && p.Active && !ctx.ProyectoFiltro.Any(f => f.ProjectId == p.ProjectId && f.FuncionalidadId == ProyectoFiltroFuncionalidades.HabilitacionInduccion && !f.Active))
                .OrderBy(p => p.ProjectDescription)
                .Select(p => new ProyectoSimpleDto
                {
                    Id = p.ProjectId,
                    Nombre = p.ProjectDescription
                })
                .ToListAsync();
        }
    }
}

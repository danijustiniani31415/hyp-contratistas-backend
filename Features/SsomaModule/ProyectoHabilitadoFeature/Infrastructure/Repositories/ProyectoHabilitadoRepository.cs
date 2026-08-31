using Abril_Backend.Features.SsomaModule.ProyectoHabilitadoFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.ProyectoHabilitadoFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.SsomaModule.ProyectoHabilitadoFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.SsomaModule.ProyectoHabilitadoFeature.Infrastructure.Repositories
{
    public class ProyectoHabilitadoRepository : IProyectoHabilitadoRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public ProyectoHabilitadoRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<ProyectoHabilitadoListDto>> GetTodosAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Project
                .OrderBy(p => p.ProjectDescription)
                .Select(p => new ProyectoHabilitadoListDto
                {
                    ProyectoId = p.ProjectId,
                    ProyectoDescription = p.ProjectDescription,
                    Habilitado = ctx.SsProyectoHabilitado
                        .Any(h => h.ProyectoId == p.ProjectId && h.State && h.Active),
                    ProyectoActivo = p.Active,
                })
                .ToListAsync();
        }

        public async Task<List<ProyectoSsomaSimpleDto>> GetHabilitadosAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsProyectoHabilitado
                .Where(h => h.State && h.Active)
                .OrderBy(h => h.Proyecto.ProjectDescription)
                .Select(h => new ProyectoSsomaSimpleDto
                {
                    ProjectId = h.ProyectoId,
                    ProjectDescription = h.Proyecto.ProjectDescription,
                })
                .ToListAsync();
        }

        public async Task SetHabilitadoAsync(int proyectoId, bool habilitado, int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var existente = await ctx.SsProyectoHabilitado
                .FirstOrDefaultAsync(h => h.ProyectoId == proyectoId && h.State);

            if (existente == null)
            {
                ctx.SsProyectoHabilitado.Add(new SsProyectoHabilitado
                {
                    ProyectoId = proyectoId,
                    Active = habilitado,
                    CreatedDateTime = DateTimeOffset.UtcNow,
                    CreatedUserId = userId,
                    State = true,
                });
            }
            else
            {
                existente.Active = habilitado;
                existente.UpdatedDateTime = DateTimeOffset.UtcNow;
                existente.UpdatedUserId = userId;
            }

            await ctx.SaveChangesAsync();
        }
    }
}

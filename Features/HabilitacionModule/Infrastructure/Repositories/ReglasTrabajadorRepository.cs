using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Repositories
{
    public class ReglasTrabajadorRepository : IReglasTrabajadorRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public ReglasTrabajadorRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<SsItemTrabajadorRegla>> GetAllAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsItemTrabajadorRegla
                .Include(r => r.Item)
                .OrderBy(r => r.ItemId)
                .ToListAsync();
        }

        public async Task<SsItemTrabajadorRegla> CreateAsync(SsItemTrabajadorRegla regla)
        {
            using var ctx = _factory.CreateDbContext();
            regla.CreatedAt = DateTime.UtcNow;
            regla.UpdatedAt = DateTime.UtcNow;
            ctx.SsItemTrabajadorRegla.Add(regla);
            await ctx.SaveChangesAsync();
            return regla;
        }

        public async Task<SsItemTrabajadorRegla> UpdateAsync(SsItemTrabajadorRegla regla)
        {
            using var ctx = _factory.CreateDbContext();
            regla.UpdatedAt = DateTime.UtcNow;
            ctx.SsItemTrabajadorRegla.Update(regla);
            await ctx.SaveChangesAsync();
            return regla;
        }

        public async Task DeleteAsync(int id)
        {
            using var ctx = _factory.CreateDbContext();
            var regla = await ctx.SsItemTrabajadorRegla.FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new AbrilException("Regla no encontrada.", 404);
            ctx.SsItemTrabajadorRegla.Remove(regla);
            await ctx.SaveChangesAsync();
        }
    }
}

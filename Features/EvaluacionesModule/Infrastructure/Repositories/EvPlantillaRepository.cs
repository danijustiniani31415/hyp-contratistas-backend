using Abril_Backend.Features.Evaluaciones.Application.Interfaces;
using Abril_Backend.Features.Evaluaciones.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Repositories
{
    public class EvPlantillaRepository : IEvPlantillaRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public EvPlantillaRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<EvPlantilla>> GetAllActivasAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.EvPlantillas
                .Where(p => p.Activo)
                .OrderBy(p => p.AreaNombre)
                .ThenBy(p => p.Orden)
                .ToListAsync();
        }

        public async Task<List<EvPlantilla>> GetByAreaAsync(string area)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.EvPlantillas
                .Where(p => p.Activo && p.AreaNombre == area)
                .OrderBy(p => p.Orden)
                .ToListAsync();
        }

        public async Task<List<string>> GetAreasAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.EvPlantillas
                .Where(p => p.Activo)
                .Select(p => p.AreaNombre)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();
        }

        public async Task<EvPlantilla?> GetByIdAsync(int id)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.EvPlantillas.FindAsync(id);
        }

        public async Task<EvPlantilla> CreateAsync(EvPlantilla plantilla)
        {
            using var ctx = _factory.CreateDbContext();
            ctx.EvPlantillas.Add(plantilla);
            await ctx.SaveChangesAsync();
            return plantilla;
        }

        public async Task UpdateAsync(EvPlantilla plantilla)
        {
            using var ctx = _factory.CreateDbContext();
            plantilla.UpdatedAt = DateTime.UtcNow;
            ctx.EvPlantillas.Update(plantilla);
            await ctx.SaveChangesAsync();
        }
    }
}

using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Models;
using Abril_Backend.Shared.Services.Decolecta.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Shared.Services.Decolecta.Services
{
    public class DecolectaTokenStore : IDecolectaTokenStore
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public DecolectaTokenStore(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<DecolectaToken>> GetDisponiblesAsync()
        {
            using var ctx = _factory.CreateDbContext();
            // La fecha de expiración es una fecha calendario de Perú (UTC-5).
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
            return await ctx.DecolectaToken
                .AsNoTracking()
                .Where(t => t.State && !t.Agotado && t.FechaExpiracion >= hoy)
                .OrderBy(t => t.FechaExpiracion)
                .ToListAsync();
        }

        public async Task MarcarAgotadoAsync(int id)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.DecolectaToken
                .Where(t => t.Id == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.Agotado, true)
                    .SetProperty(t => t.UpdatedDateTime, DateTime.UtcNow));
        }

        public async Task<int> RenovarCuotaMensualAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.DecolectaToken
                .Where(t => t.State && t.Agotado)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.Agotado, false)
                    .SetProperty(t => t.UpdatedDateTime, DateTime.UtcNow));
        }
    }
}

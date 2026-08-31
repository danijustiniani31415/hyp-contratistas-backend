using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Infrastructure.Repositories {
    public class AreaRepository {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public AreaRepository(IDbContextFactory<AppDbContext> factory) {
            _factory = factory;
        }

        public async Task<List<AreaSimpleDTO>> GetAllFactory()
        {
            using var ctx = _factory.CreateDbContext();
            var registros = ctx.Area
                .Where(item => item.State)
                .OrderBy(item => item.AreaDescription)
                .Select(item => new AreaSimpleDTO
                {
                    AreaId = item.AreaId,
                    AreaDescription = item.AreaDescription,
                });
            return await registros.ToListAsync();
        }
    }
}
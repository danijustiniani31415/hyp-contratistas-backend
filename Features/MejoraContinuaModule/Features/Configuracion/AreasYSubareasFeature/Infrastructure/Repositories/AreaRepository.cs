using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Application.Dtos;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Infrastructure.Repositories
{
    public class AreaRepository : IAreaRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public AreaRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<AreaSimpleDTO>> GetAllSimple()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Area
                .Where(a => a.State)
                .OrderBy(a => a.AreaDescription)
                .Select(a => new AreaSimpleDTO { AreaId = a.AreaId, AreaDescription = a.AreaDescription })
                .ToListAsync();
        }

        public async Task<object> GetPaged(int page)
        {
            const int pageSize = 10;
            using var ctx = _factory.CreateDbContext();

            var query = from area in ctx.Area
                        where area.State == true
                        orderby area.AreaId descending
                        select new AreaDTO
                        {
                            AreaId = area.AreaId,
                            AreaDescription = area.AreaDescription,
                            CreatedDateTime = area.CreatedDateTime,
                            CreatedUserId = area.CreatedUserId,
                            UpdatedDateTime = area.UpdatedDateTime,
                            UpdatedUserId = area.UpdatedUserId,
                            Active = area.Active
                        };

            var totalRecords = await query.CountAsync();
            var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new
            {
                page,
                pageSize,
                totalRecords,
                totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                data
            };
        }

        public async Task CreateAsync(AreaCreateDTO dto, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var area = await ctx.Area.FirstOrDefaultAsync(a => a.AreaDescription == dto.AreaDescription.Trim());

            if (area != null && area.State)
                throw new AbrilException("El área ya existe");

            if (area != null && !area.State)
            {
                area.State = true;
                area.Active = dto.Active;
                area.UpdatedDateTime = DateTimeOffset.UtcNow;
                area.UpdatedUserId = userId;
                await ctx.SaveChangesAsync();
                return;
            }

            area = new Area
            {
                AreaDescription = dto.AreaDescription.Trim(),
                Active = dto.Active,
                State = true,
                CreatedDateTime = DateTimeOffset.UtcNow,
                CreatedUserId = userId
            };

            ctx.Area.Add(area);
            await ctx.SaveChangesAsync();
        }

        public async Task UpdateAsync(AreaEditDTO dto, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var area = await ctx.Area.FirstOrDefaultAsync(p => p.AreaId == dto.AreaId);

            if (area == null)
                throw new AbrilException("El area no existe");

            var duplicate = await ctx.Area.FirstOrDefaultAsync(p =>
                p.AreaDescription == dto.AreaDescription.Trim() &&
                p.AreaId != dto.AreaId &&
                p.State
            );

            if (duplicate != null)
                throw new AbrilException("Ya existe otra area con la misma descripción");

            area.AreaDescription = dto.AreaDescription.Trim();
            area.Active = dto.Active;
            area.UpdatedDateTime = DateTimeOffset.UtcNow;
            area.UpdatedUserId = userId;

            await ctx.SaveChangesAsync();
        }

        public async Task<bool> DeleteSoftAsync(int areaId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var area = await ctx.Area.FirstOrDefaultAsync(u => u.AreaId == areaId && u.State == true);

            if (area == null)
                return false;

            area.State = false;
            area.Active = false;
            area.UpdatedDateTime = DateTimeOffset.UtcNow;
            area.UpdatedUserId = userId;

            await ctx.SaveChangesAsync();
            return true;
        }
    }
}

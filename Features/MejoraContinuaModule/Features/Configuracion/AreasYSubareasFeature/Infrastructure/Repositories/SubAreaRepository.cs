using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Application.Dtos;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Infrastructure.Repositories
{
    public class SubAreaRepository : ISubAreaRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public SubAreaRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<object> GetPaged(int page, int? areaId)
        {
            const int pageSize = 10;
            using var ctx = _factory.CreateDbContext();

            var query = from sa in ctx.SubArea
                        join a in ctx.Area on sa.AreaId equals a.AreaId
                        where sa.State == true && (areaId == null || sa.AreaId == areaId)
                        orderby sa.SubAreaId descending
                        select new SubAreaDTO
                        {
                            SubAreaId = sa.SubAreaId,
                            AreaId = sa.AreaId,
                            AreaDescription = a.AreaDescription,
                            SubAreaDescription = sa.SubAreaDescription,
                            CreatedDateTime = sa.CreatedDateTime,
                            CreatedUserId = sa.CreatedUserId,
                            UpdatedDateTime = sa.UpdatedDateTime,
                            UpdatedUserId = sa.UpdatedUserId,
                            Active = sa.Active
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

        public async Task<List<SubAreaSimpleDTO>> GetSimpleByAreaAsync(int areaId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SubArea
                .Where(sa => sa.AreaId == areaId && sa.State && sa.Active)
                .OrderBy(sa => sa.SubAreaDescription)
                .Select(sa => new SubAreaSimpleDTO { SubAreaId = sa.SubAreaId, AreaId = sa.AreaId, SubAreaDescription = sa.SubAreaDescription })
                .ToListAsync();
        }

        public async Task<List<SubAreaSimpleDTO>> GetAllSimpleAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SubArea
                .Where(sa => sa.State && sa.Active)
                .OrderBy(sa => sa.SubAreaDescription)
                .Select(sa => new SubAreaSimpleDTO { SubAreaId = sa.SubAreaId, AreaId = sa.AreaId, SubAreaDescription = sa.SubAreaDescription })
                .ToListAsync();
        }

        public async Task<bool> AreaHasScopeAsync(int areaId)
        {
            // Modelo legacy: el scope ahora vive en scope_item.area_item_id (apunta a area_item),
            // y este Area legacy ya no se mapea directo. Devolvemos false para no bloquear deletes.
            await Task.CompletedTask;
            return false;
        }

        private async Task DeleteAreaScopeAsync(int areaId, AppDbContext ctx)
        {
            // Modelo legacy: el scope ya no se asocia a las tablas Area/SubArea.
            // Nada que borrar aquí.
            await Task.CompletedTask;
        }

        public async Task CreateAsync(SubAreaCreateDTO dto, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var areaExists = await ctx.Area.AnyAsync(a => a.AreaId == dto.AreaId && a.State);
            if (!areaExists)
                throw new AbrilException("El área seleccionada no existe");

            var duplicate = await ctx.SubArea.FirstOrDefaultAsync(sa =>
                sa.SubAreaDescription == dto.SubAreaDescription.Trim() &&
                sa.AreaId == dto.AreaId);

            if (duplicate != null && duplicate.State)
                throw new AbrilException("La subárea ya existe en esta área");

            // Si es la primera subárea del área, eliminar el scope del área
            var isFirstSubArea = !await ctx.SubArea.AnyAsync(sa => sa.AreaId == dto.AreaId && sa.State);
            if (isFirstSubArea)
                await DeleteAreaScopeAsync(dto.AreaId, ctx);

            if (duplicate != null && !duplicate.State)
            {
                duplicate.State = true;
                duplicate.Active = dto.Active;
                duplicate.UpdatedDateTime = DateTimeOffset.UtcNow;
                duplicate.UpdatedUserId = userId;
                await ctx.SaveChangesAsync();
                return;
            }

            ctx.SubArea.Add(new SubArea
            {
                AreaId = dto.AreaId,
                SubAreaDescription = dto.SubAreaDescription.Trim(),
                Active = dto.Active,
                State = true,
                CreatedDateTime = DateTimeOffset.UtcNow,
                CreatedUserId = userId
            });
            await ctx.SaveChangesAsync();
        }

        public async Task UpdateAsync(SubAreaEditDTO dto, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var subArea = await ctx.SubArea.FirstOrDefaultAsync(sa => sa.SubAreaId == dto.SubAreaId);
            if (subArea == null)
                throw new AbrilException("La subárea no existe");

            var duplicate = await ctx.SubArea.FirstOrDefaultAsync(sa =>
                sa.SubAreaDescription == dto.SubAreaDescription.Trim() &&
                sa.AreaId == dto.AreaId &&
                sa.SubAreaId != dto.SubAreaId &&
                sa.State);

            if (duplicate != null)
                throw new AbrilException("Ya existe otra subárea con la misma descripción en esta área");

            subArea.AreaId = dto.AreaId;
            subArea.SubAreaDescription = dto.SubAreaDescription.Trim();
            subArea.Active = dto.Active;
            subArea.UpdatedDateTime = DateTimeOffset.UtcNow;
            subArea.UpdatedUserId = userId;

            await ctx.SaveChangesAsync();
        }

        public async Task<bool> DeleteSoftAsync(int subAreaId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var subArea = await ctx.SubArea.FirstOrDefaultAsync(sa => sa.SubAreaId == subAreaId && sa.State);
            if (subArea == null)
                return false;

            subArea.State = false;
            subArea.Active = false;
            subArea.UpdatedDateTime = DateTimeOffset.UtcNow;
            subArea.UpdatedUserId = userId;

            await ctx.SaveChangesAsync();
            return true;
        }
    }
}

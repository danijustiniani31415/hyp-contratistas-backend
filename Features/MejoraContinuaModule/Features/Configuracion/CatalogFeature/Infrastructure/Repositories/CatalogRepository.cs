using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.CatalogFeature.Application.Dtos;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.CatalogFeature.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.CatalogFeature.Infrastructure.Repositories
{
    public class CatalogRepository : ICatalogRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public CatalogRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<CatalogTypeDTO>> GetAllTypesAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.CatalogType
                .Where(t => t.Active)
                .OrderBy(t => t.CatalogTypeName)
                .Select(t => new CatalogTypeDTO
                {
                    CatalogTypeId = t.CatalogTypeId,
                    CatalogTypeName = t.CatalogTypeName,
                    Active = t.Active
                })
                .ToListAsync();
        }

        public async Task<List<CatalogItemDTO>> GetItemsByTypeAsync(int catalogTypeId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.CatalogItem
                .Where(i => i.CatalogTypeId == catalogTypeId && i.Active)
                .OrderBy(i => i.CatalogItemDescription)
                .Select(i => new CatalogItemDTO
                {
                    CatalogItemId = i.CatalogItemId,
                    CatalogTypeId = i.CatalogTypeId,
                    CatalogItemDescription = i.CatalogItemDescription,
                    Active = i.Active
                })
                .ToListAsync();
        }

        public async Task<List<CatalogItemDTO>> GetFullTreeAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await (
                from item in ctx.CatalogItem
                join type in ctx.CatalogType on item.CatalogTypeId equals type.CatalogTypeId
                where item.Active
                orderby type.CatalogTypeName, item.CatalogItemDescription
                select new CatalogItemDTO
                {
                    CatalogItemId = item.CatalogItemId,
                    CatalogTypeId = item.CatalogTypeId,
                    CatalogTypeName = type.CatalogTypeName,
                    CatalogItemDescription = item.CatalogItemDescription,
                    Active = item.Active
                }
            ).ToListAsync();
        }

        public async Task UpdateTypeAsync(CatalogTypeEditDTO dto)
        {
            using var ctx = _factory.CreateDbContext();
            var type = await ctx.CatalogType.FirstOrDefaultAsync(t => t.CatalogTypeId == dto.CatalogTypeId);
            if (type == null) throw new AbrilException("El tipo de catálogo no existe.", 404);

            type.CatalogTypeName = dto.CatalogTypeName.Trim();
            type.Active = dto.Active;
            await ctx.SaveChangesAsync();
        }

        public async Task DeleteTypeAsync(int catalogTypeId)
        {
            using var ctx = _factory.CreateDbContext();
            var hasItems = await ctx.CatalogItem.AnyAsync(i => i.CatalogTypeId == catalogTypeId && i.Active);
            if (hasItems) throw new AbrilException("No se puede eliminar un tipo que tiene ítems activos.", 400);

            var type = await ctx.CatalogType.FirstOrDefaultAsync(t => t.CatalogTypeId == catalogTypeId);
            if (type == null) throw new AbrilException("El tipo de catálogo no existe.", 404);

            type.Active = false;
            await ctx.SaveChangesAsync();
        }

        public async Task CreateTypeAsync(CatalogTypeCreateDTO dto)
        {
            using var ctx = _factory.CreateDbContext();
            ctx.CatalogType.Add(new CatalogType
            {
                CatalogTypeName = dto.CatalogTypeName.Trim(),
                Active = dto.Active
            });
            await ctx.SaveChangesAsync();
        }

        public async Task CreateItemAsync(CatalogItemCreateDTO dto)
        {
            using var ctx = _factory.CreateDbContext();

            var typeExists = await ctx.CatalogType.AnyAsync(t => t.CatalogTypeId == dto.CatalogTypeId && t.Active);
            if (!typeExists)
                throw new AbrilException("El tipo de catálogo no existe.", 400);

            ctx.CatalogItem.Add(new CatalogItem
            {
                CatalogTypeId = dto.CatalogTypeId,
                CatalogItemDescription = dto.CatalogItemDescription.Trim(),
                Active = dto.Active
            });
            await ctx.SaveChangesAsync();
        }

        public async Task UpdateItemAsync(CatalogItemEditDTO dto)
        {
            using var ctx = _factory.CreateDbContext();

            var item = await ctx.CatalogItem.FirstOrDefaultAsync(i => i.CatalogItemId == dto.CatalogItemId);
            if (item == null)
                throw new AbrilException("El ítem no existe.", 404);

            item.CatalogTypeId = dto.CatalogTypeId;
            item.CatalogItemDescription = dto.CatalogItemDescription.Trim();
            item.Active = dto.Active;

            await ctx.SaveChangesAsync();
        }

        public async Task DeleteItemAsync(int catalogItemId)
        {
            using var ctx = _factory.CreateDbContext();

            var item = await ctx.CatalogItem.FirstOrDefaultAsync(i => i.CatalogItemId == catalogItemId);
            if (item == null)
                throw new AbrilException("El ítem no existe.", 404);

            item.Active = false;
            await ctx.SaveChangesAsync();
        }
    }
}

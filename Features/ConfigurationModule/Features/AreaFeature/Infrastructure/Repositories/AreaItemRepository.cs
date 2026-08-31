using Microsoft.EntityFrameworkCore;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Application.Dtos;
using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Infrastructure.Models;

namespace Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Infrastructure.Repositories
{
    public class AreaItemRepository : IAreaItemRepository
    {
        private readonly AppDbContext _context;

        public AreaItemRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<AreaItemDto>> GetPaged(AreaItemFilterDto filter)
        {
            var query =
                from i in _context.AreaItem
                join t in _context.AreaType on i.AreaTypeId equals t.AreaTypeId
                where i.State && t.State
                select new { i, t };

            if (filter.AreaTypeId.HasValue)
                query = query.Where(x => x.i.AreaTypeId == filter.AreaTypeId.Value);

            if (filter.Active.HasValue)
                query = query.Where(x => x.i.Active == filter.Active.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var s = filter.Search.Trim().ToLower();
                query = query.Where(x => x.i.AreaItemName.ToLower().Contains(s));
            }

            query = query.OrderBy(x => x.t.AreaTypeName).ThenBy(x => x.i.AreaItemName);

            var totalRecords = await query.CountAsync();

            var data = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => new AreaItemDto
                {
                    AreaItemId = x.i.AreaItemId,
                    AreaItemName = x.i.AreaItemName,
                    AreaTypeId = x.i.AreaTypeId,
                    AreaTypeName = x.t.AreaTypeName,
                    Active = x.i.Active,
                })
                .ToListAsync();

            return new PagedResult<AreaItemDto>
            {
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)filter.PageSize),
                Data = data
            };
        }

        public async Task<List<AreaItemSimpleDto>> GetSimple(int? areaTypeId)
        {
            var query = _context.AreaItem.Where(i => i.State && i.Active);
            if (areaTypeId.HasValue)
                query = query.Where(i => i.AreaTypeId == areaTypeId.Value);

            return await query
                .OrderBy(i => i.AreaItemName)
                .Select(i => new AreaItemSimpleDto
                {
                    AreaItemId = i.AreaItemId,
                    AreaItemName = i.AreaItemName,
                    AreaTypeId = i.AreaTypeId,
                })
                .ToListAsync();
        }

        public async Task Create(AreaItemCreateDto dto)
        {
            var name = dto.AreaItemName.Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new AbrilException("El nombre es obligatorio.");

            var typeExists = await _context.AreaType.AnyAsync(t => t.State && t.AreaTypeId == dto.AreaTypeId);
            if (!typeExists)
                throw new AbrilException("El tipo de área no existe.");

            // Duplicados solo cuentan entre registros vivos (state = true) del MISMO tipo de área.
            // Se permite el mismo nombre en tipos distintos.
            var duplicate = await _context.AreaItem.AnyAsync(i =>
                i.State && i.AreaTypeId == dto.AreaTypeId && i.AreaItemName.ToLower() == name.ToLower());
            if (duplicate)
                throw new AbrilException("Ya existe un área con ese nombre para este tipo de área.");

            _context.AreaItem.Add(new AreaItem
            {
                AreaItemName = name,
                AreaTypeId = dto.AreaTypeId,
                Active = dto.Active,
                State = true
            });
            await _context.SaveChangesAsync();
        }

        public async Task Update(AreaItemEditDto dto)
        {
            var entity = await _context.AreaItem.FirstOrDefaultAsync(i => i.State && i.AreaItemId == dto.AreaItemId);
            if (entity == null)
                throw new AbrilException("El área no existe.");

            var name = dto.AreaItemName.Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new AbrilException("El nombre es obligatorio.");

            var typeExists = await _context.AreaType.AnyAsync(t => t.State && t.AreaTypeId == dto.AreaTypeId);
            if (!typeExists)
                throw new AbrilException("El tipo de área no existe.");

            // Duplicado solo dentro del MISMO tipo de área (se permite el mismo nombre en tipos distintos).
            var duplicate = await _context.AreaItem.AnyAsync(i =>
                i.State &&
                i.AreaTypeId == dto.AreaTypeId &&
                i.AreaItemName.ToLower() == name.ToLower() &&
                i.AreaItemId != dto.AreaItemId);
            if (duplicate)
                throw new AbrilException("Ya existe otra área con ese nombre para este tipo de área.");

            entity.AreaItemName = name;
            entity.AreaTypeId = dto.AreaTypeId;
            entity.Active = dto.Active;
            await _context.SaveChangesAsync();
        }

        /// <summary>Soft delete: marca state = false (el registro se mantiene en BD para auditoría).</summary>
        public async Task<bool> DeleteSoftAsync(int areaItemId)
        {
            var entity = await _context.AreaItem.FirstOrDefaultAsync(i => i.State && i.AreaItemId == areaItemId);
            if (entity == null) return false;

            var inUseInScope = await _context.AreaScope.AnyAsync(s => s.State && s.AreaItemId == areaItemId);
            if (inUseInScope)
                throw new AbrilException("No se puede eliminar: el área está siendo usada en el árbol de áreas.");

            entity.State = false;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

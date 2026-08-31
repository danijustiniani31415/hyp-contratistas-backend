using Microsoft.EntityFrameworkCore;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Features.ConfigurationModule.Features.HolidayFeature.Application.Dtos;
using Abril_Backend.Features.ConfigurationModule.Features.HolidayFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.ConfigurationModule.Features.HolidayFeature.Infrastructure.Models;

namespace Abril_Backend.Features.ConfigurationModule.Features.HolidayFeature.Infrastructure.Repositories
{
    public class HolidayRepository : IHolidayRepository
    {
        private readonly AppDbContext _context;

        public HolidayRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<HolidayTypeSimpleDto>> GetTypes()
        {
            return await _context.HolidayType
                .Where(t => t.State && t.Active)
                .OrderBy(t => t.HolidayTypeName)
                .Select(t => new HolidayTypeSimpleDto
                {
                    HolidayTypeId = t.HolidayTypeId,
                    HolidayTypeName = t.HolidayTypeName,
                })
                .ToListAsync();
        }

        public async Task<PagedResult<HolidayDto>> GetPaged(int page, int pageSize)
        {
            var query = _context.Holiday
                .Where(h => h.State)
                .OrderBy(h => h.HolidayDate)
                .ThenBy(h => h.Description);

            var totalRecords = await query.CountAsync();

            var data = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(h => new HolidayDto
                {
                    HolidayId = h.HolidayId,
                    HolidayTypeId = h.HolidayTypeId,
                    HolidayTypeName = h.HolidayType!.HolidayTypeName,
                    HolidayDate = h.HolidayDate,
                    Description = h.Description,
                    RecurringYearly = h.RecurringYearly,
                    Active = h.Active,
                })
                .ToListAsync();

            return new PagedResult<HolidayDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Data = data
            };
        }

        public async Task Create(HolidayCreateDto dto)
        {
            var description = (dto.Description ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(description))
                throw new AbrilException("La descripción es obligatoria.");

            var typeExists = await _context.HolidayType.AnyAsync(t => t.State && t.HolidayTypeId == dto.HolidayTypeId);
            if (!typeExists)
                throw new AbrilException("El tipo seleccionado no existe.");

            var duplicate = await _context.Holiday.AnyAsync(h =>
                h.State && h.HolidayTypeId == dto.HolidayTypeId && h.HolidayDate == dto.HolidayDate);
            if (duplicate)
                throw new AbrilException("Ya existe un registro con esa fecha y tipo.");

            _context.Holiday.Add(new Holiday
            {
                HolidayTypeId = dto.HolidayTypeId,
                HolidayDate = dto.HolidayDate,
                Description = description,
                RecurringYearly = dto.RecurringYearly,
                Active = dto.Active,
                State = true,
                CreatedDateTime = DateTime.UtcNow,
            });
            await _context.SaveChangesAsync();
        }

        public async Task Update(HolidayEditDto dto)
        {
            var entity = await _context.Holiday.FirstOrDefaultAsync(h => h.State && h.HolidayId == dto.HolidayId);
            if (entity == null)
                throw new AbrilException("El registro no existe.");

            var description = (dto.Description ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(description))
                throw new AbrilException("La descripción es obligatoria.");

            var typeExists = await _context.HolidayType.AnyAsync(t => t.State && t.HolidayTypeId == dto.HolidayTypeId);
            if (!typeExists)
                throw new AbrilException("El tipo seleccionado no existe.");

            var duplicate = await _context.Holiday.AnyAsync(h =>
                h.State && h.HolidayTypeId == dto.HolidayTypeId &&
                h.HolidayDate == dto.HolidayDate && h.HolidayId != dto.HolidayId);
            if (duplicate)
                throw new AbrilException("Ya existe otro registro con esa fecha y tipo.");

            entity.HolidayTypeId = dto.HolidayTypeId;
            entity.HolidayDate = dto.HolidayDate;
            entity.Description = description;
            entity.RecurringYearly = dto.RecurringYearly;
            entity.Active = dto.Active;
            entity.UpdatedDateTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        /// <summary>Soft delete: marca state = false (el registro se mantiene en BD para auditoría).</summary>
        public async Task<bool> DeleteSoftAsync(int holidayId)
        {
            var entity = await _context.Holiday.FirstOrDefaultAsync(h => h.State && h.HolidayId == holidayId);
            if (entity == null) return false;

            entity.State = false;
            entity.UpdatedDateTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

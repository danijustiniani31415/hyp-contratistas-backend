using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkSpecialtyFeature.Application.Dtos;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkSpecialtyFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkSpecialtyFeature.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.CostsModule.Features.Configuration.WorkSpecialtyFeature.Infrastructure.Repositories
{
    public class WorkSpecialtyRepository : IWorkSpecialtyRepository
    {
        private readonly AppDbContext _context;

        public WorkSpecialtyRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<WorkSpecialtyDto>> GetPaged(WorkSpecialtyFilterDto filter)
        {
            const int pageSize = 10;

            var query = _context.WorkSpecialty.Where(x => x.State);

            // Búsqueda por palabras en cualquier orden, insensible a mayúsculas y tildes
            // (alineado con app-search-input del front).
            if (!string.IsNullOrWhiteSpace(filter.Description))
            {
                foreach (var word in filter.Description.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var pattern = $"%{word}%";
                    query = query.Where(x => EF.Functions.ILike(
                        AppDbContext.Unaccent(x.WorkSpecialtyDescription), AppDbContext.Unaccent(pattern)));
                }
            }

            if (filter.Active.HasValue)
                query = query.Where(x => x.Active == filter.Active.Value);

            var totalRecords = await query.CountAsync();

            var data = await query
                .OrderByDescending(x => x.WorkSpecialtyId)
                .Skip((filter.Page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new WorkSpecialtyDto
                {
                    WorkSpecialtyId = x.WorkSpecialtyId,
                    WorkSpecialtyDescription = x.WorkSpecialtyDescription,
                    CreatedDateTime = x.CreatedDateTime.ToOffset(TimeSpan.FromHours(-5)).DateTime,
                    CreatedUserId = x.CreatedUserId,
                    UpdatedDateTime = x.UpdatedDateTime.HasValue
                        ? x.UpdatedDateTime.Value.ToOffset(TimeSpan.FromHours(-5)).DateTime
                        : null,
                    UpdatedUserId = x.UpdatedUserId,
                    Active = x.Active
                })
                .ToListAsync();

            return new PagedResult<WorkSpecialtyDto>
            {
                Page = filter.Page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Data = data
            };
        }

        public async Task Create(WorkSpecialtyCreateDto dto, int userId)
        {
            var desc = dto.WorkSpecialtyDescription.Trim();
            var exists = await _context.WorkSpecialty
                .AnyAsync(x => x.WorkSpecialtyDescription.ToLower() == desc.ToLower() && x.State);

            if (exists)
                throw new AbrilException("Ya existe una especialidad con esa descripción.");

            _context.WorkSpecialty.Add(new WorkSpecialty
            {
                WorkSpecialtyDescription = desc,
                Active = true,
                State = true,
                CreatedDateTime = DateTimeOffset.UtcNow,
                CreatedUserId = userId
            });
            await _context.SaveChangesAsync();
        }

        public async Task Update(WorkSpecialtyEditDto dto, int userId)
        {
            var record = await _context.WorkSpecialty
                .FirstOrDefaultAsync(x => x.WorkSpecialtyId == dto.WorkSpecialtyId && x.State)
                ?? throw new AbrilException("La especialidad no existe.");

            var desc = dto.WorkSpecialtyDescription.Trim();
            var duplicate = await _context.WorkSpecialty
                .AnyAsync(x => x.WorkSpecialtyDescription.ToLower() == desc.ToLower()
                             && x.WorkSpecialtyId != dto.WorkSpecialtyId && x.State);

            if (duplicate)
                throw new AbrilException("Ya existe una especialidad con esa descripción.");

            record.WorkSpecialtyDescription = desc;
            record.Active = dto.Active;
            record.UpdatedDateTime = DateTimeOffset.UtcNow;
            record.UpdatedUserId = userId;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> Delete(int workSpecialtyId, int userId)
        {
            var record = await _context.WorkSpecialty
                .FirstOrDefaultAsync(x => x.WorkSpecialtyId == workSpecialtyId && x.State);

            if (record == null) return false;

            record.State = false;
            record.Active = false;
            record.UpdatedDateTime = DateTimeOffset.UtcNow;
            record.UpdatedUserId = userId;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

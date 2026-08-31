using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Features.CostsModule.Features.Configuration.CostosPresupuestosEmailFeature.Application.Dtos;
using Abril_Backend.Features.CostsModule.Features.Configuration.CostosPresupuestosEmailFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.CostsModule.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.CostsModule.Features.Configuration.CostosPresupuestosEmailFeature.Infrastructure.Repositories
{
    public class CostosPresupuestosEmailRepository : ICostosPresupuestosEmailRepository
    {
        private readonly AppDbContext _context;

        public CostosPresupuestosEmailRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<CostosPresupuestosEmailDto>> GetPaged(CostosPresupuestosEmailFilterDto filter)
        {
            const int pageSize = 10;

            var query = _context.CostosPresupuestosEmail.Where(x => x.State);

            // Búsqueda por palabras en cualquier orden, insensible a mayúsculas y tildes
            // (alineado con app-search-input del front).
            if (!string.IsNullOrWhiteSpace(filter.Email))
            {
                foreach (var word in filter.Email.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var pattern = $"%{word}%";
                    query = query.Where(x => EF.Functions.ILike(
                        AppDbContext.Unaccent(x.Email), AppDbContext.Unaccent(pattern)));
                }
            }

            var totalRecords = await query.CountAsync();

            var data = await query
                .OrderByDescending(x => x.CostosPresupuestosEmailId)
                .Skip((filter.Page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new CostosPresupuestosEmailDto
                {
                    CostosPresupuestosEmailId = x.CostosPresupuestosEmailId,
                    Email = x.Email,
                    CreatedDateTime = x.CreatedDateTime.ToOffset(TimeSpan.FromHours(-5)).DateTime,
                    CreatedUserId = x.CreatedUserId,
                    UpdatedDateTime = x.UpdatedDateTime.HasValue
                        ? x.UpdatedDateTime.Value.ToOffset(TimeSpan.FromHours(-5)).DateTime
                        : null,
                    UpdatedUserId = x.UpdatedUserId,
                    Active = x.Active
                })
                .ToListAsync();

            return new PagedResult<CostosPresupuestosEmailDto>
            {
                Page = filter.Page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Data = data
            };
        }

        public async Task<List<string>> GetActiveEmails()
        {
            return await _context.CostosPresupuestosEmail
                .Where(x => x.State && x.Active)
                .Select(x => x.Email)
                .ToListAsync();
        }

        public async Task Create(CostosPresupuestosEmailCreateDto dto, int userId)
        {
            var email = dto.Email.Trim().ToLower();

            var exists = await _context.CostosPresupuestosEmail
                .AnyAsync(x => x.Email.ToLower() == email && x.State);

            if (exists)
                throw new AbrilException("Ya existe un correo registrado con ese valor.");

            var record = new CostosPresupuestosEmail
            {
                Email = email,
                Active = true,
                State = true,
                CreatedDateTime = DateTimeOffset.UtcNow,
                CreatedUserId = userId
            };

            _context.CostosPresupuestosEmail.Add(record);
            await _context.SaveChangesAsync();
        }

        public async Task Update(CostosPresupuestosEmailEditDto dto, int userId)
        {
            var record = await _context.CostosPresupuestosEmail
                .FirstOrDefaultAsync(x => x.CostosPresupuestosEmailId == dto.CostosPresupuestosEmailId && x.State);

            if (record == null)
                throw new AbrilException("El correo no existe.");

            var email = dto.Email.Trim().ToLower();

            var duplicate = await _context.CostosPresupuestosEmail
                .AnyAsync(x => x.Email.ToLower() == email
                             && x.CostosPresupuestosEmailId != dto.CostosPresupuestosEmailId
                             && x.State);

            if (duplicate)
                throw new AbrilException("Ya existe un correo registrado con ese valor.");

            record.Email = email;
            record.Active = dto.Active;
            record.UpdatedDateTime = DateTimeOffset.UtcNow;
            record.UpdatedUserId = userId;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> Delete(int id, int userId)
        {
            var record = await _context.CostosPresupuestosEmail
                .FirstOrDefaultAsync(x => x.CostosPresupuestosEmailId == id && x.State);

            if (record == null)
                return false;

            record.State = false;
            record.Active = false;
            record.UpdatedDateTime = DateTimeOffset.UtcNow;
            record.UpdatedUserId = userId;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}

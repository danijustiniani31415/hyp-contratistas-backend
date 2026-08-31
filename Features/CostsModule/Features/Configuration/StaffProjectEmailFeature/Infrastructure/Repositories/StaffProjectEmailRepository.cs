using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Features.CostsModule.Features.Configuration.StaffProjectEmailFeature.Application.Dtos;
using Abril_Backend.Features.CostsModule.Features.Configuration.StaffProjectEmailFeature.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Abril_Backend.Features.CostsModule.Shared.Models;

namespace Abril_Backend.Features.CostsModule.Features.Configuration.StaffProjectEmailFeature.Infrastructure.Repositories
{
    public class StaffProjectEmailRepository : IStaffProjectEmailRepository
    {
        private readonly AppDbContext _context;

        public StaffProjectEmailRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<StaffProjectEmailTypeDto>> GetTypesFactory()
        {
            return await _context.StaffProjectEmailType
                .OrderBy(t => t.StaffProjectEmailTypeId)
                .Select(t => new StaffProjectEmailTypeDto
                {
                    StaffProjectEmailTypeId = t.StaffProjectEmailTypeId,
                    Description = t.Description,
                })
                .ToListAsync();
        }

        public async Task<PagedResult<StaffProjectEmailDto>> GetPaged(StaffProjectEmailFilterDto filter)
        {
            const int pageSize = 10;

            var query = from s in _context.StaffProjectEmail
                        join p in _context.Project on s.ProjectId equals p.ProjectId
                        join t in _context.StaffProjectEmailType on s.StaffProjectEmailTypeId equals t.StaffProjectEmailTypeId
                        where s.State
                        select new { s, p, t };

            if (filter.ProjectId.HasValue)
                query = query.Where(x => x.s.ProjectId == filter.ProjectId.Value);

            // Búsqueda por palabras en cualquier orden, insensible a mayúsculas y tildes
            // (alineado con app-search-input del front).
            if (!string.IsNullOrWhiteSpace(filter.Email))
            {
                foreach (var word in filter.Email.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var pattern = $"%{word}%";
                    query = query.Where(x => EF.Functions.ILike(
                        AppDbContext.Unaccent(x.s.Email), AppDbContext.Unaccent(pattern)));
                }
            }

            if (filter.StaffProjectEmailTypeId.HasValue)
                query = query.Where(x => x.s.StaffProjectEmailTypeId == filter.StaffProjectEmailTypeId.Value);

            var totalRecords = await query.CountAsync();

            var data = await query
                .OrderByDescending(x => x.s.StaffProjectEmailId)
                .Skip((filter.Page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new StaffProjectEmailDto
                {
                    StaffProjectEmailId              = x.s.StaffProjectEmailId,
                    ProjectId                        = x.s.ProjectId,
                    ProjectName                      = x.p.ProjectDescription,
                    Email                            = x.s.Email,
                    StaffProjectEmailTypeId          = x.s.StaffProjectEmailTypeId,
                    StaffProjectEmailTypeDescription = x.t.Description,
                    CreatedDateTime                  = x.s.CreatedDateTime.ToOffset(TimeSpan.FromHours(-5)).DateTime,
                    CreatedUserId                    = x.s.CreatedUserId,
                    UpdatedDateTime                  = x.s.UpdatedDateTime.HasValue
                        ? x.s.UpdatedDateTime.Value.ToOffset(TimeSpan.FromHours(-5)).DateTime
                        : null,
                    UpdatedUserId = x.s.UpdatedUserId,
                    Active        = x.s.Active
                })
                .ToListAsync();

            return new PagedResult<StaffProjectEmailDto>
            {
                Page         = filter.Page,
                PageSize     = pageSize,
                TotalRecords = totalRecords,
                TotalPages   = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Data         = data
            };
        }

        public async Task Create(StaffProjectEmailCreateDto dto, int userId)
        {
            var exists = await _context.StaffProjectEmail
                .AnyAsync(s => s.ProjectId == dto.ProjectId && s.Email == dto.Email.Trim() && s.State);

            if (exists)
                throw new AbrilException("Ya existe ese correo para el proyecto indicado.");

            var record = new StaffProjectEmail
            {
                ProjectId               = dto.ProjectId,
                Email                   = dto.Email.Trim(),
                StaffProjectEmailTypeId = dto.StaffProjectEmailTypeId,
                Active                  = true,
                State                   = true,
                CreatedDateTime         = DateTimeOffset.UtcNow,
                CreatedUserId           = userId
            };

            _context.StaffProjectEmail.Add(record);
            await _context.SaveChangesAsync();
        }

        public async Task Update(StaffProjectEmailEditDto dto, int userId)
        {
            var record = await _context.StaffProjectEmail
                .FirstOrDefaultAsync(s => s.StaffProjectEmailId == dto.StaffProjectEmailId && s.State);

            if (record == null)
                throw new AbrilException("El registro no existe.");

            var duplicate = await _context.StaffProjectEmail
                .AnyAsync(s => s.ProjectId == record.ProjectId
                            && s.Email == dto.Email.Trim()
                            && s.StaffProjectEmailId != dto.StaffProjectEmailId
                            && s.State);

            if (duplicate)
                throw new AbrilException("Ya existe ese correo para el proyecto indicado.");

            record.Email                   = dto.Email.Trim();
            record.StaffProjectEmailTypeId = dto.StaffProjectEmailTypeId;
            record.Active                  = dto.Active;
            record.UpdatedDateTime         = DateTimeOffset.UtcNow;
            record.UpdatedUserId           = userId;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> Delete(int staffProjectEmailId, int userId)
        {
            var record = await _context.StaffProjectEmail
                .FirstOrDefaultAsync(s => s.StaffProjectEmailId == staffProjectEmailId && s.State);

            if (record == null)
                return false;

            record.State           = false;
            record.Active          = false;
            record.UpdatedDateTime = DateTimeOffset.UtcNow;
            record.UpdatedUserId   = userId;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}

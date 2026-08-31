using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using System.Linq;

namespace Abril_Backend.Infrastructure.Repositories {
    public class MilestoneRepository {
        private readonly AppDbContext _context;
        private readonly IDbContextFactory<AppDbContext> _factory;
        public MilestoneRepository(AppDbContext contexto, IDbContextFactory<AppDbContext> factory) {
            _context = contexto;
            _factory = factory;
        }

        public async Task<List<MilestoneDTO>> GetAll()
        {
            var registros = _context.Milestone
                .OrderBy(item => item.MilestoneDescription)
                .Select(item => new MilestoneDTO
                {
                    MilestoneId = item.MilestoneId,
                    MilestoneDescription = item.MilestoneDescription,

                    CreatedDateTime = item.CreatedDateTime,
                    CreatedUserId = item.CreatedUserId,
                    UpdatedDateTime = item.UpdatedDateTime,
                    UpdatedUserId = item.UpdatedUserId,
                    Active = item.Active
                });
            return await registros.ToListAsync();
        }

        public async Task<List<MilestoneDTO>> GetAllFactory()
        {
            using var ctx = _factory.CreateDbContext();
            var registros = ctx.Milestone
                .Where(item => item.State)
                .OrderBy(item => item.MilestoneDescription)
                .Select(item => new MilestoneDTO
                {
                    MilestoneId = item.MilestoneId,
                    MilestoneDescription = item.MilestoneDescription,

                    CreatedDateTime = item.CreatedDateTime,
                    CreatedUserId = item.CreatedUserId,
                    UpdatedDateTime = item.UpdatedDateTime,
                    UpdatedUserId = item.UpdatedUserId,
                    Active = item.Active
                });
            return await registros.ToListAsync();
        }

        public async Task<List<MilestoneSimpleDTO>> GetAllFactorySimple()
        {
            using var ctx = _factory.CreateDbContext();
            var registros = ctx.Milestone
                .Where(item => item.State)
                .Select(item => new MilestoneSimpleDTO
                {
                    MilestoneId = item.MilestoneId,
                    MilestoneDescription = item.MilestoneDescription,
                });
            return await registros.ToListAsync();
        }

        public async Task<object> GetPaged(int page)
        {
            const int pageSize = 10;

            var query = from milestone in _context.Milestone
                        where milestone.State == true
                        orderby milestone.MilestoneId descending
                        select new MilestoneDTO
                        {
                            MilestoneId = milestone.MilestoneId,
                            MilestoneDescription = milestone.MilestoneDescription,
                            CreatedDateTime = milestone.CreatedDateTime,
                            CreatedUserId = milestone.CreatedUserId,
                            UpdatedDateTime = milestone.UpdatedDateTime,
                            UpdatedUserId = milestone.UpdatedUserId,
                            Active = milestone.Active
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

        public async Task<Milestone> Create(MilestoneCreateDTO dto, int userId)
        {
            var milestone = await _context.Milestone.FirstOrDefaultAsync(a => a.MilestoneDescription == dto.MilestoneDescription.Trim());

            if (milestone != null && milestone.State)
                throw new AbrilException("El hito ya existe");

            if (milestone != null && !milestone.State)
            {
                milestone.State = true;
                milestone.Active = dto.Active;
                milestone.UpdatedDateTime = DateTime.UtcNow;
                milestone.UpdatedUserId = userId;

                await _context.SaveChangesAsync();
                return milestone;
            }

            milestone = new Milestone
            {
                MilestoneDescription = dto.MilestoneDescription.Trim(),
                Active = dto.Active,
                State = true,
                CreatedDateTime = DateTime.UtcNow,
                CreatedUserId = userId
            };

            _context.Milestone.Add(milestone);
            await _context.SaveChangesAsync();

            return milestone;
        }

        public async Task<Milestone> Update(MilestoneEditDTO dto, int userId)
        {
            var milestone = await _context.Milestone.FirstOrDefaultAsync(p => p.MilestoneId == dto.MilestoneId);

            if (milestone == null)
                throw new AbrilException("El milestone no existe");

            var duplicate = await _context.Milestone.FirstOrDefaultAsync(p =>
                p.MilestoneDescription == dto.MilestoneDescription.Trim() &&
                p.MilestoneId != dto.MilestoneId &&
                p.State
            );

            if (duplicate != null)
                throw new AbrilException("Ya existe otra milestone con la misma descripción");

            milestone.MilestoneDescription = dto.MilestoneDescription.Trim();
            milestone.Active = dto.Active;
            milestone.UpdatedDateTime = DateTime.UtcNow;
            milestone.UpdatedUserId = userId;

            await _context.SaveChangesAsync();

            return milestone;
        }

        public async Task<bool> DeleteSoftAsync(int milestoneId, int userId)
        {
            var milestone = await _context.Milestone.FirstOrDefaultAsync(u => u.MilestoneId == milestoneId && u.State == true);

            if (milestone == null)
                return false;

            milestone.State = false;
            milestone.Active = false;
            milestone.UpdatedDateTime = DateTime.UtcNow;
            milestone.UpdatedUserId = userId;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
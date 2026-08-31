using Microsoft.EntityFrameworkCore;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Application.Dtos;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Infrastructure.Repositories
{
    public class MilestoneScheduleRepository : IMilestoneScheduleRepository
    {
        private readonly AppDbContext _context;
        private readonly IDbContextFactory<AppDbContext> _factory;

        public MilestoneScheduleRepository(AppDbContext context, IDbContextFactory<AppDbContext> factory)
        {
            _context = context;
            _factory = factory;
        }

        public async Task<List<ScheduleChangeInfoDTO>> GetSchedulesWithChangesThisMonthAsync()
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var startOfNextMonth = startOfMonth.AddMonths(1);

            var data = await (
                from msh in _context.MilestoneScheduleHistory
                join p in _context.Project on msh.ProjectId equals p.ProjectId
                join u in _context.User on msh.CreatedUserId equals u.UserId
                join person in _context.Person on u.UserId equals person.UserId
                where msh.CreatedDateTime >= startOfMonth
                    && msh.CreatedDateTime < startOfNextMonth
                    && msh.Active && msh.State
                select new
                {
                    ProjectDescription = p.ProjectDescription ?? string.Empty,
                    ChangedBy = person.FullName,
                    ChangeDate = msh.CreatedDateTime
                }
            ).ToListAsync();

            return data
                .GroupBy(x => new { x.ProjectDescription, x.ChangedBy })
                .Select(g => new ScheduleChangeInfoDTO
                {
                    ProjectDescription = g.Key.ProjectDescription,
                    ChangedBy = g.Key.ChangedBy,
                    ChangeDate = g.Select(x => x.ChangeDate).Distinct().OrderBy(d => d).ToList()
                })
                .ToList();
        }

        public async Task<List<MilestoneScheduleDTO>> GetAllByMilestoneScheduleHistoryIdFactory(int milestoneScheduleHistoryId)
        {
            using var ctx = _factory.CreateDbContext();

            var registros = from ms in ctx.MilestoneSchedule
                            join m in ctx.Milestone on ms.MilestoneId equals m.MilestoneId into gj
                            from m in gj.DefaultIfEmpty()
                            where ms.State && ms.MilestoneScheduleHistoryId == milestoneScheduleHistoryId
                            orderby ms.Order
                            select new MilestoneScheduleDTO
                            {
                                MilestoneScheduleId = ms.MilestoneScheduleId,
                                MilestoneId = ms.MilestoneId,
                                MilestoneDescription = ms.MilestoneId != null ? m.MilestoneDescription : ms.CustomDescription,
                                MilestoneScheduleHistoryId = ms.MilestoneScheduleHistoryId,
                                Order = ms.Order,
                                PlannedStartDate = ms.PlannedStartDate,
                                PlannedEndDate = ms.PlannedEndDate,
                                FechaRealFin = ms.FechaRealFin,
                                CreatedDateTime = ms.CreatedDateTime,
                                CreatedUserId = ms.CreatedUserId,
                                UpdatedDateTime = ms.UpdatedDateTime,
                                UpdatedUserId = ms.UpdatedUserId,
                                Active = ms.Active,
                                EsHitoCritico = ms.EsHitoCritico
                            };

            return await registros.ToListAsync();
        }

        public async Task CulminarAsync(int milestoneScheduleId, DateOnly? fechaRealFin, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var ms = await ctx.MilestoneSchedule
                .FirstOrDefaultAsync(x => x.MilestoneScheduleId == milestoneScheduleId && x.State);
            if (ms == null)
                throw new AbrilException("Hito no encontrado.", 404);

            ms.FechaRealFin = fechaRealFin;
            ms.UpdatedDateTime = DateTime.UtcNow;
            ms.UpdatedUserId = userId;

            await ctx.SaveChangesAsync();
        }

        public async Task MarcarCriticoAsync(int milestoneScheduleId, bool esHitoCritico, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var ms = await ctx.MilestoneSchedule
                .FirstOrDefaultAsync(x => x.MilestoneScheduleId == milestoneScheduleId && x.State);
            if (ms == null)
                throw new AbrilException("Hito no encontrado.", 404);

            ms.EsHitoCritico = esHitoCritico;
            ms.UpdatedDateTime = DateTime.UtcNow;
            ms.UpdatedUserId = userId;

            await ctx.SaveChangesAsync();
        }
    }
}

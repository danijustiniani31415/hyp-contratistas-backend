using Microsoft.EntityFrameworkCore;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Application.Dtos;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Infrastructure.Repositories
{
    public class MilestoneScheduleHistoryRepository : IMilestoneScheduleHistoryRepository
    {
        private readonly AppDbContext _context;
        private readonly IDbContextFactory<AppDbContext> _factory;

        public MilestoneScheduleHistoryRepository(AppDbContext context, IDbContextFactory<AppDbContext> factory)
        {
            _context = context;
            _factory = factory;
        }

        public async Task<List<MilestoneScheduleHistoryDTO>> GetAllByProjectIdFactory(int projectId)
        {
            using var ctx = _factory.CreateDbContext();
            var registros = ctx.MilestoneScheduleHistory
                .Where(item => item.State && item.ProjectId == projectId)
                .OrderByDescending(item => item.CreatedDateTime)
                .Select(item => new MilestoneScheduleHistoryDTO
                {
                    MilestoneScheduleHistoryId = item.MilestoneScheduleHistoryId,
                    ProjectId = item.ProjectId,
                    CreatedDateTime = item.CreatedDateTime,
                    CreatedUserId = item.CreatedUserId,
                    UpdatedDateTime = item.UpdatedDateTime,
                    UpdatedUserId = item.UpdatedUserId,
                    Active = item.Active
                });
            return await registros.ToListAsync();
        }

        public async Task<ScheduleChangeResult> Create(MilestoneScheduleHistoryCreateDTO dto, int userId)
        {
            var lastHistory = await _context.MilestoneScheduleHistory
                .Where(h => h.ProjectId == dto.ProjectId && h.Active && h.State)
                .OrderByDescending(h => h.CreatedDateTime)
                .FirstOrDefaultAsync();

            // Carga los hitos del último historial UNA SOLA VEZ; se reutiliza en ambos bloques
            List<MilestoneSchedule> lastMilestones = new();
            if (lastHistory != null)
            {
                lastMilestones = await _context.MilestoneSchedule
                    .Where(ms => ms.MilestoneScheduleHistoryId == lastHistory.MilestoneScheduleHistoryId
                                 && ms.Active && ms.State)
                    .OrderBy(ms => ms.Order)
                    .ToListAsync();
            }

            if (lastHistory != null)
            {
                var newMilestones = dto.MilestoneSchedules.OrderBy(ms => ms.Order).ToList();

                if (lastMilestones.Count == newMilestones.Count)
                {
                    bool areEqual = true;
                    for (int i = 0; i < lastMilestones.Count; i++)
                    {
                        var last = lastMilestones[i];
                        var current = newMilestones[i];
                        if (last.MilestoneId != current.MilestoneId ||
                            last.Order != current.Order ||
                            last.PlannedStartDate != current.PlannedStartDate ||
                            last.PlannedEndDate != current.PlannedEndDate)
                        {
                            areEqual = false;
                            break;
                        }
                    }
                    if (areEqual && !dto.ForceSave)
                        throw new AbrilException("El cronograma es igual a la última versión subida.");
                }
            }

            List<MilestoneChange> changes = new();

            if (lastHistory != null)
            {
                // milestoneIds: unión de los nuevos y los del historial anterior (ya en memoria), solo catálogo (no personalizados)
                var milestoneIds = dto.MilestoneSchedules.Select(m => m.MilestoneId)
                    .Union(lastMilestones.Select(ms => ms.MilestoneId))
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .Distinct()
                    .ToList();

                var milestoneDescriptions = await _context.Milestone
                    .Where(m => milestoneIds.Contains(m.MilestoneId))
                    .ToDictionaryAsync(m => m.MilestoneId, m => m.MilestoneDescription);

                changes = DetectChanges(lastMilestones, dto.MilestoneSchedules, milestoneDescriptions);

                if (!changes.Any() && !dto.ForceSave)
                    throw new AbrilException("El cronograma es igual a la última versión subida.");
            }

            if (dto.ForceSave && changes.Any())
                throw new AbrilException("Para guardar sin cambios la última versión subida debe ser igual a la que se está editando actualmente.");

            var history = new MilestoneScheduleHistory
            {
                ProjectId = dto.ProjectId,
                IsEqualToLastVersion = !changes.Any(),
                Active = true,
                State = true,
                CreatedDateTime = DateTime.UtcNow,
                CreatedUserId = userId
            };

            _context.MilestoneScheduleHistory.Add(history);
            await _context.SaveChangesAsync();

            var milestoneSchedules = dto.MilestoneSchedules.Select(item => new MilestoneSchedule
            {
                MilestoneId = item.MilestoneId,
                CustomDescription = item.CustomDescription,
                MilestoneScheduleHistoryId = history.MilestoneScheduleHistoryId,
                Order = item.Order,
                PlannedStartDate = item.PlannedStartDate,
                PlannedEndDate = item.PlannedEndDate,
                EsHitoCritico = item.EsHitoCritico,
                Active = true,
                State = true,
                CreatedDateTime = DateTime.UtcNow,
                CreatedUserId = userId
            }).ToList();

            _context.MilestoneSchedule.AddRange(milestoneSchedules);
            await _context.SaveChangesAsync();

            var projectName = await _context.Project
                .Where(p => p.ProjectId == dto.ProjectId)
                .Select(p => p.ProjectDescription ?? string.Empty)
                .FirstAsync();

            return new ScheduleChangeResult { ProjectName = projectName, Changes = changes };
        }

        public async Task<List<UserWithoutMilestoneDTO>> GetUsersWithoutScheduleHistoryThisMonth()
        {
            await using var ctx = await _factory.CreateDbContextAsync();

            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var startOfNextMonth = startOfMonth.AddMonths(1);

            var query =
                from pr in ctx.ProjectResident
                join pj in ctx.Project on pr.ProjectId equals pj.ProjectId
                join u in ctx.User on pr.UserId equals u.UserId
                join person in ctx.Person on u.UserId equals person.UserId
                where pr.Active && pr.State && pj.Active
                where !ctx.MilestoneScheduleHistory.Any(msh =>
                    msh.ProjectId == pr.ProjectId &&
                    msh.CreatedUserId == pr.UserId &&
                    msh.Active && msh.State &&
                    msh.CreatedDateTime >= startOfMonth &&
                    msh.CreatedDateTime < startOfNextMonth)
                group new { pj, u } by new { u.UserId, person.FullName, u.Email }
                into g
                select new UserWithoutMilestoneDTO
                {
                    UserId = g.Key.UserId,
                    UserFullName = g.Key.FullName,
                    Email = g.Key.Email,
                    Projects = g.Select(x => new ProjectSimpleDTO
                    {
                        ProjectId = x.pj.ProjectId,
                        ProjectDescription = x.pj.ProjectDescription ?? string.Empty
                    }).ToList()
                };

            return await query.ToListAsync();
        }

        private List<MilestoneChange> DetectChanges(
            List<MilestoneSchedule> lastMilestones,
            List<MilestoneScheduleCreateDTO> newMilestones,
            Dictionary<int, string> milestoneDescriptions)
        {
            // Se compara por Order (posición en el cronograma), no por MilestoneId: los hitos
            // personalizados (MilestoneId nulo, ej. una 2da/3ra grúa) no tienen un id de catálogo
            // que los identifique de forma única, pero su posición sí es estable.
            string Describe(int? milestoneId, string? customDescription) =>
                milestoneId.HasValue
                    ? milestoneDescriptions.GetValueOrDefault(milestoneId.Value, "Desconocido")
                    : (customDescription ?? "Hito personalizado");

            var changes = new List<MilestoneChange>();
            var lastDict = lastMilestones.ToDictionary(m => m.Order);
            var newDict = newMilestones.ToDictionary(m => m.Order);

            foreach (var newItem in newMilestones)
            {
                if (!lastDict.TryGetValue(newItem.Order, out var last))
                {
                    changes.Add(new MilestoneChange
                    {
                        MilestoneId = newItem.MilestoneId,
                        MilestoneDescription = Describe(newItem.MilestoneId, newItem.CustomDescription),
                        ChangeType = "Añadido"
                    });
                    continue;
                }

                var change = new MilestoneChange
                {
                    MilestoneId = newItem.MilestoneId,
                    MilestoneDescription = Describe(newItem.MilestoneId, newItem.CustomDescription),
                    ChangeType = "Actualizado",
                    OrderChanged = last.MilestoneId != newItem.MilestoneId || last.CustomDescription != newItem.CustomDescription,
                    StartDateChanged = last.PlannedStartDate != newItem.PlannedStartDate,
                    EndDateChanged = last.PlannedEndDate != newItem.PlannedEndDate
                };

                if (change.OrderChanged || change.StartDateChanged || change.EndDateChanged)
                    changes.Add(change);
            }

            foreach (var last in lastMilestones)
            {
                if (!newDict.ContainsKey(last.Order))
                    changes.Add(new MilestoneChange
                    {
                        MilestoneId = last.MilestoneId,
                        MilestoneDescription = Describe(last.MilestoneId, last.CustomDescription),
                        ChangeType = "Eliminado"
                    });
            }

            return changes;
        }
    }
}

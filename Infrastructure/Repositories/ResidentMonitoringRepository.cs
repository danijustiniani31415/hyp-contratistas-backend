using Abril_Backend.Shared.Constants;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Models;

namespace Abril_Backend.Infrastructure.Repositories
{
    public class ResidentMonitoringRepository : IResidentMonitoringRepository
    {
        private readonly AppDbContext _context;
        private readonly IDbContextFactory<AppDbContext> _factory;
        public ResidentMonitoringRepository(AppDbContext contexto, IDbContextFactory<AppDbContext> factory)
        {
            _context = contexto;
            _factory = factory;
        }

        public async Task<IEnumerable<TrackingRawDto>> GetTrackingDataAsync(
            int? projectId,
            int? residentUserId,
            int? month,
            int? year)
        {
            return await (
                from pr in _context.ProjectResident
                join p in _context.Project on pr.ProjectId equals p.ProjectId
                join u in _context.User on pr.UserId equals u.UserId
                join pe in _context.Person on u.UserId equals pe.UserId
                where pr.State && pr.Active
                   && p.Active
                   && !_context.ProyectoFiltro.Any(f => f.ProjectId == p.ProjectId && f.FuncionalidadId == ProyectoFiltroFuncionalidades.Residentes && !f.Active)
                   && u.State && u.Active
                   && (!projectId.HasValue || p.ProjectId == projectId.Value)
                   && (!residentUserId.HasValue || u.UserId == residentUserId.Value)
                select new TrackingRawDto
                {
                    ProjectId = p.ProjectId,
                    ProjectDescription = p.ProjectDescription ?? string.Empty,
                    ResidentUserId = u.UserId,
                    ResidentFullName = pe.FullName ?? (pe.FirstLastName + " " + pe.FirstNames),

                    ScheduleReportedCount = _context.MilestoneScheduleHistory
                        .Where(h =>
                            h.ProjectId == p.ProjectId
                            && h.State && h.Active
                            && (!month.HasValue || h.CreatedDateTime.AddHours(-5).Month == month.Value)
                            && (!year.HasValue || h.CreatedDateTime.AddHours(-5).Year == year.Value))
                        .Select(h => new
                        {
                            h.CreatedDateTime.AddHours(-5).Year,
                            h.CreatedDateTime.AddHours(-5).Month
                        })
                        .Distinct()
                        .Count(),

                    IvtsUploaded = _context.IvtControlPdf.Count(x =>
                        x.ProjectId == p.ProjectId
                        && x.State && x.Active
                        && (!month.HasValue || x.PeriodDate.Month == month.Value)
                        && (!year.HasValue || x.PeriodDate.Year == year.Value)),

                    ConstructionLogsUploaded = _context.ConstructionSiteLogbookControl.Count(x =>
                        x.ProjectId == p.ProjectId
                        && x.State && x.Active
                        && (!month.HasValue || x.PeriodDate.Month == month.Value)
                        && (!year.HasValue || x.PeriodDate.Year == year.Value)),

                    TotalIncidences = _context.ResidentReportIncidence.Count(x =>
                        x.ProjectId == p.ProjectId
                        && x.State && x.Active
                        && (!month.HasValue || x.CreatedDateTime.AddHours(-5).Month == month.Value)
                        && (!year.HasValue || x.CreatedDateTime.AddHours(-5).Year == year.Value)),

                    AnsweredIncidences = _context.ResidentReportIncidence.Count(x =>
                        x.ProjectId == p.ProjectId
                        && x.State && x.Active
                        && (!month.HasValue || x.CreatedDateTime.AddHours(-5).Month == month.Value)
                        && (!year.HasValue || x.CreatedDateTime.AddHours(-5).Year == year.Value)
                        && x.ResidentReportResponses.Any(r => r.State && r.Active)),
                }
            ).ToListAsync();
        }
    }
}
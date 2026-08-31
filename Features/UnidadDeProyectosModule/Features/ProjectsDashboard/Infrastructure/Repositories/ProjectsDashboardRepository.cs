using Abril_Backend.Shared.Constants;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.ProjectsDashboard.Application.Dtos;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.ProjectsDashboard.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.ProjectsDashboard.Infrastructure.Repositories
{
    public class ProjectsDashboardRepository : IProjectsDashboardRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly ILogger<ProjectsDashboardRepository> _logger;

        public ProjectsDashboardRepository(
            IDbContextFactory<AppDbContext> factory,
            ILogger<ProjectsDashboardRepository> logger)
        {
            _factory = factory;
            _logger = logger;
        }

        // -----------------------------------------------------------------------
        // Filters
        // -----------------------------------------------------------------------

        public async Task<(List<ProyectoSimpleDto> Projects, List<string> Estados, List<ResponsableSimpleDto> Responsables)> GetFiltersDataFactory()
        {
            using var ctx = _factory.CreateDbContext();

            // Una sola query trae las columnas necesarias; las 3 listas se derivan en memoria.
            var rows = await ctx.Project
                .Where(p => p.State && p.Active && p.TieneUnidadDeProyectos && !ctx.ProyectoFiltro.Any(f => f.ProjectId == p.ProjectId && f.FuncionalidadId == ProyectoFiltroFuncionalidades.UdpDashboard && !f.Active))
                .Select(p => new
                {
                    p.ProjectId,
                    p.ProjectDescription,
                    p.Estado,
                    p.ResponsableUdpId,
                    p.ResponsableUdp
                })
                .ToListAsync();

            var projects = rows
                .OrderBy(p => p.ProjectDescription)
                .Select(p => new ProyectoSimpleDto
                {
                    Id = p.ProjectId,
                    Nombre = p.ProjectDescription
                })
                .ToList();

            var estados = rows
                .Where(p => p.Estado != null)
                .Select(p => p.Estado!)
                .Distinct()
                .OrderBy(e => e)
                .ToList();

            var responsables = rows
                .Where(p => p.ResponsableUdpId.HasValue)
                .Select(p => new { p.ResponsableUdpId, p.ResponsableUdp })
                .Distinct()
                .Select(r => new ResponsableSimpleDto
                {
                    Id = r.ResponsableUdpId!.Value,
                    Nombre = r.ResponsableUdp
                })
                .OrderBy(r => r.Nombre)
                .ToList();

            return (projects, estados, responsables);
        }

        // -----------------------------------------------------------------------
        // Dashboard principal
        // -----------------------------------------------------------------------

        public async Task<(List<ProyectoDetalleDto> Proyectos, List<ResponsableRankingDto> Ranking, List<HeatmapResponsableDto> Heatmap)> GetDashboardAsync(
            int? proyectoId, string? estado, int? responsableId, DateOnly today, DateOnly heatDesde, DateOnly heatHasta)
        {
            try
            {
                // Una sola conexión. Los datos base (proyectos + actividades) se cargan UNA vez
                // y dashboard, ranking y heatmap se calculan en memoria sobre ese mismo dataset.
                using var ctx = _factory.CreateDbContext();

                var projects = await BuildProjectQueryAsync(ctx, proyectoId, estado, responsableId);
                _logger.LogInformation("DASHBOARD_DEBUG projects={Count}", projects.Count);
                if (projects.Count == 0)
                    return (new(), new(), new());

                var projectIds = projects.Select(p => p.ProjectId).ToList();
                var activities = await GetActivitiesAsync(ctx, projectIds);
                _logger.LogInformation("DASHBOARD_DEBUG activities={Count}", activities.Count);

                var activitiesByProject = activities
                    .GroupBy(a => a.ProjectId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var proyectos = projects.Select(p =>
                {
                    var items = activitiesByProject.TryGetValue(p.ProjectId, out var list) ? list : new();
                    return BuildProyectoDetalle(p.ProjectId, p.ProjectDescription, p.Estado, p.ResponsableNombre, items, today);
                }).ToList();

                var ranking = BuildRanking(projects, activitiesByProject, today);
                var heatmap = BuildHeatmap(projects, activitiesByProject, heatDesde, heatHasta);

                return (proyectos, ranking, heatmap);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DASHBOARD_ERROR");
                throw;
            }
        }

        // -----------------------------------------------------------------------
        // Ranking de responsables
        // -----------------------------------------------------------------------

        private static List<ResponsableRankingDto> BuildRanking(
            List<ProjectFlat> projects,
            Dictionary<int, List<ActivityFlat>> activitiesByProject,
            DateOnly today)
        {
            var proyectosConResponsable = projects.Where(p => p.ResponsableId.HasValue).ToList();
            if (proyectosConResponsable.Count == 0) return new();

            var responsableNombre = proyectosConResponsable
                .GroupBy(p => p.ResponsableId!.Value)
                .ToDictionary(g => g.Key, g => g.First().ResponsableNombre);

            return proyectosConResponsable
                .GroupBy(p => p.ResponsableId!.Value)
                .Select(g =>
                {
                    var rId = g.Key;
                    var allItems = g
                        .SelectMany(p => activitiesByProject.TryGetValue(p.ProjectId, out var s) ? s : new())
                        .ToList();

                    var totalActs = allItems.Count;
                    var completadas = allItems.Count(a => a.ActualEndDate != null);
                    var vencidas = allItems.Count(a => a.PlannedEndDate < today && a.ActualEndDate == null);
                    var avgProgress = totalActs > 0 ? allItems.Average(a => (double)a.ProgressPercentage) : 0d;
                    var score = Math.Max(0d, Math.Round(avgProgress - vencidas * 5, 1));

                    responsableNombre.TryGetValue(rId, out var nombre);

                    return new ResponsableRankingDto
                    {
                        ResponsableId = rId,
                        Nombre = nombre,
                        Proyectos = g.Count(),
                        Completadas = completadas,
                        Vencidas = vencidas,
                        Score = score
                    };
                })
                .OrderByDescending(r => r.Score)
                .ToList();
        }

        // -----------------------------------------------------------------------
        // Heatmap de carga
        // -----------------------------------------------------------------------

        private static List<HeatmapResponsableDto> BuildHeatmap(
            List<ProjectFlat> projects,
            Dictionary<int, List<ActivityFlat>> activitiesByProject,
            DateOnly fechaDesde, DateOnly fechaHasta)
        {
            var proyectosConResponsable = projects.Where(p => p.ResponsableId.HasValue).ToList();
            if (proyectosConResponsable.Count == 0) return new();

            var projectResponsable = proyectosConResponsable
                .ToDictionary(p => p.ProjectId, p => p.ResponsableNombre);

            var flat = proyectosConResponsable
                .SelectMany(p => activitiesByProject.TryGetValue(p.ProjectId, out var s) ? s : new())
                .Where(a => a.PlannedEndDate.HasValue
                         && a.PlannedEndDate.Value >= fechaDesde
                         && a.PlannedEndDate.Value <= fechaHasta)
                .Select(a => new
                {
                    Responsable = projectResponsable[a.ProjectId] ?? string.Empty,
                    Semana = ToIsoWeekLabel(a.PlannedEndDate!.Value)
                })
                .GroupBy(a => new { a.Responsable, a.Semana })
                .Select(g => new { g.Key.Responsable, g.Key.Semana, Cantidad = g.Count() })
                .ToList();

            return flat
                .GroupBy(x => x.Responsable)
                .Select(g => new HeatmapResponsableDto
                {
                    Responsable = g.Key,
                    Semanas = g
                        .OrderBy(x => x.Semana)
                        .Select(x => new HeatmapSemanaDto { Semana = x.Semana, Cantidad = x.Cantidad })
                        .ToList()
                })
                .OrderBy(h => h.Responsable)
                .ToList();
        }

        // -----------------------------------------------------------------------
        // Detalle por proyecto
        // -----------------------------------------------------------------------

        public async Task<ProyectoDetailDashboardDto> GetProyectoDetailAsync(int proyectoId, DateOnly today)
        {
            using var ctx = _factory.CreateDbContext();

            var project = await ctx.Project
                .Where(p => p.ProjectId == proyectoId)
                .Select(p => new { p.ProjectId, p.ProjectDescription, p.Estado, p.ResponsableUdp })
                .FirstOrDefaultAsync();

            var responsableNombre = project?.ResponsableUdp;

            var activities = await GetActivitiesAsync(ctx, new List<int> { proyectoId });

            if (activities.Count == 0)
            {
                return new ProyectoDetailDashboardDto
                {
                    ProyectoId = proyectoId,
                    ProyectoNombre = project?.ProjectDescription,
                    Estado = project?.Estado,
                    Semaforo = "verde"
                };
            }

            var vencidas = activities.Count(a => a.PlannedEndDate < today && a.ActualEndDate == null);

            var avanceReal = Math.Round(activities.Average(a => (double)a.ProgressPercentage), 1);

            var conFechas = activities
                .Where(a => a.PlannedStartDate.HasValue && a.PlannedEndDate.HasValue).ToList();
            var avanceProgramado = conFechas.Count > 0
                ? Math.Round(conFechas.Average(a =>
                {
                    var totalDays = a.PlannedEndDate!.Value.DayNumber - a.PlannedStartDate!.Value.DayNumber;
                    if (totalDays <= 0) return 100d;
                    var elapsed = today.DayNumber - a.PlannedStartDate!.Value.DayNumber;
                    return Math.Clamp((double)elapsed / totalDays * 100, 0d, 100d);
                }), 1)
                : 0d;

            var diasRetraso = vencidas > 0
                ? activities
                    .Where(a => a.PlannedEndDate < today && a.ActualEndDate == null && a.PlannedEndDate.HasValue)
                    .Max(a => today.DayNumber - a.PlannedEndDate!.Value.DayNumber)
                : 0;

            var actVencidas = activities
                .Where(a => a.PlannedEndDate < today && a.ActualEndDate == null && a.PlannedEndDate.HasValue)
                .Select(a => new ActividadCriticaDto
                {
                    Id = a.ProjectActivityId,
                    Nombre = a.ActivityDescription,
                    ResponsableNombre = responsableNombre,
                    FinProgramado = a.PlannedEndDate,
                    DiasRetraso = today.DayNumber - a.PlannedEndDate!.Value.DayNumber
                })
                .OrderByDescending(a => a.DiasRetraso)
                .ToList();

            // Críticas: vencen en los próximos 7 días y no están culminadas
            var actCriticas = activities
                .Where(a => a.ActualEndDate == null
                         && a.PlannedEndDate.HasValue
                         && a.PlannedEndDate >= today
                         && a.PlannedEndDate <= today.AddDays(7))
                .Select(a => new ActividadCriticaDto
                {
                    Id = a.ProjectActivityId,
                    Nombre = a.ActivityDescription,
                    ResponsableNombre = responsableNombre,
                    FinProgramado = a.PlannedEndDate,
                    DiasRetraso = 0
                })
                .OrderBy(a => a.FinProgramado)
                .ToList();

            var ganttTasks = activities
                .Where(a => a.PlannedStartDate.HasValue && a.PlannedEndDate.HasValue)
                .Select(a => new GanttTareaDto
                {
                    Id = a.ProjectActivityId,
                    Text = a.ActivityDescription,
                    StartDate = a.PlannedStartDate!.Value.ToString("dd-MM-yyyy") + " 00:00",
                    Duration = a.PlannedEndDate.HasValue && a.PlannedStartDate.HasValue
                        ? Math.Max(1, a.PlannedEndDate.Value.DayNumber - a.PlannedStartDate.Value.DayNumber)
                        : 1,
                    Progress = Math.Round(a.ProgressPercentage / 100.0, 2)
                })
                .OrderBy(t => t.StartDate)
                .ToList();

            return new ProyectoDetailDashboardDto
            {
                ProyectoId = proyectoId,
                ProyectoNombre = project?.ProjectDescription,
                Estado = project?.Estado,
                AvanceReal = avanceReal,
                AvanceProgramado = avanceProgramado,
                DiasRetraso = diasRetraso,
                Semaforo = diasRetraso == 0 ? "verde" : diasRetraso <= 7 ? "amarillo" : "rojo",
                ActividadesVencidas = actVencidas,
                ActividadesCriticas = actCriticas,
                Gantt = new GanttDataDto { Tasks = ganttTasks, Links = new() }
            };
        }

        // -----------------------------------------------------------------------
        // Helpers privados
        // -----------------------------------------------------------------------

        private async Task<List<ProjectFlat>> BuildProjectQueryAsync(
            AppDbContext ctx, int? proyectoId, string? estado, int? responsableId)
        {
            var q = ctx.Project.Where(p => p.State && p.Active && p.TieneUnidadDeProyectos && !ctx.ProyectoFiltro.Any(f => f.ProjectId == p.ProjectId && f.FuncionalidadId == ProyectoFiltroFuncionalidades.UdpDashboard && !f.Active));
            if (proyectoId.HasValue) q = q.Where(p => p.ProjectId == proyectoId.Value);
            if (estado != null) q = q.Where(p => p.Estado == estado);
            if (responsableId.HasValue) q = q.Where(p => p.ResponsableUdpId == responsableId.Value);

            return await q
                .Select(p => new ProjectFlat
                {
                    ProjectId = p.ProjectId,
                    ProjectDescription = p.ProjectDescription,
                    Estado = p.Estado,
                    ResponsableNombre = p.ResponsableUdp,
                    ResponsableId = p.ResponsableUdpId
                })
                .ToListAsync();
        }

        private async Task<List<ActivityFlat>> GetActivitiesAsync(
            AppDbContext ctx, List<int> projectIds,
            DateOnly? fechaDesde = null, DateOnly? fechaHasta = null)
        {
            var q = ctx.ProjectActivity
                .Where(a => projectIds.Contains(a.ProjectId) && a.State && a.Active);

            if (fechaDesde.HasValue) q = q.Where(a => a.PlannedEndDate >= fechaDesde.Value);
            if (fechaHasta.HasValue) q = q.Where(a => a.PlannedEndDate <= fechaHasta.Value);

            return await q
                .Select(a => new ActivityFlat
                {
                    ProjectActivityId = a.ProjectActivityId,
                    ProjectId = a.ProjectId,
                    ActivityDescription = a.ActivityDescription,
                    PlannedStartDate = a.PlannedStartDate,
                    PlannedEndDate = a.PlannedEndDate,
                    ActualEndDate = a.ActualEndDate,
                    ProgressPercentage = a.ProgressPercentage
                })
                .ToListAsync();
        }

        private static ProyectoDetalleDto BuildProyectoDetalle(
            int projectId, string? description, string? estado, string? responsable,
            List<ActivityFlat> items, DateOnly today)
        {
            var total = items.Count;
            var culminadas = items.Count(a => a.ActualEndDate != null);
            var vencidas = items.Count(a => a.PlannedEndDate < today && a.ActualEndDate == null);
            var enProceso = items.Count(a =>
                a.ActualEndDate == null
                && a.PlannedStartDate.HasValue && a.PlannedStartDate <= today
                && (a.PlannedEndDate == null || a.PlannedEndDate >= today));
            var avanceReal = total > 0
                ? Math.Round(items.Average(a => (double)a.ProgressPercentage), 1)
                : 0d;

            var conFechas = items
                .Where(a => a.PlannedStartDate.HasValue && a.PlannedEndDate.HasValue)
                .ToList();
            var avanceProgramado = conFechas.Count > 0
                ? Math.Round(conFechas.Average(a =>
                {
                    var totalDays = a.PlannedEndDate!.Value.DayNumber - a.PlannedStartDate!.Value.DayNumber;
                    if (totalDays <= 0) return 100d;
                    var elapsed = today.DayNumber - a.PlannedStartDate!.Value.DayNumber;
                    return Math.Clamp((double)elapsed / totalDays * 100, 0d, 100d);
                }), 1)
                : 0d;

            var diasRetraso = vencidas > 0
                ? items
                    .Where(a => a.PlannedEndDate < today && a.ActualEndDate == null && a.PlannedEndDate.HasValue)
                    .Max(a => today.DayNumber - a.PlannedEndDate!.Value.DayNumber)
                : 0;

            return new ProyectoDetalleDto
            {
                ProyectoId = projectId,
                ProjectDescription = description,
                Estado = estado,
                ResponsableNombre = responsable,
                TotalActividades = total,
                Culminadas = culminadas,
                EnProceso = enProceso,
                Vencidas = vencidas,
                AvanceReal = avanceReal,
                AvanceProgramado = avanceProgramado,
                EstaConRetraso = vencidas > 0,
                DiasRetraso = diasRetraso,
                Semaforo = diasRetraso == 0 ? "verde" : diasRetraso <= 7 ? "amarillo" : "rojo",
                EtapaNombre = null
            };
        }

        private static string ComputeEstado(
            DateOnly? plannedStart, DateOnly? plannedEnd, DateOnly? actualEndDate, DateOnly today)
        {
            if (actualEndDate != null) return "CULMINADO";
            if (plannedEnd.HasValue && plannedEnd < today) return "VENCIDO";
            if (plannedStart.HasValue && plannedStart <= today) return "EN_PROCESO";
            return "PENDIENTE";
        }

        private static string ToIsoWeekLabel(DateOnly date)
        {
            var dt = date.ToDateTime(TimeOnly.MinValue);
            var year = System.Globalization.ISOWeek.GetYear(dt);
            var week = System.Globalization.ISOWeek.GetWeekOfYear(dt);
            return $"{year}-W{week:D2}";
        }

        private class ActivityFlat
        {
            public int ProjectActivityId { get; init; }
            public int ProjectId { get; init; }
            public string ActivityDescription { get; init; } = string.Empty;
            public DateOnly? PlannedStartDate { get; init; }
            public DateOnly? PlannedEndDate { get; init; }
            public DateOnly? ActualEndDate { get; init; }
            public int ProgressPercentage { get; init; }
        }

        private class ProjectFlat
        {
            public int ProjectId { get; set; }
            public string? ProjectDescription { get; set; }
            public string? Estado { get; set; }
            public string? ResponsableNombre { get; set; }
            public int? ResponsableId { get; set; }
        }
    }
}

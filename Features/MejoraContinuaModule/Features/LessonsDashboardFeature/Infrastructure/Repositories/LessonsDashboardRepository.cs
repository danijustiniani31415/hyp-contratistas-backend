using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsDashboardFeature.Application.Dtos;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsDashboardFeature.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.LessonsDashboardFeature.Infrastructure.Repositories
{
    /// <summary>
    /// Dashboard de Lecciones Aprendidas usando el modelo NUEVO: la clasificación
    /// (Fase/Etapa/Subetapa) y el área se derivan de lesson.catalog_item_id + lesson.lesson_area_id
    /// caminando hacia arriba el árbol scope_item (catalog_item / catalog_type), no de
    /// phase_stage_sub_stage_sub_specialty_id ni de area_id.
    /// </summary>
    public class LessonsDashboardRepository : ILessonsDashboardRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        // Nombres de catalog_type que representan cada nivel de clasificación.
        private const string TypeFase = "Fase";
        private const string TypeEtapa = "Etapa";
        private const string TypeSubetapa = "Subetapa";

        public LessonsDashboardRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<LessonsDashboardDataDTO> GetDataAsync(DateTimeOffset? periodDate, int? userId, List<int>? lessonAreaIds, int? obraOficinaStaffId, List<int>? projectIds, string? approvalStatus)
        {
            using var ctx = _factory.CreateDbContext();

            // Misma lógica que el filtro de Lecciones Aprendidas: el front (cascada de
            // área→subárea) ya envía TODOS los lesson_area_id del subárbol donde el
            // usuario se detuvo. Aquí solo filtramos LessonAreaId IN (lista).
            var effectiveAreaIds = (lessonAreaIds != null && lessonAreaIds.Count > 0) ? lessonAreaIds : null;
            // Para el detalle de fases por área: solo cuando se eligió UNA área específica
            // (hoja); si es un padre con varias subáreas, se usa el comportamiento agregado.
            int? singleAreaId = (lessonAreaIds != null && lessonAreaIds.Count == 1) ? lessonAreaIds[0] : (int?)null;

            // 1. Lecciones filtradas (modelo nuevo)
            var q = ctx.Lesson.Where(l => l.Active && l.State);

            if (periodDate.HasValue)
            {
                var start = new DateTimeOffset(periodDate.Value.Year, periodDate.Value.Month, 1, 0, 0, 0, TimeSpan.Zero);
                var end = start.AddMonths(1);
                q = q.Where(l => l.PeriodDate != null && l.PeriodDate >= start && l.PeriodDate < end);
            }
            if (userId.HasValue)
                q = q.Where(l => l.CreatedUserId == userId.Value);

            // Filtro por estado de aprobación (PENDIENTE | APROBADA | RECHAZADA).
            // El front envía "APROBADA" por defecto para que el dashboard muestre solo
            // lecciones aprobadas al cargar; null/"" = todos los estados.
            if (!string.IsNullOrWhiteSpace(approvalStatus))
                q = q.Where(l => l.ApprovalStatus == approvalStatus);

            if (effectiveAreaIds != null)
                q = q.Where(l => l.LessonAreaId != null && effectiveAreaIds.Contains(l.LessonAreaId.Value));

            // Obra / Staff / Oficina Central: filtro propio (antes era el ultimo nivel
            // de la cascada de areas, con nodos de tipo Obra_Oficina).
            if (obraOficinaStaffId.HasValue)
                q = q.Where(l => l.ObraOficinaStaffId == obraOficinaStaffId.Value);

            if (projectIds != null && projectIds.Count > 0)
                q = q.Where(l => l.ProjectId != null && projectIds.Contains(l.ProjectId.Value));

            var lessons = await q
                .Select(l => new
                {
                    l.LessonId,
                    l.ProjectId,
                    l.LessonAreaId,
                    l.CatalogItemId,
                    l.ObraOficinaStaffId,
                    l.CreatedUserId
                })
                .ToListAsync();

            // Tendencia mensual: ignora el filtro de período para mostrar la evolución
            // completa en el tiempo, respetando los filtros de usuario, área, proyectos y estado.
            var lessonsByMonth = await BuildMonthlyTrendAsync(ctx, userId, effectiveAreaIds, obraOficinaStaffId, projectIds, approvalStatus);

            // Usuarios pendientes (de registrar lecciones) del período seleccionado o del mes actual.
            var (pendingLabel, pendingUsers) = await BuildPendingUsersAsync(ctx, periodDate);

            if (lessons.Count == 0)
                return new LessonsDashboardDataDTO
                {
                    LessonsByMonth = lessonsByMonth,
                    PendingPeriodLabel = pendingLabel,
                    PendingUsers = pendingUsers
                };

            // 2. Descripciones de proyecto
            var lessonProjectIds = lessons.Where(l => l.ProjectId.HasValue).Select(l => l.ProjectId!.Value).Distinct().ToList();
            var projDescById = (await ctx.Project
                    .Where(p => lessonProjectIds.Contains(p.ProjectId))
                    .Select(p => new { p.ProjectId, p.ProjectDescription })
                    .ToListAsync())
                .ToDictionary(p => p.ProjectId, p => p.ProjectDescription ?? string.Empty);

            // 2b. Nombres de usuario (para el ranking "Top usuarios")
            var userIds = lessons.Select(l => l.CreatedUserId).Distinct().ToList();
            var userNameById = (await (
                    from u in ctx.User
                    join p in ctx.Person on u.UserId equals p.UserId
                    where userIds.Contains(u.UserId)
                    select new { u.UserId, p.FullName }
                ).ToListAsync())
                .ToDictionary(x => x.UserId, x => x.FullName ?? $"Usuario {x.UserId}");

            // 3. Etiqueta de área (lesson_area -> area_scope -> area_item: nombre de la hoja)
            var areaLabelById = await BuildAreaLabelsAsync(ctx);

            // 3b. Etiquetas del catálogo Obra / Staff / Oficina Central.
            var obraOficinaLabelById = await ctx.WorkersObraOficinaStaff
                .Where(c => c.State)
                .ToDictionaryAsync(c => c.WorkersObraOficinaStaffId, c => c.Name);

            // 4. Clasificación por scope_item (Fase/Etapa/Subetapa) por (lesson_area_id, catalog_item_id)
            //
            // El ORDER BY ScopeItemId es CRÍTICO: como un mismo par puede aparecer
            // en múltiples scope_items (catalog_item reutilizado bajo padres
            // distintos), tenemos que elegir UNO de forma determinística para
            // poder contar cada lección bajo una sola fase. Usamos "menor id
            // gana" — mismo criterio que LessonRepository.BuildAncestorCatalogItemsByPairAsync
            // y LessonEnrichmentHelper. Si se cambia, debe cambiarse en los tres
            // lugares al mismo tiempo o dashboard/listado/filtro se desincronizan.
            var scope = await (
                from si in ctx.ScopeItem
                join ci in ctx.CatalogItem on si.CatalogItemId equals ci.CatalogItemId
                join ct in ctx.CatalogType on ci.CatalogTypeId equals ct.CatalogTypeId
                where si.Active
                orderby si.ScopeItemId
                select new
                {
                    si.ScopeItemId,
                    si.LessonAreaId,
                    si.CatalogItemId,
                    si.ScopeItemParentId,
                    si.DisplayOrder,
                    ct.CatalogTypeName,
                    ci.CatalogItemDescription
                }
            ).ToListAsync();

            var scopeById = scope.ToDictionary(s => s.ScopeItemId);
            var leafByPair = new Dictionary<(int, int), int>();
            foreach (var s in scope)
            {
                var key = (s.LessonAreaId, s.CatalogItemId);
                if (!leafByPair.ContainsKey(key)) leafByPair[key] = s.ScopeItemId;
            }

            // Para cada par (area, catItem) devuelve el mapa tipo -> (id, descripción)
            (int Id, string Desc)? Level(int areaId, int catId, string typeName)
            {
                if (!leafByPair.TryGetValue((areaId, catId), out var leafId)) return null;
                int? cur = leafId;
                int safety = 50;
                while (cur.HasValue && safety-- > 0 && scopeById.TryGetValue(cur.Value, out var n))
                {
                    if (string.Equals(n.CatalogTypeName, typeName, StringComparison.OrdinalIgnoreCase))
                        return (n.CatalogItemId, n.CatalogItemDescription);
                    cur = n.ScopeItemParentId;
                }
                return null;
            }

            // 5. Agregaciones
            var byProject = new Dictionary<int, (string Label, int Count)>();
            var byArea = new Dictionary<int, (string Label, int Count)>();
            var byUser = new Dictionary<int, (string Label, int Count)>();
            var byPhase = new Dictionary<int, (string Label, int Count)>();
            var byObraOficina = new Dictionary<int, (string Label, int Count)>();
            var bySubStage = new Dictionary<int, (string Label, int Count)>();
            // (phaseId, stageId) -> (phaseLabel, stageLabel, count)
            var byPhaseStage = new Dictionary<(int, int), (string PhaseLabel, string StageLabel, int Count)>();

            var distinctUsers = new HashSet<int>();

            foreach (var l in lessons)
            {
                distinctUsers.Add(l.CreatedUserId);

                // Usuario (ranking de aportes)
                var ulabel = userNameById.TryGetValue(l.CreatedUserId, out var un) ? un : $"Usuario {l.CreatedUserId}";
                Bump(byUser, l.CreatedUserId, ulabel);

                // Proyecto
                if (l.ProjectId.HasValue)
                {
                    var pid = l.ProjectId.Value;
                    var plabel = projDescById.TryGetValue(pid, out var pd) ? pd : $"Proyecto {pid}";
                    Bump(byProject, pid, plabel);
                }

                // Área (lesson_area)
                if (l.LessonAreaId.HasValue)
                {
                    var aid = l.LessonAreaId.Value;
                    var alabel = areaLabelById.TryGetValue(aid, out var an) ? an : $"Área {aid}";
                    Bump(byArea, aid, alabel);
                }

                // Obra / Staff / Oficina Central
                if (l.ObraOficinaStaffId.HasValue)
                {
                    var ooid = l.ObraOficinaStaffId.Value;
                    var oolabel = obraOficinaLabelById.TryGetValue(ooid, out var oon) ? oon : $"Obra/Oficina {ooid}";
                    Bump(byObraOficina, ooid, oolabel);
                }

                // Clasificación
                if (l.LessonAreaId.HasValue && l.CatalogItemId.HasValue)
                {
                    var areaId = l.LessonAreaId.Value;
                    var catId = l.CatalogItemId.Value;

                    var fase = Level(areaId, catId, TypeFase);
                    var etapa = Level(areaId, catId, TypeEtapa);
                    var subetapa = Level(areaId, catId, TypeSubetapa);

                    if (fase.HasValue) Bump(byPhase, fase.Value.Id, fase.Value.Desc);
                    if (subetapa.HasValue) Bump(bySubStage, subetapa.Value.Id, subetapa.Value.Desc);

                    if (fase.HasValue && etapa.HasValue)
                    {
                        var key = (fase.Value.Id, etapa.Value.Id);
                        byPhaseStage.TryGetValue(key, out var cur);
                        byPhaseStage[key] = (fase.Value.Desc, etapa.Value.Desc, cur.Count + 1);
                    }
                }
            }

            // 6. Armar PhaseStage agrupado por fase.
            // Etapas (con lecciones) de cada fase, calculadas a partir de byPhaseStage.
            List<ChartItemDTO> StagesOf(int phaseId) =>
                byPhaseStage
                    .Where(kv => kv.Key.Item1 == phaseId)
                    .Select(kv => new ChartItemDTO
                    {
                        Id = kv.Key.Item2,
                        Label = kv.Value.StageLabel,
                        Value = kv.Value.Count
                    })
                    .OrderByDescending(s => s.Value)
                    .ToList();

            List<PhaseStageChartDTO> phaseStageCharts;

            if (singleAreaId.HasValue)
            {
                // Con UNA área específica seleccionada: TODAS las fases del scope de esa área,
                // en su orden (DisplayOrder), incluyendo las que no tienen lecciones (vacías).
                phaseStageCharts = scope
                    .Where(s => s.LessonAreaId == singleAreaId.Value
                                && string.Equals(s.CatalogTypeName, TypeFase, StringComparison.OrdinalIgnoreCase))
                    .GroupBy(s => s.CatalogItemId)
                    .Select(g => g.OrderBy(s => s.DisplayOrder).First())
                    .OrderBy(s => s.DisplayOrder)
                    .ThenBy(s => s.CatalogItemDescription)
                    .Select(s => new PhaseStageChartDTO
                    {
                        PhaseId = s.CatalogItemId,
                        PhaseLabel = s.CatalogItemDescription,
                        Stages = StagesOf(s.CatalogItemId)
                    })
                    .ToList();
            }
            else
            {
                // Sin área: solo las fases que tienen lecciones (comportamiento actual).
                phaseStageCharts = byPhaseStage
                    .GroupBy(kv => new { PhaseId = kv.Key.Item1, kv.Value.PhaseLabel })
                    .OrderBy(g => g.Key.PhaseLabel)
                    .Select(g => new PhaseStageChartDTO
                    {
                        PhaseId = g.Key.PhaseId,
                        PhaseLabel = g.Key.PhaseLabel,
                        Stages = StagesOf(g.Key.PhaseId)
                    })
                    .ToList();
            }

            return new LessonsDashboardDataDTO
            {
                Summary = new DashboardSummaryDTO
                {
                    TotalLessons = lessons.Count,
                    TotalProjects = byProject.Count,
                    TotalAreas = byArea.Count,
                    TotalPhases = byPhase.Count,
                    TotalUsers = distinctUsers.Count
                },
                LessonsByProject = ToChart(byProject),
                LessonsByArea = ToChart(byArea),
                LessonsByUser = ToChart(byUser),
                LessonsByPhase = ToChart(byPhase),
                LessonsBySubStage = ToChart(bySubStage),
                LessonsByObraOficina = ToChart(byObraOficina),
                LessonsByPhaseAndStage = phaseStageCharts,
                LessonsByMonth = lessonsByMonth,
                PendingPeriodLabel = pendingLabel,
                PendingUsers = pendingUsers
            };
        }

        /// <summary>
        /// Usuarios asignados a recordatorios (user_project activos) que NO han registrado
        /// una lección en el período objetivo (el seleccionado, o el mes actual en hora Lima).
        /// Lista accionable que refleja la misma lógica del cron de recordatorios.
        /// </summary>
        private async Task<(string Label, List<PendingUserDTO> Users)> BuildPendingUsersAsync(
            AppDbContext ctx, DateTimeOffset? periodDate)
        {
            var basis = periodDate ?? DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-5));
            var target = new DateTimeOffset(basis.Year, basis.Month, 1, 0, 0, 0, TimeSpan.Zero);
            var label = MonthYearLabel(target.Year, target.Month);

            var rows = await (
                from up in ctx.UserProject
                join u in ctx.User on up.UserId equals u.UserId
                join p in ctx.Person on u.UserId equals p.UserId
                join pj in ctx.Project on up.ProjectId equals pj.ProjectId
                where up.State && up.Active
                      && !ctx.Lesson.Any(l =>
                            l.CreatedUserId == u.UserId &&
                            l.ProjectId == up.ProjectId &&
                            l.PeriodDate == target &&
                            l.State && l.Active)
                select new { UserId = u.UserId, p.FullName, u.Email, pj.ProjectDescription }
            ).ToListAsync();

            var users = rows
                .GroupBy(x => new { x.UserId, x.FullName, x.Email })
                .Select(g => new PendingUserDTO
                {
                    UserId = g.Key.UserId,
                    FullName = g.Key.FullName,
                    Email = g.Key.Email,
                    Projects = g.Select(x => x.ProjectDescription ?? string.Empty)
                                .Where(d => !string.IsNullOrWhiteSpace(d))
                                .Distinct()
                                .OrderBy(d => d)
                                .ToList()
                })
                .OrderBy(u => u.FullName)
                .ToList();

            return (label, users);
        }


        /// <summary>
        /// Tendencia mensual: lecciones agrupadas por mes (period_date), en orden cronológico.
        /// No aplica el filtro de período (para ver la evolución completa); sí usuario, área y proyectos.
        /// </summary>
        private async Task<List<ChartItemDTO>> BuildMonthlyTrendAsync(AppDbContext ctx, int? userId, List<int>? areaIds, int? obraOficinaStaffId, List<int>? projectIds, string? approvalStatus)
        {
            var q = ctx.Lesson.Where(l => l.Active && l.State && l.PeriodDate != null);
            if (userId.HasValue) q = q.Where(l => l.CreatedUserId == userId.Value);
            if (!string.IsNullOrWhiteSpace(approvalStatus)) q = q.Where(l => l.ApprovalStatus == approvalStatus);
            if (areaIds != null) q = q.Where(l => l.LessonAreaId != null && areaIds.Contains(l.LessonAreaId.Value));
            if (obraOficinaStaffId.HasValue) q = q.Where(l => l.ObraOficinaStaffId == obraOficinaStaffId.Value);
            if (projectIds != null && projectIds.Count > 0)
                q = q.Where(l => l.ProjectId != null && projectIds.Contains(l.ProjectId.Value));

            var monthly = await q
                .GroupBy(l => l.PeriodDate)
                .Select(g => new { Period = g.Key, Count = g.Count() })
                .ToListAsync();

            return monthly
                .Where(m => m.Period.HasValue)
                .OrderBy(m => m.Period!.Value)
                .Select(m => new ChartItemDTO
                {
                    Id = m.Period!.Value.Year * 100 + m.Period.Value.Month,
                    Label = MonthYearLabel(m.Period.Value.Year, m.Period.Value.Month),
                    Value = m.Count
                })
                .ToList();
        }

        public async Task<LessonsDashboardFiltersDTO> GetFiltersAsync()
        {
            using var ctx = _factory.CreateDbContext();

            var periods = await ctx.Lesson
                .Where(l => l.State)
                .Select(l => l.PeriodDate)
                .Distinct()
                .OrderByDescending(d => d)
                .Select(d => new DashboardPeriodDTO { PeriodDate = d })
                .ToListAsync();

            var users = await (
                from u in ctx.User
                join p in ctx.Person on u.UserId equals p.UserId
                where u.Active
                orderby p.FullName
                select new DashboardUserDTO { UserId = u.UserId, FullName = p.FullName }
            ).ToListAsync();

            var areaLabels = await BuildAreaLabelsAsync(ctx, onlyActive: true);
            var areas = areaLabels
                .Select(kv => new DashboardAreaDTO { LessonAreaId = kv.Key, AreaDescription = kv.Value })
                .OrderBy(a => a.AreaDescription)
                .ToList();

            // Proyectos que tienen al menos una lección activa (para el filtro).
            var projects = await (
                from p in ctx.Project
                where p.Active && ctx.Lesson.Any(l => l.ProjectId == p.ProjectId && l.Active && l.State)
                orderby p.ProjectDescription
                select new DashboardProjectDTO
                {
                    ProjectId = p.ProjectId,
                    ProjectDescription = p.ProjectDescription ?? string.Empty
                }
            ).ToListAsync();

            var obraOficinaStaff = await ctx.WorkersObraOficinaStaff
                .Where(c => c.State && c.Active)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new DashboardObraOficinaStaffDTO
                {
                    ObraOficinaStaffId = c.WorkersObraOficinaStaffId,
                    Name = c.Name
                })
                .ToListAsync();

            return new LessonsDashboardFiltersDTO
            {
                Periods = periods,
                Users = users,
                Areas = areas,
                Projects = projects,
                ObraOficinaStaff = obraOficinaStaff
            };
        }

        // ── helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Etiqueta de cada lesson_area = path COMPLETO (raíz &gt; ... &gt; hoja) reconstruido
        /// caminando hacia arriba el árbol area_scope, igual que el desplegable de área de
        /// Lecciones Aprendidas. Desambigua áreas que comparten el mismo nombre de hoja.
        /// </summary>
        private async Task<Dictionary<int, string>> BuildAreaLabelsAsync(AppDbContext ctx, bool onlyActive = false)
        {
            var laQuery = ctx.LessonArea.AsQueryable();
            if (onlyActive) laQuery = laQuery.Where(la => la.Active);

            var lessonAreas = await laQuery
                .Select(la => new { la.LessonAreaId, la.AreaScopeId })
                .ToListAsync();

            // Todos los nodos vivos de area_scope con su nombre y su padre, para
            // reconstruir el path completo desde la raíz hasta la hoja.
            var scopeNodes = await (
                from s in ctx.AreaScope
                join ai in ctx.AreaItem on s.AreaItemId equals ai.AreaItemId
                where s.State && ai.State
                select new { s.AreaScopeId, s.AreaScopeParentId, ai.AreaItemName }
            ).ToListAsync();
            var nodeById = scopeNodes.ToDictionary(n => n.AreaScopeId);

            var result = new Dictionary<int, string>();
            foreach (var la in lessonAreas)
            {
                var parts = new List<string>();
                int? cur = la.AreaScopeId;
                int safety = 50;
                while (cur.HasValue && safety-- > 0 && nodeById.TryGetValue(cur.Value, out var n))
                {
                    parts.Insert(0, n.AreaItemName);
                    cur = n.AreaScopeParentId;
                }
                if (parts.Count > 0)
                    result[la.LessonAreaId] = string.Join(" > ", parts);
            }
            return result;
        }

        private static void Bump(Dictionary<int, (string Label, int Count)> dict, int id, string label)
        {
            dict.TryGetValue(id, out var cur);
            dict[id] = (label, cur.Count + 1);
        }

        private static List<ChartItemDTO> ToChart(Dictionary<int, (string Label, int Count)> dict) =>
            dict.Select(kv => new ChartItemDTO { Id = kv.Key, Label = kv.Value.Label, Value = kv.Value.Count })
                .OrderByDescending(c => c.Value)
                .ToList();

        // Etiqueta de período "Mes Año" en español, ej. "Abril 2026" (igual que el
        // pipe periodLabel del front). Se construye por mes/año para evitar problemas
        // de cultura/zona horaria.
        private static readonly string[] MesesEs =
        {
            "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
            "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre",
        };

        private static string MonthYearLabel(int year, int month) =>
            (month >= 1 && month <= 12) ? $"{MesesEs[month - 1]} {year}" : $"{month:00}-{year}";
    }
}

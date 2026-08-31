using Abril_Backend.Shared.Constants;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Dtos;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Helpers;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Interfaces;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
// Hay dos LessonPeriodDTO: el legacy (compartido con LessonsLearnedDashboard de
// UnidadDeProyectos) y la copia de la feature. En este archivo usamos siempre la
// de la feature.
using LessonPeriodDTO = Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Dtos.LessonPeriodDTO;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Infrastructure.Repositories
{
    /// <summary>
    /// Repo de lecciones. Modelo nuevo: las relaciones de fase/etapa/etc. se
    /// derivan de lesson.catalog_item_id + lesson.lesson_area_id caminando el
    /// árbol scope_item (vía LessonEnrichmentHelper). Las tablas legacy
    /// phase / stage / layer / sub_stage / sub_specialty / partida /
    /// phase_stage_sub_stage_sub_specialty ya no se referencian.
    /// </summary>
    public class LessonRepository : ILessonRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IFileStorageService _fileStorageService;
        private readonly IStorageContainerResolver _containerResolver;
        private readonly ILessonJefeResolver _jefeResolver;

        public LessonRepository(
            IDbContextFactory<AppDbContext> factory,
            IFileStorageService fileStorageService,
            IStorageContainerResolver containerResolver,
            ILessonJefeResolver jefeResolver)
        {
            _factory = factory;
            _fileStorageService = fileStorageService;
            _containerResolver = containerResolver;
            _jefeResolver = jefeResolver;
        }

        // ──────────────────────────────────────────────────────────────────
        // FERIADOS (ventana de revisión de fin de mes)
        // ──────────────────────────────────────────────────────────────────

        // OJO: misma resolución que LessonReminderRepository.GetHolidayDatesAsync
        // (resuelve recurrentes al año pedido). Se replica aquí para no acoplar la
        // feature de lecciones con la de recordatorios.
        public async Task<HashSet<DateOnly>> GetHolidayDatesAsync(int year, int month)
        {
            using var ctx = _factory.CreateDbContext();

            var holidays = await ctx.Holiday
                .Where(h => h.State && h.Active)
                .Select(h => new { h.HolidayDate, h.RecurringYearly })
                .ToListAsync();

            var result = new HashSet<DateOnly>();
            foreach (var h in holidays)
            {
                if (h.RecurringYearly)
                {
                    // Se repite cada año: resolvemos al año solicitado por mes/día.
                    // Guardamos contra fechas inválidas (ej. 29-feb en año no bisiesto).
                    if (h.HolidayDate.Month != month) continue;
                    var day = Math.Min(h.HolidayDate.Day, DateTime.DaysInMonth(year, month));
                    result.Add(new DateOnly(year, month, day));
                }
                else if (h.HolidayDate.Year == year && h.HolidayDate.Month == month)
                {
                    result.Add(h.HolidayDate);
                }
            }
            return result;
        }

        // ──────────────────────────────────────────────────────────────────
        // SELECTORES
        // ──────────────────────────────────────────────────────────────────

        public async Task<LessonDetailDTO?> GetByIdAsync(int id, int currentUserId)
        {
            using var ctx = _factory.CreateDbContext();

            var registro = await (
                from lesson in ctx.Lesson
                join project in ctx.Project on lesson.ProjectId equals project.ProjectId into pj
                from project in pj.DefaultIfEmpty()

                join area in ctx.Area on lesson.AreaId equals area.AreaId into aj
                from area in aj.DefaultIfEmpty()

                join user in ctx.User on lesson.CreatedUserId equals user.UserId into us
                from user in us.DefaultIfEmpty()

                join person in ctx.Person on user.UserId equals person.UserId into pe
                from person in pe.DefaultIfEmpty()

                join ruser in ctx.User on lesson.ReviewedByUserId equals ruser.UserId into rus
                from ruser in rus.DefaultIfEmpty()
                join rperson in ctx.Person on ruser.UserId equals rperson.UserId into rpe
                from rperson in rpe.DefaultIfEmpty()

                join state in ctx.State on lesson.StateId equals state.StateId

                join oo in ctx.WorkersObraOficinaStaff on lesson.ObraOficinaStaffId equals oo.WorkersObraOficinaStaffId into ooj
                from oo in ooj.DefaultIfEmpty()

                where lesson.State == true && lesson.LessonId == id

                select new LessonDetailDTO
                {
                    LessonId = lesson.LessonId,
                    LessonCode = lesson.LessonCode,
                    Period = lesson.Period,
                    ProblemDescription = lesson.ProblemDescription,
                    ReasonDescription = lesson.ReasonDescription,
                    LessonDescription = lesson.LessonDescription,
                    ImpactDescription = lesson.ImpactDescription,
                    ProjectId = lesson.ProjectId,
                    ProjectDescription = project != null ? project.ProjectDescription : null,
                    AreaId = lesson.AreaId,
                    AreaDescription = area != null ? area.AreaDescription : null,
                    LessonAreaId = lesson.LessonAreaId,
                    CatalogItemId = lesson.CatalogItemId,
                    ObraOficinaStaffId = lesson.ObraOficinaStaffId,
                    ObraOficinaStaffName = oo != null ? oo.Name : null,
                    StateId = lesson.StateId,
                    StateDescription = state.StateDescription,
                    ApprovalStatus = lesson.ApprovalStatus,
                    RejectionComment = lesson.RejectionComment,
                    ReviewedByFullName = rperson != null ? rperson.FullName : null,
                    CreatedDateTime = lesson.CreatedDateTime,
                    CreatedUserId = lesson.CreatedUserId,
                    CreatedUserFullName = person != null ? person.FullName : null,
                    UpdatedDateTime = lesson.UpdatedDateTime,
                    UpdatedUserId = lesson.UpdatedUserId,
                    Active = lesson.Active
                }
            ).FirstOrDefaultAsync();

            if (registro == null) return null;

            // CanReview + ReviewedByFullName para lecciones PENDIENTE:
            // el jefe se obtiene por worker_lesson_jefe_id del autor (sin requerir
            // que el jefe tenga usuario).
            if (registro.ApprovalStatus == "PENDIENTE")
            {
                // Nombre del jefe: autor → worker → worker_lesson_jefe_id → persona del jefe.
                var jefeInfo = await (
                    from authorPerson in ctx.Person
                    join authorWorker in ctx.Worker on authorPerson.PersonId equals authorWorker.PersonId
                    join jefeWorker in ctx.Worker on authorWorker.WorkerLessonJefeId equals jefeWorker.Id
                    join jefePerson in ctx.Person on jefeWorker.PersonId equals jefePerson.PersonId
                    where authorPerson.UserId == registro.CreatedUserId
                    select new { jefePerson.FullName, jefeWorker.Id, JefeUserId = jefePerson.UserId }
                ).FirstOrDefaultAsync();

                if (jefeInfo != null)
                {
                    registro.ReviewedByFullName = jefeInfo.FullName;

                    if (registro.CreatedUserId != currentUserId && jefeInfo.JefeUserId.HasValue)
                    {
                        registro.CanReview = jefeInfo.JefeUserId.Value == currentUserId
                            && await _jefeResolver.CanReviewProjectAsync(currentUserId, registro.ProjectId);
                    }
                }
            }

            registro.Images = await (
                from img in ctx.LessonImages
                join imagetype in ctx.ImageType on img.ImageTypeId equals imagetype.ImageTypeId
                where img.LessonId == id && img.State == true
                select new LessonImageDTO
                {
                    LessonImageId = img.LessonImageId,
                    ImageUrl = img.ImageUrl,
                    LessonId = img.LessonId,
                    ImageTypeId = img.ImageTypeId,
                    ImageTypeDescription = imagetype.ImageTypeDescription
                }
            ).ToListAsync();

            var enrichments = await LessonEnrichmentHelper.ComputeAsync(
                ctx,
                new[] { (registro.LessonId, registro.LessonAreaId, registro.CatalogItemId) }
            );
            if (enrichments.TryGetValue(registro.LessonId, out var e))
            {
                if (e.AreaDescription != null) registro.AreaDescription = e.AreaDescription;
                if (e.AreaListDescription != null) registro.AreaListDescription = e.AreaListDescription;
                if (e.ClassificationSegments != null && e.ClassificationSegments.Count > 0)
                    registro.ClassificationSegments = e.ClassificationSegments;
            }

            return registro;
        }

        public Task<PagedResult<LessonListDTO>> GetLessonsFilterPaged(
            DateTimeOffset? periodDate,
            int? stateId,
            int? projectId,
            int? areaId,
            int? userId,
            int page,
            int pageSize)
            => GetLessonsFilterPagedInternal(periodDate, stateId, projectId, areaId, null, null, userId, null, null, null, false, 0, page, pageSize);

        private async Task<PagedResult<LessonListDTO>> GetLessonsFilterPagedInternal(
            DateTimeOffset? periodDate,
            int? stateId,
            int? projectId,
            int? areaId,
            List<int>? lessonAreaIds,
            int? obraOficinaStaffId,
            int? userId,
            int? reviewerWorkerId,
            List<int>? catalogItemIds,
            string? approvalStatus,
            bool onlyMyPendingReview,
            int currentUserId,
            int page,
            int pageSize)
        {
            using var ctx = _factory.CreateDbContext();

            var query = ctx.Lesson.Where(x => x.Active).AsQueryable();

            if (periodDate.HasValue) query = query.Where(x => x.PeriodDate == periodDate);
            if (stateId.HasValue) query = query.Where(x => x.StateId == stateId.Value);
            if (projectId.HasValue) query = query.Where(x => x.ProjectId == projectId.Value);
            // Filtro de área: el conjunto (cascada del listado) tiene prioridad sobre el simple.
            if (lessonAreaIds != null && lessonAreaIds.Count > 0)
                query = query.Where(x => x.LessonAreaId != null && lessonAreaIds.Contains(x.LessonAreaId.Value));
            else if (areaId.HasValue)
                query = query.Where(x => x.LessonAreaId == areaId.Value);
            // Obra/Oficina: antes se filtraba bajando al nodo Obra_Oficina del arbol
            // de areas; ahora es una columna propia de la leccion.
            if (obraOficinaStaffId.HasValue)
                query = query.Where(x => x.ObraOficinaStaffId == obraOficinaStaffId.Value);
            if (userId.HasValue) query = query.Where(x => x.CreatedUserId == userId.Value);

            // Filtro por revisor: la lección matchea si su AUTOR tiene asignado a este
            // worker como revisor (worker.worker_lesson_jefe_id). Resolvemos primero los
            // user_id de los autores cuyo revisor asignado es el seleccionado.
            if (reviewerWorkerId.HasValue)
            {
                var revieweeUserIds = await (
                    from w in ctx.Worker
                    join p in ctx.Person on w.PersonId equals p.PersonId
                    where w.WorkerLessonJefeId == reviewerWorkerId.Value && p.UserId != null
                    select p.UserId!.Value
                ).Distinct().ToListAsync();

                if (revieweeUserIds.Count == 0)
                    return new PagedResult<LessonListDTO> { Page = page, PageSize = pageSize, TotalRecords = 0, TotalPages = 0, Data = new List<LessonListDTO>() };

                query = query.Where(x => revieweeUserIds.Contains(x.CreatedUserId));
            }

            if (!string.IsNullOrWhiteSpace(approvalStatus)) query = query.Where(x => x.ApprovalStatus == approvalStatus);

            // "Pendientes de mi revisión": lecciones PENDIENTES de los subordinados
            // (autores cuyo Jefe es el usuario actual).
            if (onlyMyPendingReview)
            {
                var subordinateUserIds = await _jefeResolver.GetSubordinateUserIdsAsync(currentUserId);
                if (subordinateUserIds.Count == 0)
                    return new PagedResult<LessonListDTO> { Page = page, PageSize = pageSize, TotalRecords = 0, TotalPages = 0, Data = new List<LessonListDTO>() };
                query = query.Where(x => x.ApprovalStatus == "PENDIENTE" && subordinateUserIds.Contains(x.CreatedUserId));

                // Si el revisor es Residente, acotar a sus proyectos asignados (user_project).
                var residenteProjectScope = await _jefeResolver.GetResidenteProjectScopeAsync(currentUserId);
                if (residenteProjectScope != null)
                    query = query.Where(x => x.ProjectId.HasValue && residenteProjectScope.Contains(x.ProjectId.Value));
            }

            // Filtro por catalog_item_ids: una lección matchea si TODOS los
            // catalog_item_ids seleccionados aparecen en el ancestor chain del
            // (lesson_area_id, catalog_item_id) de la lección en scope_item.
            //
            // OJO: el mapa se indexa por PAR (lesson_area_id, catalog_item_id)
            // — NO solo por catalog_item_id. Si el mismo catalog_item aparece
            // bajo padres distintos (PROYECTO vs PRE-ANTEPROYECTO, etc.), cada
            // par tiene su propia cadena de ancestros aislada.
            if (catalogItemIds != null && catalogItemIds.Count > 0)
            {
                var ancestorByPair = await BuildAncestorCatalogItemsByPairAsync(ctx);
                var validPairs = ancestorByPair
                    .Where(kv => catalogItemIds.All(id => kv.Value.Contains(id)))
                    .Select(kv => kv.Key)
                    .ToHashSet();
                if (validPairs.Count == 0)
                {
                    return new PagedResult<LessonListDTO>
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalRecords = 0,
                        TotalPages = 0,
                        Data = new List<LessonListDTO>()
                    };
                }

                // Bajamos los pares (lesson_id, lesson_area_id, catalog_item_id) de
                // las lecciones que ya pasaron los otros filtros, y refinamos en
                // memoria por par exacto. Luego aplicamos WHERE lesson_id IN (...)
                // contra la query original para preservar paginación.
                var candidates = await query
                    .Where(l => l.LessonAreaId.HasValue && l.CatalogItemId.HasValue)
                    .Select(l => new { l.LessonId, LaId = l.LessonAreaId!.Value, CiId = l.CatalogItemId!.Value })
                    .ToListAsync();

                var matchingIds = candidates
                    .Where(c => validPairs.Contains((c.LaId, c.CiId)))
                    .Select(c => c.LessonId)
                    .ToList();

                if (matchingIds.Count == 0)
                {
                    return new PagedResult<LessonListDTO>
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalRecords = 0,
                        TotalPages = 0,
                        Data = new List<LessonListDTO>()
                    };
                }
                query = query.Where(l => matchingIds.Contains(l.LessonId));
            }

            var totalRecords = await query.CountAsync();

            var registros = await (
                from lesson in query
                join project in ctx.Project on lesson.ProjectId equals project.ProjectId into pj
                from project in pj.DefaultIfEmpty()
                join area in ctx.Area on lesson.AreaId equals area.AreaId into aj
                from area in aj.DefaultIfEmpty()
                join user in ctx.User on lesson.CreatedUserId equals user.UserId into us
                from user in us.DefaultIfEmpty()
                join person in ctx.Person on user.UserId equals person.UserId into pe
                from person in pe.DefaultIfEmpty()
                join state in ctx.State on lesson.StateId equals state.StateId
                join oo in ctx.WorkersObraOficinaStaff on lesson.ObraOficinaStaffId equals oo.WorkersObraOficinaStaffId into ooj
                from oo in ooj.DefaultIfEmpty()
                orderby lesson.CreatedDateTime descending
                select new LessonListDTO
                {
                    LessonId = lesson.LessonId,
                    LessonCode = lesson.LessonCode,
                    Period = lesson.Period,
                    ProblemDescription = lesson.ProblemDescription,
                    ReasonDescription = lesson.ReasonDescription,
                    LessonDescription = lesson.LessonDescription,
                    ImpactDescription = lesson.ImpactDescription,
                    ProjectId = lesson.ProjectId,
                    ProjectDescription = project != null ? project.ProjectDescription : null,
                    AreaId = lesson.AreaId,
                    AreaDescription = area != null ? area.AreaDescription : null,
                    LessonAreaId = lesson.LessonAreaId,
                    CatalogItemId = lesson.CatalogItemId,
                    ObraOficinaStaffId = lesson.ObraOficinaStaffId,
                    ObraOficinaStaffName = oo != null ? oo.Name : null,
                    StateId = lesson.StateId,
                    StateDescription = state.StateDescription,
                    ApprovalStatus = lesson.ApprovalStatus,
                    CreatedDateTime = lesson.CreatedDateTime,
                    CreatedUserId = lesson.CreatedUserId,
                    CreatedUserFullName = person != null ? person.FullName : null,
                    UpdatedDateTime = lesson.UpdatedDateTime,
                    UpdatedUserId = lesson.UpdatedUserId,
                    Active = lesson.Active,
                    Images = new List<LessonImageDTO>()
                })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            await AttachImagesAndEnrichAsync(ctx, registros);

            return new PagedResult<LessonListDTO>
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Data = registros
            };
        }

        public async Task<LessonsPagedWithFiltersDTO> GetPagedWithFiltersAsync(LessonFilterDTO filter)
        {
            if (filter.Page < 1) filter.Page = 1;
            int pageSize = filter.PageSize > 0 ? filter.PageSize : 10;

            using var ctx = _factory.CreateDbContext();

            // Lecciones paginadas + enriched (incluye filtro por catalog_item_ids)
            var paged = await GetLessonsFilterPagedInternal(
                filter.PeriodDate, filter.StateId, filter.ProjectId,
                filter.AreaId, filter.LessonAreaIds, filter.ObraOficinaStaffId,
                filter.UserId, filter.ReviewerWorkerId,
                filter.CatalogItemIds, filter.ApprovalStatus, filter.OnlyMyPendingReview,
                filter.CurrentUserId, filter.Page, pageSize);

            // ── Filters dropdowns ───────────────────────────────────────────
            // Áreas: solo las ramas ACTIVAS de lesson_area (lo que aparece en
            // /mejora-continua/configuration/areas con toggle on). El AreaId
            // del filtro es lesson_area_id.
            var activeLessonAreas = await ctx.LessonArea
                .Where(la => la.Active)
                .Select(la => new { la.LessonAreaId, la.AreaScopeId, la.IncludeInForm, la.IncludeAsIndependent })
                .ToListAsync();

            // Se carga una sola vez: lo usan tanto las etiquetas de área del filtro como
            // la resolución del área por defecto del formulario.
            var scopeNodes = await (
                from s in ctx.AreaScope
                join ai in ctx.AreaItem on s.AreaItemId equals ai.AreaItemId
                where s.State && ai.State
                select new { s.AreaScopeId, s.AreaScopeParentId, s.AreaItemId, ai.AreaItemName }
            ).ToListAsync();
            var nodeById = scopeNodes.ToDictionary(n => n.AreaScopeId);

            List<AreaSimpleDTO> areas;
            if (activeLessonAreas.Count == 0)
            {
                areas = new List<AreaSimpleDTO>();
            }
            else
            {
                areas = activeLessonAreas
                    .Select(la =>
                    {
                        var parts = new List<string>();
                        int? cur = la.AreaScopeId;
                        int safety = 50;
                        while (cur.HasValue && safety-- > 0 && nodeById.TryGetValue(cur.Value, out var n))
                        {
                            parts.Insert(0, n.AreaItemName);
                            cur = n.AreaScopeParentId;
                        }
                        return new AreaSimpleDTO
                        {
                            AreaId = la.LessonAreaId,
                            AreaDescription = string.Join(" > ", parts)
                        };
                    })
                    .Where(a => !string.IsNullOrWhiteSpace(a.AreaDescription))
                    .OrderBy(a => a.AreaDescription)
                    .ToList();
            }

            var projects = await ctx.Project
                .Where(p => p.Active && p.State && !ctx.ProyectoFiltro.Any(f => f.ProjectId == p.ProjectId && f.FuncionalidadId == ProyectoFiltroFuncionalidades.LeccionesAprendidas && !f.Active))
                .OrderBy(p => p.ProjectDescription)
                .Select(p => new ProjectSimpleDTO
                {
                    ProjectId = p.ProjectId,
                    ProjectDescription = p.ProjectDescription ?? string.Empty
                })
                .ToListAsync();

            var periods = await ctx.Lesson
                .Where(l => l.State)
                .Select(l => l.PeriodDate)
                .Distinct()
                .OrderByDescending(d => d)
                .Select(d => new LessonPeriodDTO { PeriodDate = d })
                .ToListAsync();

            var users = await (
                from u in ctx.User
                join pe in ctx.Person on u.UserId equals pe.UserId
                where u.Active
                orderby pe.FullName
                select new UserFilterDTO
                {
                    UserId = u.UserId,
                    FullName = pe.FullName
                }
            ).ToListAsync();

            // Revisores: workers asignados como revisor (worker_lesson_jefe_id) de
            // algún autor que tenga lecciones. Solo se listan los que realmente
            // aplican, para que el filtro no muestre revisores sin resultados.
            var reviewersRaw = await (
                from l in ctx.Lesson
                where l.State
                join p in ctx.Person on l.CreatedUserId equals p.UserId
                join w in ctx.Worker on p.PersonId equals w.PersonId
                join jw in ctx.Worker on w.WorkerLessonJefeId equals jw.Id
                join jp in ctx.Person on jw.PersonId equals jp.PersonId
                select new { jw.Id, jp.FullName }
            ).Distinct().ToListAsync();

            var reviewers = reviewersRaw
                .Select(r => new LessonReviewerFilterDTO { WorkerId = r.Id, FullName = r.FullName })
                .OrderBy(r => r.FullName)
                .ToList();

            // Filtros dinámicos por catalog_type: solo catalog_items que existen
            // en scope_item activo (es decir, que están realmente en uso).
            var categories = await (
                from si in ctx.ScopeItem
                join ci in ctx.CatalogItem on si.CatalogItemId equals ci.CatalogItemId
                join ct in ctx.CatalogType on ci.CatalogTypeId equals ct.CatalogTypeId
                where si.Active && ci.Active && ct.Active
                select new
                {
                    ct.CatalogTypeId,
                    ct.CatalogTypeName,
                    ci.CatalogItemId,
                    ci.CatalogItemDescription
                }
            ).Distinct().ToListAsync();

            var categoryGroups = categories
                .GroupBy(x => new { x.CatalogTypeId, x.CatalogTypeName })
                .OrderBy(g => g.Key.CatalogTypeId)
                .Select(g => new CatalogFilterGroupDTO
                {
                    CatalogTypeId = g.Key.CatalogTypeId,
                    CatalogTypeName = g.Key.CatalogTypeName,
                    Items = g
                        .GroupBy(x => x.CatalogItemId)
                        .Select(ig => new CatalogFilterItemDTO
                        {
                            CatalogItemId = ig.Key,
                            CatalogItemDescription = ig.First().CatalogItemDescription
                        })
                        .OrderBy(i => i.CatalogItemDescription)
                        .ToList()
                })
                .ToList();

            // Catalogo Obra / Staff / Oficina Central: alimenta el filtro del listado y el
            // desplegable del formulario. Reemplaza al nivel "subarea" que antes salia del
            // arbol de areas (nodos de tipo Obra_Oficina, ya eliminados).
            var obraOficinaStaff = await ctx.WorkersObraOficinaStaff
                .Where(c => c.State && c.Active)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new ObraOficinaStaffOptionDTO
                {
                    ObraOficinaStaffId = c.WorkersObraOficinaStaffId,
                    Name = c.Name
                })
                .ToListAsync();

            // Sugerencias para el formulario: lo que tenga el trabajador que está registrando.
            // Con estas dos, la cascada de área y el desplegable de Obra/Oficina llegan
            // preseleccionados y al usuario solo le queda elegir el proyecto.
            var perfil = await (
                from p in ctx.Person
                join w in ctx.Worker on p.PersonId equals w.PersonId
                where p.UserId == filter.CurrentUserId
                orderby (w.ObraOficinaStaffId != null ? 0 : 1), w.Id
                select new { w.ObraOficinaStaffId, w.AreaScopeId }
            ).FirstOrDefaultAsync();

            // lesson_area_ids con plantilla: una lesson_area sin scope_item no aparece
            // en el formulario, así que tampoco puede ser el área por defecto.
            var lessonAreaIdsConPlantilla = (await ctx.ScopeItem
                    .Where(si => si.Active)
                    .Select(si => si.LessonAreaId)
                    .Distinct()
                    .ToListAsync())
                .ToHashSet();

            var defaultLessonAreaId = perfil?.AreaScopeId is int workerScopeId
                ? ResolveDefaultLessonAreaId(
                    workerScopeId,
                    scopeNodes.Select(n => (n.AreaScopeId, n.AreaScopeParentId, n.AreaItemId)).ToList(),
                    activeLessonAreas
                        .Where(la => la.IncludeInForm && lessonAreaIdsConPlantilla.Contains(la.LessonAreaId))
                        .Select(la => (la.AreaScopeId, la.LessonAreaId, la.IncludeAsIndependent))
                        .ToList())
                : null;

            return new LessonsPagedWithFiltersDTO
            {
                Paged = paged,
                Filters = new LessonFiltersFormDataDTO
                {
                    Areas = areas,
                    Projects = projects,
                    Periods = periods,
                    Users = users,
                    Reviewers = reviewers,
                    Categories = categoryGroups,
                    ObraOficinaStaff = obraOficinaStaff,
                    DefaultObraOficinaStaffId = perfil?.ObraOficinaStaffId,
                    DefaultLessonAreaId = defaultLessonAreaId
                }
            };
        }

        /// <summary>
        /// Área del formulario que le corresponde por defecto a un trabajador según el nodo
        /// del árbol que tiene asignado (<c>workers.area_scope_id</c>).
        ///
        /// La cascada del formulario solo acepta HOJAS, así que se busca la hoja más cercana
        /// subiendo desde su nodo:
        ///   1. El propio nodo, si es hoja del árbol del formulario.
        ///   2. Un descendiente que repita el mismo <c>area_item</c> del nodo — la convención
        ///      "&lt;Área&gt; puro" (p. ej. "Unidad de Proyectos &gt; Unidad de Proyectos"),
        ///      que es donde va el personal del área que no pertenece a ninguna subárea.
        ///   3. Se sube al padre y se repite.
        /// Un área marcada "independiente" siempre cuenta como hoja: el formulario la promueve
        /// al primer nivel y no despliega sus hijas.
        ///
        /// Devuelve null si no hay match — en ese caso el usuario elige el área a mano.
        /// </summary>
        private static int? ResolveDefaultLessonAreaId(
            int workerAreaScopeId,
            IReadOnlyList<(int AreaScopeId, int? ParentId, int AreaItemId)> nodes,
            IReadOnlyList<(int AreaScopeId, int LessonAreaId, bool Independiente)> formAreas)
        {
            if (nodes.Count == 0 || formAreas.Count == 0) return null;

            var byId = nodes.ToDictionary(n => n.AreaScopeId);
            var childrenByParent = nodes
                .Where(n => n.ParentId.HasValue)
                .GroupBy(n => n.ParentId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(x => x.AreaScopeId).ToList());

            var formByScope = formAreas
                .GroupBy(f => f.AreaScopeId)
                .ToDictionary(g => g.Key, g => g.First());

            List<int> Descendants(int id)
            {
                var result = new List<int>();
                var pending = new Stack<int>();
                var seen = new HashSet<int>();
                pending.Push(id);
                while (pending.Count > 0)
                {
                    if (!childrenByParent.TryGetValue(pending.Pop(), out var hijos)) continue;
                    foreach (var h in hijos)
                    {
                        if (!seen.Add(h)) continue;
                        result.Add(h);
                        pending.Push(h);
                    }
                }
                return result;
            }

            bool EsHoja(int scopeId) =>
                (formByScope.TryGetValue(scopeId, out var f) && f.Independiente)
                || !Descendants(scopeId).Any(formByScope.ContainsKey);

            int? cur = workerAreaScopeId;
            var visitados = new HashSet<int>();
            while (cur.HasValue && visitados.Add(cur.Value) && byId.TryGetValue(cur.Value, out var node))
            {
                if (formByScope.TryGetValue(node.AreaScopeId, out var propio) && EsHoja(node.AreaScopeId))
                    return propio.LessonAreaId;

                foreach (var d in Descendants(node.AreaScopeId))
                {
                    if (byId[d].AreaItemId != node.AreaItemId) continue;
                    if (formByScope.TryGetValue(d, out var puro) && EsHoja(d))
                        return puro.LessonAreaId;
                }

                cur = node.ParentId;
            }

            return null;
        }

        /// <summary>
        /// Construye un mapa <c>(lesson_area_id, catalog_item_id) → ancestor
        /// catalog_item_ids</c> (incluyendo el propio catalog_item) caminando
        /// el árbol scope_item.
        ///
        /// Por qué la clave es el PAR y no solo catalog_item_id: el mismo
        /// catalog_item puede aparecer en scope_item bajo padres distintos
        /// (p. ej. "ARQUITECTURA" como subetapa de PRE-ANTEPROYECTO y también
        /// de PROYECTO). Si la clave fuera solo catalog_item_id, los ancestros
        /// se unirían y un filtro por PROYECTO matchearía lecciones que en
        /// realidad están bajo PRE-ANTEPROYECTO. Al usar (lesson_area_id,
        /// catalog_item_id) cada par tiene su cadena de ancestros aislada.
        ///
        /// AMBIGÜEDAD: como <c>lesson</c> solo guarda <c>(lesson_area_id,
        /// catalog_item_id)</c> y no <c>scope_item_id</c>, un mismo par puede
        /// mapear a varios scope_items dentro de la misma lesson_area cuando
        /// el catalog_item se reutiliza bajo padres distintos. No tenemos forma
        /// de saber cuál posición eligió el usuario, así que aplicamos la regla
        /// "el scope_item de MENOR id gana" — determinística y consistente con
        /// <c>LessonEnrichmentHelper</c> y con <c>LessonsDashboardRepository</c>,
        /// que también ordenan por <c>ScopeItemId</c> ascendente. Cualquier
        /// cambio de criterio (DESC, etc.) debe replicarse en los tres lugares
        /// para que dashboard, listado y filtro reporten lo mismo.
        /// </summary>
        private static async Task<Dictionary<(int LessonAreaId, int CatalogItemId), HashSet<int>>>
            BuildAncestorCatalogItemsByPairAsync(AppDbContext ctx)
        {
            var allScope = await ctx.ScopeItem
                .Where(s => s.Active)
                .OrderBy(s => s.ScopeItemId)
                .Select(s => new { s.ScopeItemId, s.LessonAreaId, s.CatalogItemId, s.ScopeItemParentId })
                .ToListAsync();
            var byId = allScope.ToDictionary(s => s.ScopeItemId);

            var result = new Dictionary<(int, int), HashSet<int>>();
            foreach (var s in allScope)
            {
                var key = (s.LessonAreaId, s.CatalogItemId);
                if (result.ContainsKey(key)) continue; // primer scope_item (menor id) gana

                var set = new HashSet<int>();
                int? cur = s.ScopeItemId;
                int safety = 50;
                while (cur.HasValue && safety-- > 0 && byId.TryGetValue(cur.Value, out var n))
                {
                    set.Add(n.CatalogItemId);
                    cur = n.ScopeItemParentId;
                }
                result[key] = set;
            }
            return result;
        }

        public async Task<List<LessonListDTO>> GetLessonsFilterAsync(
            string? period,
            int? stateId,
            int? projectId,
            int? areaId,
            int? userId,
            List<int>? lessonAreaIds = null,
            int? obraOficinaStaffId = null)
        {
            using var ctx = _factory.CreateDbContext();

            var query = ctx.Lesson.Where(x => x.Active).AsQueryable();

            if (!string.IsNullOrWhiteSpace(period)) query = query.Where(x => x.Period == period);
            if (stateId.HasValue) query = query.Where(x => x.StateId == stateId.Value);
            if (projectId.HasValue) query = query.Where(x => x.ProjectId == projectId.Value);
            if (lessonAreaIds != null && lessonAreaIds.Count > 0)
                query = query.Where(x => x.LessonAreaId != null && lessonAreaIds.Contains(x.LessonAreaId.Value));
            else if (areaId.HasValue)
                query = query.Where(x => x.LessonAreaId == areaId.Value);
            if (obraOficinaStaffId.HasValue)
                query = query.Where(x => x.ObraOficinaStaffId == obraOficinaStaffId.Value);
            if (userId.HasValue) query = query.Where(x => x.CreatedUserId == userId.Value);

            var registros = await (
                from lesson in query
                join project in ctx.Project on lesson.ProjectId equals project.ProjectId into pj
                from project in pj.DefaultIfEmpty()
                join area in ctx.Area on lesson.AreaId equals area.AreaId into aj
                from area in aj.DefaultIfEmpty()
                join user in ctx.User on lesson.CreatedUserId equals user.UserId into us
                from user in us.DefaultIfEmpty()
                join person in ctx.Person on user.UserId equals person.UserId into pe
                from person in pe.DefaultIfEmpty()
                join state in ctx.State on lesson.StateId equals state.StateId
                join oo in ctx.WorkersObraOficinaStaff on lesson.ObraOficinaStaffId equals oo.WorkersObraOficinaStaffId into ooj
                from oo in ooj.DefaultIfEmpty()
                orderby lesson.CreatedDateTime descending
                select new LessonListDTO
                {
                    LessonId = lesson.LessonId,
                    LessonCode = lesson.LessonCode,
                    Period = lesson.Period,
                    ProblemDescription = lesson.ProblemDescription,
                    ReasonDescription = lesson.ReasonDescription,
                    LessonDescription = lesson.LessonDescription,
                    ImpactDescription = lesson.ImpactDescription,
                    ProjectId = lesson.ProjectId,
                    ProjectDescription = project != null ? project.ProjectDescription : null,
                    AreaId = lesson.AreaId,
                    AreaDescription = area != null ? area.AreaDescription : null,
                    LessonAreaId = lesson.LessonAreaId,
                    CatalogItemId = lesson.CatalogItemId,
                    ObraOficinaStaffId = lesson.ObraOficinaStaffId,
                    ObraOficinaStaffName = oo != null ? oo.Name : null,
                    StateId = lesson.StateId,
                    StateDescription = state.StateDescription,
                    ApprovalStatus = lesson.ApprovalStatus,
                    CreatedDateTime = lesson.CreatedDateTime,
                    CreatedUserId = lesson.CreatedUserId,
                    CreatedUserFullName = person != null ? person.FullName : null,
                    UpdatedDateTime = lesson.UpdatedDateTime,
                    UpdatedUserId = lesson.UpdatedUserId,
                    Active = lesson.Active,
                    Images = new List<LessonImageDTO>()
                }
            ).ToListAsync();

            await AttachImagesAndEnrichAsync(ctx, registros);
            return registros;
        }

        // ──────────────────────────────────────────────────────────────────
        // MUTACIONES
        // ──────────────────────────────────────────────────────────────────

        public async Task<object?> CreateAsync(LessonCreateDTO dto, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            int? catalogItemId = dto.CatalogItemId > 0 ? dto.CatalogItemId : null;
            if (catalogItemId.HasValue)
            {
                var exists = await ctx.CatalogItem.AnyAsync(c => c.CatalogItemId == catalogItemId.Value && c.Active);
                if (!exists) return null;
            }

            var autoApprove = await (
                from p in ctx.Person
                join w in ctx.Worker on p.PersonId equals w.PersonId
                where p.UserId == userId
                select w.AutoApproveLesson
            ).FirstOrDefaultAsync();

            var now = DateTimeOffset.UtcNow;
            var lesson = new Lesson
            {
                Period = now.ToString("MM-yyyy"),
                PeriodDate = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero),
                ProblemDescription = dto.ProblemDescription,
                ReasonDescription = dto.ReasonDescription,
                LessonDescription = dto.LessonDescription,
                ImpactDescription = dto.ImpactDescription,
                ProjectId = dto.ProjectId,
                AreaId = dto.AreaId,
                CatalogItemId = catalogItemId,
                LessonAreaId = dto.LessonAreaId,
                ObraOficinaStaffId = dto.ObraOficinaStaffId,
                StateId = 2,
                ApprovalStatus = autoApprove ? "APROBADA" : "PENDIENTE",
                ReviewedByUserId = autoApprove ? userId : null,
                ReviewedAt = autoApprove ? now : null,
                CreatedDateTime = now,
                CreatedUserId = userId,
                UpdatedDateTime = null,
                Active = true,
                State = true
            };

            ctx.Lesson.Add(lesson);
            await ctx.SaveChangesAsync();

            if (dto.OpportunityImages?.Any() == true)
                await SaveImagesAsync(dto.OpportunityImages, lesson.LessonId, 1, ctx);
            if (dto.ImprovementImages?.Any() == true)
                await SaveImagesAsync(dto.ImprovementImages, lesson.LessonId, 2, ctx);

            return lesson.LessonId;
        }

        public async Task<bool> DeleteSoftAsync(int lessonId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var lesson = await ctx.Lesson
                .FirstOrDefaultAsync(u => u.LessonId == lessonId && u.State == true);
            if (lesson == null) return false;

            // Solo el autor puede eliminar su lección (el jefe solo aprueba/rechaza/edita).
            if (lesson.CreatedUserId != userId)
                throw new AbrilException("Solo el autor puede eliminar la lección.", 403);

            lesson.State = false;
            lesson.Active = false;
            lesson.UpdatedDateTime = DateTime.UtcNow;
            lesson.UpdatedUserId = userId;

            await ctx.LessonImages
                .Where(x => x.LessonId == lessonId && x.State == true)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.State, false)
                    .SetProperty(x => x.Active, false)
                    .SetProperty(x => x.UpdatedDateTime, DateTime.UtcNow)
                    .SetProperty(x => x.UpdatedUserId, userId)
                );

            await ctx.SaveChangesAsync();
            return true;
        }

        public Task<LessonReviewResultDTO> ApproveAsync(int lessonId, int currentUserId)
            => SetReviewAsync(lessonId, currentUserId, approved: true, comment: null);

        public Task<LessonReviewResultDTO> RejectAsync(int lessonId, int currentUserId, string? comment)
            => SetReviewAsync(lessonId, currentUserId, approved: false, comment: comment);

        /// <summary>
        /// Aprueba o rechaza una lección. Solo el Jefe del autor puede hacerlo y solo
        /// mientras esté PENDIENTE. Devuelve el contacto del autor para notificarle.
        /// </summary>
        private async Task<LessonReviewResultDTO> SetReviewAsync(int lessonId, int currentUserId, bool approved, string? comment)
        {
            using var ctx = _factory.CreateDbContext();

            var lesson = await ctx.Lesson.FirstOrDefaultAsync(l => l.LessonId == lessonId && l.State);
            if (lesson == null) throw new AbrilException("Lección no encontrada.", 404);
            if (lesson.ApprovalStatus != "PENDIENTE")
                throw new AbrilException("La lección ya fue revisada.", 400);

            var jefeUserId = await _jefeResolver.ResolveJefeUserIdAsync(lesson.CreatedUserId);
            if (!jefeUserId.HasValue || jefeUserId.Value != currentUserId)
                throw new AbrilException("No tienes permiso para revisar esta lección.", 403);

            // Si el revisor es Residente, solo puede revisar lecciones de los proyectos
            // que tiene asignados en user_project.
            if (!await _jefeResolver.CanReviewProjectAsync(currentUserId, lesson.ProjectId))
                throw new AbrilException("Como Residente, solo puedes revisar lecciones de los proyectos que tienes asignados.", 403);

            lesson.ApprovalStatus = approved ? "APROBADA" : "RECHAZADA";
            lesson.RejectionComment = approved ? null : comment;
            lesson.ReviewedByUserId = currentUserId;
            lesson.ReviewedAt = DateTimeOffset.UtcNow;
            lesson.UpdatedDateTime = DateTimeOffset.UtcNow;
            lesson.UpdatedUserId = currentUserId;
            await ctx.SaveChangesAsync();

            var contact = await (
                from u in ctx.User
                join p in ctx.Person on u.UserId equals p.UserId into pe
                from p in pe.DefaultIfEmpty()
                where u.UserId == lesson.CreatedUserId
                select new { u.Email, FullName = p != null ? p.FullName : null }
            ).FirstOrDefaultAsync();

            return new LessonReviewResultDTO
            {
                LessonId = lesson.LessonId,
                LessonCode = lesson.LessonCode,
                CreatorEmail = contact?.Email,
                CreatorFullName = contact?.FullName
            };
        }

        /// <summary>
        /// Edición de una lección (solo el autor). Al guardar, la lección vuelve a
        /// PENDIENTE y se limpia la revisión previa.
        /// </summary>
        public async Task<bool> UpdateAsync(int lessonId, LessonUpdateDTO dto, int currentUserId)
        {
            using var ctx = _factory.CreateDbContext();

            var lesson = await ctx.Lesson.FirstOrDefaultAsync(l => l.LessonId == lessonId && l.State);
            if (lesson == null) throw new AbrilException("Lección no encontrada.", 404);

            // Puede editar: el AUTOR, o la jefatura ACTIVA resuelta del autor (la misma
            // que puede aprobar/rechazar; si es Residente, acotada a sus proyectos).
            if (lesson.CreatedUserId != currentUserId)
            {
                var jefeUserId = await _jefeResolver.ResolveJefeUserIdAsync(lesson.CreatedUserId);
                var esRevisorActivo = jefeUserId.HasValue
                    && jefeUserId.Value == currentUserId
                    && await _jefeResolver.CanReviewProjectAsync(currentUserId, lesson.ProjectId);
                if (!esRevisorActivo)
                    throw new AbrilException("Solo el autor o su jefatura activa asignada pueden editar la lección.", 403);
            }

            int? catalogItemId = dto.CatalogItemId > 0 ? dto.CatalogItemId : null;
            if (catalogItemId.HasValue)
            {
                var exists = await ctx.CatalogItem.AnyAsync(c => c.CatalogItemId == catalogItemId.Value && c.Active);
                if (!exists) throw new AbrilException("Escoger una relación válida.", 400);
            }

            lesson.ProblemDescription = dto.ProblemDescription;
            lesson.ReasonDescription = dto.ReasonDescription;
            lesson.LessonDescription = dto.LessonDescription;
            lesson.ImpactDescription = dto.ImpactDescription;
            lesson.ProjectId = dto.ProjectId;
            lesson.AreaId = dto.AreaId;
            lesson.CatalogItemId = catalogItemId;
            lesson.LessonAreaId = dto.LessonAreaId;
            lesson.ObraOficinaStaffId = dto.ObraOficinaStaffId;
            // Editar devuelve la lección a revisión (salvo auto-aprobación propia).
            lesson.ApprovalStatus = "PENDIENTE";
            lesson.ReviewedByUserId = null;
            lesson.ReviewedAt = null;
            lesson.RejectionComment = null;
            lesson.UpdatedDateTime = DateTimeOffset.UtcNow;
            lesson.UpdatedUserId = currentUserId;

            // Auto-aprobar solo si el autor está editando su propia lección.
            if (lesson.CreatedUserId == currentUserId)
            {
                var autoApprove = await (
                    from p in ctx.Person
                    join w in ctx.Worker on p.PersonId equals w.PersonId
                    where p.UserId == currentUserId
                    select w.AutoApproveLesson
                ).FirstOrDefaultAsync();

                if (autoApprove)
                {
                    lesson.ApprovalStatus = "APROBADA";
                    lesson.ReviewedByUserId = currentUserId;
                    lesson.ReviewedAt = DateTimeOffset.UtcNow;
                }
            }

            await ctx.SaveChangesAsync();

            // Imágenes: quitar las marcadas y agregar las nuevas.
            if (dto.RemovedImageIds != null && dto.RemovedImageIds.Count > 0)
            {
                await ctx.LessonImages
                    .Where(x => x.LessonId == lessonId && x.State && dto.RemovedImageIds.Contains(x.LessonImageId))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.State, false)
                        .SetProperty(x => x.Active, false)
                        .SetProperty(x => x.UpdatedDateTime, DateTime.UtcNow)
                        .SetProperty(x => x.UpdatedUserId, currentUserId));
            }
            if (dto.OpportunityImages?.Any() == true)
                await SaveImagesAsync(dto.OpportunityImages, lessonId, 1, ctx);
            if (dto.ImprovementImages?.Any() == true)
                await SaveImagesAsync(dto.ImprovementImages, lessonId, 2, ctx);

            return true;
        }

        // ──────────────────────────────────────────────────────────────────
        // HELPERS
        // ──────────────────────────────────────────────────────────────────

        private static async Task AttachImagesAndEnrichAsync(AppDbContext ctx, List<LessonListDTO> lessons)
        {
            if (lessons.Count == 0) return;

            var lessonIds = lessons.Select(l => l.LessonId).ToList();

            var imagenes = await (
                from img in ctx.LessonImages
                join imagetype in ctx.ImageType on img.ImageTypeId equals imagetype.ImageTypeId
                where img.State == true && lessonIds.Contains(img.LessonId)
                select new LessonImageDTO
                {
                    LessonImageId = img.LessonImageId,
                    ImageUrl = img.ImageUrl,
                    LessonId = img.LessonId,
                    ImageTypeId = img.ImageTypeId,
                    ImageTypeDescription = imagetype.ImageTypeDescription
                }
            ).ToListAsync();

            var imagesByLesson = imagenes.GroupBy(i => i.LessonId).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var lesson in lessons)
            {
                if (imagesByLesson.TryGetValue(lesson.LessonId, out var imgs))
                    lesson.Images = imgs;
            }

            var enrichments = await LessonEnrichmentHelper.ComputeAsync(
                ctx,
                lessons.Select(l => (l.LessonId, l.LessonAreaId, l.CatalogItemId)).ToList()
            );
            foreach (var lesson in lessons)
            {
                if (!enrichments.TryGetValue(lesson.LessonId, out var e)) continue;
                if (e.AreaDescription != null) lesson.AreaDescription = e.AreaDescription;
                if (e.AreaListDescription != null) lesson.AreaListDescription = e.AreaListDescription;
                if (e.ClassificationSegments != null && e.ClassificationSegments.Count > 0)
                    lesson.ClassificationSegments = e.ClassificationSegments;
            }
        }

        private async Task SaveImagesAsync(
            IEnumerable<IFormFile> files,
            int lessonId,
            int imageTypeId,
            AppDbContext ctx)
        {
            var container = _containerResolver.GetLessonsContainerName();
            var filesToUpload = new List<(Stream Stream, string FileName)>();

            foreach (var file in files)
            {
                if (file.Length == 0) continue;
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                filesToUpload.Add((file.OpenReadStream(), fileName));
            }
            if (!filesToUpload.Any()) return;

            List<string> uploadedUrls;
            try
            {
                uploadedUrls = await _fileStorageService.UploadFilesAsync(filesToUpload, container);
            }
            finally
            {
                foreach (var f in filesToUpload) f.Stream.Dispose();
            }

            foreach (var url in uploadedUrls)
            {
                ctx.LessonImages.Add(new LessonImages
                {
                    ImageUrl = url,
                    LessonId = lessonId,
                    ImageTypeId = imageTypeId,
                    CreatedDateTime = DateTimeOffset.UtcNow,
                    CreatedUserId = 1,
                    UpdatedDateTime = null,
                    Active = true,
                    State = true
                });
            }
            await ctx.SaveChangesAsync();
        }
    }
}

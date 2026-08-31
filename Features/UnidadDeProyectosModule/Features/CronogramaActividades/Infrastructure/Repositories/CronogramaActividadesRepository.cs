using Abril_Backend.Shared.Constants;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.CronogramaActividades.Application.Dtos;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.CronogramaActividades.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Mpxj = MPXJ.Net;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.CronogramaActividades.Infrastructure.Repositories
{
    public class CronogramaActividadesRepository : ICronogramaActividadesRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public CronogramaActividadesRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<ProyectoSimpleCronogramaDto>> GetProyectosAsync()
        {
            using var ctx = _factory.CreateDbContext();

            var proyectos = await ctx.Project
                .Where(p => p.State && p.Active && p.TieneUnidadDeProyectos && !ctx.ProyectoFiltro.Any(f => f.ProjectId == p.ProjectId && f.FuncionalidadId == ProyectoFiltroFuncionalidades.UdpCronograma && !f.Active))
                .OrderBy(p => p.ProjectDescription)
                .Select(p => new ProyectoSimpleCronogramaDto
                {
                    ProjectId = p.ProjectId,
                    ProjectDescription = p.ProjectDescription,
                    ResponsableUdp = p.ResponsableUdp,
                    TotalActividades = ctx.ProjectActivity
                        .Count(a => a.ProjectId == p.ProjectId && a.State && a.Active)
                })
                .ToListAsync();

            if (proyectos.Count == 0)
                return proyectos;

            var projectIds = proyectos.Select(p => p.ProjectId).ToList();

            // Una sola query para TODAS las actividades de TODOS los proyectos (evita N+1).
            var actividades = await ctx.ProjectActivity
                .Where(a => projectIds.Contains(a.ProjectId) && a.State && a.Active)
                .Select(a => new ActividadAvance
                {
                    ProjectActivityId = a.ProjectActivityId,
                    ProjectId = a.ProjectId,
                    ParentId = a.ParentId,
                    HierarchyLevel = a.HierarchyLevel,
                    ActualEndDate = a.ActualEndDate,
                    ProgressPercentage = a.ProgressPercentage,
                    TipoCronograma = a.TipoCronograma
                })
                .ToListAsync();

            var actividadesPorProyecto = actividades
                .GroupBy(a => a.ProjectId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var proyecto in proyectos)
            {
                if (actividadesPorProyecto.TryGetValue(proyecto.ProjectId, out var acts))
                {
                    var porTab = acts.GroupBy(a => a.TipoCronograma).ToDictionary(g => g.Key, g => g.ToList());
                    proyecto.AvanceAnteproyecto = porTab.TryGetValue("ANTEPROYECTO", out var ant) ? CalcularAvanceNivel0(ant) : 0;
                    proyecto.AvanceProyecto = porTab.TryGetValue("PROYECTO", out var proy) ? CalcularAvanceNivel0(proy) : 0;
                    proyecto.AvanceProyectoActualizacion = porTab.TryGetValue("PROYECTO_ACTUALIZACION", out var pact) ? CalcularAvanceNivel0(pact) : 0;
                }
            }

            return proyectos;
        }

        /// <summary>Vista mínima de una actividad para calcular el avance del proyecto.</summary>
        private sealed class ActividadAvance
        {
            public int ProjectActivityId { get; set; }
            public int ProjectId { get; set; }
            public int? ParentId { get; set; }
            public int HierarchyLevel { get; set; }
            public DateOnly? PlannedStartDate { get; set; }
            public DateOnly? PlannedEndDate { get; set; }
            public DateOnly? ActualEndDate { get; set; }
            public int ProgressPercentage { get; set; }
            public string TipoCronograma { get; set; } = "ANTEPROYECTO";
        }

        /// <summary>
        /// Avance del/los nodo(s) de nivel 0: promedio recursivo de hijos directos en cada nivel.
        /// Hoja = 100 si está culminada (ActualEndDate), si no su ProgressPercentage. 0 si no hay actividades.
        /// </summary>
        private static int CalcularAvanceNivel0(List<ActividadAvance> acts)
        {
            var nivel0 = acts.Where(a => a.HierarchyLevel == 0).ToList();
            if (nivel0.Count == 0) return 0;

            var porId = acts.ToDictionary(a => a.ProjectActivityId);
            var hijosPorPadre = acts
                .Where(a => a.ParentId.HasValue)
                .GroupBy(a => a.ParentId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());
            var memo = new Dictionary<int, int>();

            int Calc(int id)
            {
                if (memo.TryGetValue(id, out var cached)) return cached;

                int result;
                if (hijosPorPadre.TryGetValue(id, out var hijos) && hijos.Count > 0)
                {
                    var suma = hijos.Sum(h => Calc(h.ProjectActivityId));
                    result = (int)Math.Round((double)suma / hijos.Count, MidpointRounding.AwayFromZero);
                }
                else
                {
                    result = porId.TryGetValue(id, out var a)
                        ? (a.ActualEndDate.HasValue ? 100 : a.ProgressPercentage)
                        : 0;
                }

                memo[id] = result;
                return result;
            }

            var total = nivel0.Sum(n => Calc(n.ProjectActivityId));
            return (int)Math.Round((double)total / nivel0.Count, MidpointRounding.AwayFromZero);
        }

        public async Task<ActividadesProyectoResponseDto> GetActividadesAsync(int proyectoId, string tipoCronograma)
        {
            using var ctx = _factory.CreateDbContext();

            var proyecto = await ctx.Project
                .Where(p => p.ProjectId == proyectoId && p.State)
                .Select(p => new ProyectoCronogramaHeaderDto
                {
                    ProjectId = p.ProjectId,
                    ProjectDescription = p.ProjectDescription,
                    ResponsableUdp = p.ResponsableUdp,
                    FechaInicio = p.FechaInicio
                })
                .FirstOrDefaultAsync();

            if (proyecto == null)
                throw new AbrilException("Proyecto no encontrado.", 404);

            var actividades = await ctx.ProjectActivity
                .Where(a => a.ProjectId == proyectoId && a.State && a.Active && a.TipoCronograma == tipoCronograma)
                .OrderBy(a => a.Order)
                .ToListAsync();

            var ids = actividades.Select(a => a.ProjectActivityId).ToHashSet();

            // Predecesoras de cada actividad del proyecto
            var relaciones = await ctx.ActivityPredecessors
                .Where(r => ids.Contains(r.ActivityId))
                .Select(r => new { r.ActivityId, r.PredecessorId })
                .ToListAsync();
            var predecesorasPorActividad = relaciones
                .GroupBy(r => r.ActivityId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.PredecessorId).ToList());

            // Un nodo es padre si alguna actividad activa lo referencia como ParentId
            var idsPadre = actividades
                .Where(a => a.ParentId.HasValue)
                .Select(a => a.ParentId!.Value)
                .ToHashSet();

            return new ActividadesProyectoResponseDto
            {
                Proyecto = proyecto,
                Actividades = actividades.Select(a => new ActividadDto
                {
                    ProjectActivityId = a.ProjectActivityId,
                    ProjectId = a.ProjectId,
                    ActivityDescription = a.ActivityDescription,
                    PlannedStartDate = a.PlannedStartDate,
                    PlannedEndDate = a.PlannedEndDate,
                    ActualEndDate = a.ActualEndDate,
                    BaselineStartDate = a.BaselineStartDate,
                    BaselineEndDate = a.BaselineEndDate,
                    ProgressPercentage = a.ProgressPercentage,
                    Order = a.Order,
                    HierarchyLevel = a.HierarchyLevel,
                    ParentId = a.ParentId,
                    Predecesoras = predecesorasPorActividad.GetValueOrDefault(a.ProjectActivityId, new List<int>()),
                    EsPadre = idsPadre.Contains(a.ProjectActivityId),
                    TipoCronograma = a.TipoCronograma
                }).ToList()
            };
        }

        public async Task<ActividadDto> CrearActividadAsync(int proyectoId, CrearActividadRequest request, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var maxOrder = await ctx.ProjectActivity
                .Where(a => a.ProjectId == proyectoId && a.State)
                .Select(a => (int?)a.Order)
                .MaxAsync() ?? 0;

            var activity = new ProjectActivity
            {
                ProjectId = proyectoId,
                ActivityDescription = request.ActivityDescription,
                PlannedStartDate = request.PlannedStartDate,
                PlannedEndDate = request.PlannedEndDate,
                ActualEndDate = null,
                BaselineStartDate = request.PlannedStartDate,
                BaselineEndDate = request.PlannedEndDate,
                ProgressPercentage = request.ProgressPercentage,
                Order = maxOrder + 1,
                HierarchyLevel = request.HierarchyLevel,
                ParentId = request.ParentId,
                IsManual = true,
                TipoCronograma = request.TipoCronograma,
                CreatedDateTime = DateTime.UtcNow,
                CreatedUserId = userId,
                Active = true,
                State = true
            };
            ctx.ProjectActivity.Add(activity);
            await ctx.SaveChangesAsync();

            return new ActividadDto
            {
                ProjectActivityId = activity.ProjectActivityId,
                ProjectId = activity.ProjectId,
                ActivityDescription = activity.ActivityDescription,
                PlannedStartDate = activity.PlannedStartDate,
                PlannedEndDate = activity.PlannedEndDate,
                ActualEndDate = activity.ActualEndDate,
                BaselineStartDate = activity.BaselineStartDate,
                BaselineEndDate = activity.BaselineEndDate,
                ProgressPercentage = activity.ProgressPercentage,
                Order = activity.Order,
                HierarchyLevel = activity.HierarchyLevel,
                ParentId = activity.ParentId,
                TipoCronograma = activity.TipoCronograma
            };
        }

        public async Task<ActividadDto> EditarActividadAsync(int projectActivityId, EditarActividadRequest request, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var data = await ctx.ProjectActivity
                .Where(a => a.ProjectActivityId == projectActivityId && a.State)
                .Select(a => new
                {
                    Activity = a,
                    EsPadre = ctx.ProjectActivity.Any(h => h.ParentId == projectActivityId && h.State && h.Active)
                })
                .FirstOrDefaultAsync();
            if (data == null)
                throw new AbrilException("Actividad no encontrada.", 404);

            var activity = data.Activity;

            // Los nodos padre tienen fechas calculadas (MIN/MAX de hijos): no se editan manualmente
            if (data.EsPadre &&
                (request.PlannedStartDate != activity.PlannedStartDate ||
                 request.PlannedEndDate != activity.PlannedEndDate))
            {
                throw new AbrilException(
                    "No se pueden editar las fechas de una actividad con sub-actividades. " +
                    "Sus fechas se calculan automáticamente a partir de sus hijos.", 400);
            }

            // Primera vez que se guarda INICIO/FIN PROG. (LB aún vacío): copiar a la línea base.
            // Si LB ya tiene valor, no se pisa — eso lo maneja el botón "Línea Base".
            if (!data.EsPadre)
            {
                if (activity.BaselineStartDate == null && request.PlannedStartDate.HasValue)
                    activity.BaselineStartDate = request.PlannedStartDate;
                if (activity.BaselineEndDate == null && request.PlannedEndDate.HasValue)
                    activity.BaselineEndDate = request.PlannedEndDate;
            }

            activity.ActivityDescription = request.ActivityDescription;
            activity.PlannedStartDate = request.PlannedStartDate;
            activity.PlannedEndDate = request.PlannedEndDate;
            activity.ActualEndDate = request.ActualEndDate;
            activity.ProgressPercentage = request.ProgressPercentage;
            activity.UpdatedDateTime = DateTime.UtcNow;
            activity.UpdatedUserId = userId;

            await ctx.SaveChangesAsync();

            return new ActividadDto
            {
                ProjectActivityId = activity.ProjectActivityId,
                ProjectId = activity.ProjectId,
                ActivityDescription = activity.ActivityDescription,
                PlannedStartDate = activity.PlannedStartDate,
                PlannedEndDate = activity.PlannedEndDate,
                ActualEndDate = activity.ActualEndDate,
                BaselineStartDate = activity.BaselineStartDate,
                BaselineEndDate = activity.BaselineEndDate,
                ProgressPercentage = activity.ProgressPercentage,
                Order = activity.Order,
                HierarchyLevel = activity.HierarchyLevel,
                ParentId = activity.ParentId,
                TipoCronograma = activity.TipoCronograma
            };
        }

        public async Task<CulminarActividadDto> CulminarActividadAsync(int projectActivityId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var activity = await ctx.ProjectActivity
                .FirstOrDefaultAsync(a => a.ProjectActivityId == projectActivityId && a.State);
            if (activity == null)
                throw new AbrilException("Actividad no encontrada.", 404);

            if (activity.ActualEndDate.HasValue)
            {
                activity.ActualEndDate = null;
                activity.ProgressPercentage = 0;
            }
            else
            {
                activity.ActualEndDate = DateOnly.FromDateTime(DateTime.UtcNow);
                activity.ProgressPercentage = 100;
            }
            activity.UpdatedDateTime = DateTime.UtcNow;
            activity.UpdatedUserId = userId;

            await ctx.SaveChangesAsync();

            return new CulminarActividadDto
            {
                ProjectActivityId = activity.ProjectActivityId,
                ActualEndDate = activity.ActualEndDate,
                ProgressPercentage = activity.ProgressPercentage
            };
        }

        public async Task<List<DebugProyectoDto>> GetDebugProyectosAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Project
                .OrderBy(p => p.ProjectId)
                .Select(p => new DebugProyectoDto
                {
                    ProjectId = p.ProjectId,
                    ProjectDescription = p.ProjectDescription,
                    TieneUnidadDeProyectos = p.TieneUnidadDeProyectos,
                    State = p.State
                })
                .ToListAsync();
        }

        public async Task EliminarActividadAsync(int projectActivityId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var activity = await ctx.ProjectActivity
                .FirstOrDefaultAsync(a => a.ProjectActivityId == projectActivityId && a.State);
            if (activity == null)
                throw new AbrilException("Actividad no encontrada.", 404);

            activity.State = false;
            activity.Active = false;
            activity.UpdatedDateTime = DateTime.UtcNow;
            activity.UpdatedUserId = userId;

            await ctx.SaveChangesAsync();
        }

        public async Task<ImportarMppResultDto> ImportarMppAsync(int proyectoId, IFormFile archivo, int userId, string tipoCronograma)
        {
            if (archivo == null || archivo.Length == 0)
                throw new AbrilException("El archivo .mpp está vacío o no fue enviado.", 400);

            using var ctx = _factory.CreateDbContext();

            var proyecto = await ctx.Project.FirstOrDefaultAsync(p => p.ProjectId == proyectoId && p.State);
            if (proyecto == null)
                throw new AbrilException("Proyecto no encontrado.", 404);

            // Guardar el archivo en un path temporal para que MPXJ pueda leerlo
            var tempPath = Path.Combine(Path.GetTempPath(), $"mpp_{Guid.NewGuid()}.mpp");
            try
            {
                using (var fs = File.Create(tempPath))
                    await archivo.CopyToAsync(fs);

                var projectFile = new Mpxj.UniversalProjectReader().Read(tempPath);

                // Fecha de inicio del proyecto en el .mpp (DateTime? nativo en MPXJ.Net)
                DateTime? mppStartDt = projectFile.ProjectProperties.StartDate;
                DateOnly? mppStartDate = mppStartDt.HasValue ? DateOnly.FromDateTime(mppStartDt.Value) : null;

                // Calcular offset en días entre el inicio del .mpp y la fecha real del proyecto en BD
                int offsetDias = 0;
                if (mppStartDate.HasValue && proyecto.FechaInicio.HasValue)
                    offsetDias = proyecto.FechaInicio.Value.DayNumber - mppStartDate.Value.DayNumber;

                // Preservar actividades manuales de esta pestaña (pueden tener padres que se eliminarán)
                var manuales = await ctx.ProjectActivity
                    .Where(a => a.ProjectId == proyectoId && a.IsManual && a.TipoCronograma == tipoCronograma)
                    .ToListAsync();

                // Eliminar solo actividades no-manuales de esta pestaña
                var existentes = await ctx.ProjectActivity
                    .Where(a => a.ProjectId == proyectoId && !a.IsManual && a.TipoCronograma == tipoCronograma)
                    .ToListAsync();
                int eliminadas = existentes.Count;

                // Borrar predecesoras de las actividades a eliminar; el FK predecessor_id
                // es RESTRICT, por lo que hay que limpiarlas antes del RemoveRange.
                var actividadIds = existentes.Select(a => a.ProjectActivityId).ToList();
                if (actividadIds.Count > 0)
                {
                    var predsAEliminar = await ctx.ActivityPredecessors
                        .Where(p => actividadIds.Contains(p.ActivityId)
                                 || actividadIds.Contains(p.PredecessorId))
                        .ToListAsync();
                    ctx.ActivityPredecessors.RemoveRange(predsAEliminar);
                }

                ctx.ProjectActivity.RemoveRange(existentes);
                await ctx.SaveChangesAsync();

                // Mapeo: UniqueID del .mpp → entidad para resolver parent_id en la 2ª pasada
                var uniqueIdToEntity = new Dictionary<int, ProjectActivity>();

                int orden = 1;

                // ── Pasada 1: insertar todas las actividades con ParentId = null ──────────────
                foreach (var tarea in projectFile.Tasks)
                {
                    if (tarea.Null || string.IsNullOrWhiteSpace(tarea.Name)) continue;

                    int uniqueId = tarea.UniqueID ?? 0;
                    DateOnly? inicio = AplicarOffset(tarea.Start, offsetDias);
                    DateOnly? fin = AplicarOffset(tarea.Finish, offsetDias);

                    int pct = (int)(tarea.PercentageComplete ?? 0.0);
                    DateOnly? actualEnd = pct >= 100
                        ? (tarea.ActualFinish.HasValue
                            ? DateOnly.FromDateTime(tarea.ActualFinish.Value)
                            : fin ?? DateOnly.FromDateTime(DateTime.UtcNow))
                        : null;
                    pct = Math.Min(pct, 100);

                    var nueva = new ProjectActivity
                    {
                        ProjectId = proyectoId,
                        ActivityDescription = tarea.Name.Trim(),
                        PlannedStartDate = inicio,
                        PlannedEndDate = fin,
                        ActualEndDate = actualEnd,
                        ProgressPercentage = pct,
                        Order = orden++,
                        ParentId = null,
                        HierarchyLevel = tarea.OutlineLevel ?? 0,
                        TipoCronograma = tipoCronograma,
                        CreatedDateTime = DateTime.UtcNow,
                        CreatedUserId = userId,
                        Active = true,
                        State = true
                    };
                    ctx.ProjectActivity.Add(nueva);

                    if (uniqueId > 0)
                        uniqueIdToEntity[uniqueId] = nueva;
                }
                await ctx.SaveChangesAsync();  // un solo INSERT masivo — genera todos los IDs

                // ── Pasada 2: asignar ParentId usando los IDs ya generados ───────────────────
                foreach (var tarea in projectFile.Tasks)
                {
                    if (tarea.Null || string.IsNullOrWhiteSpace(tarea.Name)) continue;

                    int uniqueId = tarea.UniqueID ?? 0;
                    if (uniqueId == 0 || !uniqueIdToEntity.TryGetValue(uniqueId, out var actividad)) continue;

                    var parentMpxj = tarea.ParentTask;
                    if (parentMpxj == null) continue;

                    int parentUniqueId = parentMpxj.UniqueID ?? 0;
                    if (parentUniqueId > 0 && uniqueIdToEntity.TryGetValue(parentUniqueId, out var padre))
                        actividad.ParentId = padre.ProjectActivityId;
                }
                await ctx.SaveChangesAsync();  // actualiza solo los ParentId que cambiaron

                // Post-procesado: ajustar actividades manuales conservadas
                if (manuales.Count > 0)
                {
                    var mppIdsList = uniqueIdToEntity.Values.Select(e => e.ProjectActivityId).ToList();
                    var manualesIdsList = manuales.Select(m => m.ProjectActivityId).ToList();
                    var idsValidos = mppIdsList.Concat(manualesIdsList).ToHashSet();
                    var idsValidosList = idsValidos.ToList();

                    // Eliminar predecesoras de manuales que apunten a IDs ya inexistentes
                    var predsHuerfanas = await ctx.ActivityPredecessors
                        .Where(ap => manualesIdsList.Contains(ap.ActivityId)
                                  && !idsValidosList.Contains(ap.PredecessorId))
                        .ToListAsync();
                    if (predsHuerfanas.Count > 0)
                        ctx.ActivityPredecessors.RemoveRange(predsHuerfanas);

                    int idxManual = 1;
                    foreach (var m in manuales.OrderBy(m => m.Order))
                    {
                        if (m.ParentId.HasValue && !idsValidos.Contains(m.ParentId.Value))
                        {
                            m.ParentId = null;
                            m.HierarchyLevel = 0;
                        }
                        m.Order = (orden - 1) + idxManual++;
                    }

                    await ctx.SaveChangesAsync();
                }

                return new ImportarMppResultDto
                {
                    ActividadesImportadas = orden - 1,
                    ActividadesEliminadas = eliminadas,
                    ActividadesManualesConservadas = manuales.Count
                };
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        }

        public async Task<List<ActividadDto>> ReordenarActividadesAsync(int proyectoId, List<ReordenarItem> items)
        {
            if (items == null || items.Count == 0)
                throw new AbrilException("La lista de actividades a reordenar está vacía.", 400);

            Console.WriteLine($"[Reordenar] proyectoId={proyectoId} items recibidos={items.Count}");

            using var ctx = _factory.CreateDbContext();

            var ids = items.Select(i => i.ProjectActivityId).ToList();

            // Cargamos TODAS las actividades del proyecto — se usan para validar Y para el return
            var todasActividades = await ctx.ProjectActivity
                .Where(a => a.ProjectId == proyectoId && a.State)
                .ToListAsync();

            var activities = todasActividades
                .Where(a => ids.Contains(a.ProjectActivityId))
                .ToList();

            if (activities.Count != ids.Count)
                throw new AbrilException("Una o más actividades no pertenecen al proyecto o no existen.", 400);

            foreach (var item in items)
            {
                var activity = activities.First(a => a.ProjectActivityId == item.ProjectActivityId);
                Console.WriteLine($"[Reordenar]   ID={item.ProjectActivityId} parentId={activity.ParentId} orderAnterior={activity.Order} → nuevoOrder={item.Order}");
                activity.Order = item.Order;
            }

            await ctx.SaveChangesAsync();
            Console.WriteLine("[Reordenar] Reordenamiento completado");

            return todasActividades
                .Where(a => a.Active)
                .OrderBy(a => a.Order)
                .Select(a => new ActividadDto
                {
                    ProjectActivityId = a.ProjectActivityId,
                    ProjectId = a.ProjectId,
                    ActivityDescription = a.ActivityDescription,
                    PlannedStartDate = a.PlannedStartDate,
                    PlannedEndDate = a.PlannedEndDate,
                    ActualEndDate = a.ActualEndDate,
                    BaselineStartDate = a.BaselineStartDate,
                    BaselineEndDate = a.BaselineEndDate,
                    ProgressPercentage = a.ProgressPercentage,
                    Order = a.Order,
                    HierarchyLevel = a.HierarchyLevel,
                    ParentId = a.ParentId,
                    TipoCronograma = a.TipoCronograma
                })
                .ToList();
        }



        public async Task<List<ActividadDto>> CambiarJerarquiaAsync(int proyectoId, CambiarJerarquiaRequest request)
        {
            using var ctx = _factory.CreateDbContext();

            // Una sola carga; sirve para obtener la actividad, actualizar hijos y construir el return
            var todas = await ctx.ProjectActivity
                .Where(a => a.ProjectId == proyectoId && a.State)
                .ToListAsync();

            var activity = todas.FirstOrDefault(a => a.ProjectActivityId == request.ProjectActivityId);
            if (activity == null)
                throw new AbrilException("Actividad no encontrada.", 404);

            int levelDelta = request.NuevoHierarchyLevel - activity.HierarchyLevel;
            activity.HierarchyLevel = request.NuevoHierarchyLevel;
            activity.ParentId = request.NuevoParentId;

            if (levelDelta != 0)
                ActualizarHijosRecursivo(activity.ProjectActivityId, levelDelta, todas);

            await ctx.SaveChangesAsync();

            return todas
                .Where(a => a.Active)
                .OrderBy(a => a.Order)
                .Select(a => new ActividadDto
                {
                    ProjectActivityId = a.ProjectActivityId,
                    ProjectId = a.ProjectId,
                    ActivityDescription = a.ActivityDescription,
                    PlannedStartDate = a.PlannedStartDate,
                    PlannedEndDate = a.PlannedEndDate,
                    ActualEndDate = a.ActualEndDate,
                    BaselineStartDate = a.BaselineStartDate,
                    BaselineEndDate = a.BaselineEndDate,
                    ProgressPercentage = a.ProgressPercentage,
                    Order = a.Order,
                    HierarchyLevel = a.HierarchyLevel,
                    ParentId = a.ParentId,
                    TipoCronograma = a.TipoCronograma
                })
                .ToList();
        }

        public async Task<List<ActividadDto>> SubirNivelAsync(int proyectoId, int actividadId)
        {
            using var ctx = _factory.CreateDbContext();

            var todas = await ctx.ProjectActivity
                .Where(a => a.ProjectId == proyectoId && a.State)
                .ToListAsync();

            var actividad = todas.FirstOrDefault(a => a.ProjectActivityId == actividadId);
            if (actividad == null)
                throw new AbrilException("Actividad no encontrada.", 404);

            if (actividad.HierarchyLevel == 0)
                throw new AbrilException("La actividad ya está en el nivel más alto.", 400);

            // Nuevo parentId = abuelo (parentId del padre actual)
            int? nuevoParentId = null;
            if (actividad.ParentId.HasValue)
            {
                var padre = todas.FirstOrDefault(a => a.ProjectActivityId == actividad.ParentId.Value);
                nuevoParentId = padre?.ParentId;
            }

            actividad.HierarchyLevel -= 1;
            actividad.ParentId = nuevoParentId;
            ActualizarHijosRecursivo(actividadId, -1, todas);

            await ctx.SaveChangesAsync();

            return todas
                .Where(a => a.Active)
                .OrderBy(a => a.Order)
                .Select(a => new ActividadDto
                {
                    ProjectActivityId = a.ProjectActivityId,
                    ProjectId = a.ProjectId,
                    ActivityDescription = a.ActivityDescription,
                    PlannedStartDate = a.PlannedStartDate,
                    PlannedEndDate = a.PlannedEndDate,
                    ActualEndDate = a.ActualEndDate,
                    BaselineStartDate = a.BaselineStartDate,
                    BaselineEndDate = a.BaselineEndDate,
                    ProgressPercentage = a.ProgressPercentage,
                    Order = a.Order,
                    HierarchyLevel = a.HierarchyLevel,
                    ParentId = a.ParentId,
                    TipoCronograma = a.TipoCronograma
                })
                .ToList();
        }

        public async Task<List<ActividadDto>> BajarNivelAsync(int proyectoId, int actividadId)
        {
            using var ctx = _factory.CreateDbContext();

            var todas = await ctx.ProjectActivity
                .Where(a => a.ProjectId == proyectoId && a.State)
                .ToListAsync();

            var actividad = todas.FirstOrDefault(a => a.ProjectActivityId == actividadId);
            if (actividad == null)
                throw new AbrilException("Actividad no encontrada.", 404);

            // Hermano inmediatamente anterior: mismo parentId, mayor order menor que el actual
            var hermanoAnterior = todas
                .Where(a => a.ParentId == actividad.ParentId
                         && a.ProjectActivityId != actividadId
                         && a.Order < actividad.Order)
                .OrderByDescending(a => a.Order)
                .FirstOrDefault();

            if (hermanoAnterior == null)
                throw new AbrilException("No hay un padre disponible para asignar esta actividad.", 400);

            actividad.HierarchyLevel += 1;
            actividad.ParentId = hermanoAnterior.ProjectActivityId;
            ActualizarHijosRecursivo(actividadId, 1, todas);

            await ctx.SaveChangesAsync();

            return todas
                .Where(a => a.Active)
                .OrderBy(a => a.Order)
                .Select(a => new ActividadDto
                {
                    ProjectActivityId = a.ProjectActivityId,
                    ProjectId = a.ProjectId,
                    ActivityDescription = a.ActivityDescription,
                    PlannedStartDate = a.PlannedStartDate,
                    PlannedEndDate = a.PlannedEndDate,
                    ActualEndDate = a.ActualEndDate,
                    BaselineStartDate = a.BaselineStartDate,
                    BaselineEndDate = a.BaselineEndDate,
                    ProgressPercentage = a.ProgressPercentage,
                    Order = a.Order,
                    HierarchyLevel = a.HierarchyLevel,
                    ParentId = a.ParentId,
                    TipoCronograma = a.TipoCronograma
                })
                .ToList();
        }

        // ─────────────────────────── Feriados ───────────────────────────

        public async Task<List<FeriadoDto>> GetFeriadosAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Feriados
                .OrderBy(f => f.Fecha)
                .Select(f => new FeriadoDto
                {
                    Id = f.Id,
                    Fecha = f.Fecha,
                    Descripcion = f.Descripcion
                })
                .ToListAsync();
        }

        public async Task<FeriadoDto> CrearFeriadoAsync(CrearFeriadoRequest request)
        {
            using var ctx = _factory.CreateDbContext();

            var existe = await ctx.Feriados.AnyAsync(f => f.Fecha == request.Fecha);
            if (existe)
                throw new AbrilException("Ya existe un feriado registrado para esa fecha.", 400);

            var feriado = new Feriado
            {
                Fecha = request.Fecha,
                Descripcion = request.Descripcion
            };
            ctx.Feriados.Add(feriado);
            await ctx.SaveChangesAsync();

            return new FeriadoDto
            {
                Id = feriado.Id,
                Fecha = feriado.Fecha,
                Descripcion = feriado.Descripcion
            };
        }

        public async Task EliminarFeriadoAsync(int id)
        {
            using var ctx = _factory.CreateDbContext();
            var feriado = await ctx.Feriados.FirstOrDefaultAsync(f => f.Id == id);
            if (feriado == null)
                throw new AbrilException("Feriado no encontrado.", 404);

            ctx.Feriados.Remove(feriado);
            await ctx.SaveChangesAsync();
        }

        // ─────────────────────────── Predecesoras ───────────────────────────

        public async Task<int> GetProyectoIdDeActividadAsync(int activityId)
        {
            using var ctx = _factory.CreateDbContext();
            var actividad = await ctx.ProjectActivity
                .Where(a => a.ProjectActivityId == activityId && a.State && a.Active)
                .Select(a => (int?)a.ProjectId)
                .FirstOrDefaultAsync();
            if (actividad == null)
                throw new AbrilException("Actividad no encontrada.", 404);
            return actividad.Value;
        }

        public async Task<List<int>> GetPredecesorasAsync(int activityId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.ActivityPredecessors
                .Where(r => r.ActivityId == activityId)
                .Select(r => r.PredecessorId)
                .ToListAsync();
        }

        public async Task SetPredecesorasAsync(int activityId, List<int> predecessorIds)
        {
            using var ctx = _factory.CreateDbContext();

            var actividad = await ctx.ProjectActivity
                .FirstOrDefaultAsync(a => a.ProjectActivityId == activityId && a.State && a.Active);
            if (actividad == null)
                throw new AbrilException("Actividad no encontrada.", 404);

            var distintas = predecessorIds.Where(p => p != activityId).Distinct().ToList();

            if (distintas.Count > 0)
            {
                // Las predecesoras deben existir y ser del mismo proyecto (padre o hoja, ambos válidos)
                var candidatas = await ctx.ProjectActivity
                    .Where(a => distintas.Contains(a.ProjectActivityId) && a.State && a.Active)
                    .Select(a => new { a.ProjectActivityId, a.ProjectId })
                    .ToListAsync();

                if (candidatas.Count != distintas.Count)
                    throw new AbrilException("Una o más predecesoras no existen.", 400);

                if (candidatas.Any(c => c.ProjectId != actividad.ProjectId))
                    throw new AbrilException("Las predecesoras deben pertenecer al mismo proyecto.", 400);
            }

            // Reemplazo completo del conjunto de predecesoras
            var existentes = await ctx.ActivityPredecessors
                .Where(r => r.ActivityId == activityId)
                .ToListAsync();
            ctx.ActivityPredecessors.RemoveRange(existentes);

            foreach (var predId in distintas)
                ctx.ActivityPredecessors.Add(new ActivityPredecessor
                {
                    ActivityId = activityId,
                    PredecessorId = predId
                });

            await ctx.SaveChangesAsync();
        }

        // ─────────────────────────── Línea base ───────────────────────────

        public async Task<ActividadDto> ActualizarLineaBaseAsync(int projectActivityId, ActualizarLineaBaseRequest request, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var data = await ctx.ProjectActivity
                .Where(a => a.ProjectActivityId == projectActivityId && a.State && a.Active)
                .Select(a => new
                {
                    Activity = a,
                    EsPadre = ctx.ProjectActivity.Any(h => h.ParentId == projectActivityId && h.State && h.Active)
                })
                .FirstOrDefaultAsync();
            if (data == null)
                throw new AbrilException("Actividad no encontrada.", 404);

            var activity = data.Activity;

            if (data.EsPadre)
                throw new AbrilException(
                    "La línea base solo puede definirse en actividades hoja (sin sub-actividades).", 400);

            activity.BaselineStartDate = request.BaselineStartDate;
            activity.BaselineEndDate = request.BaselineEndDate;
            activity.UpdatedDateTime = DateTime.UtcNow;
            activity.UpdatedUserId = userId;

            await ctx.SaveChangesAsync();

            return new ActividadDto
            {
                ProjectActivityId = activity.ProjectActivityId,
                ProjectId = activity.ProjectId,
                ActivityDescription = activity.ActivityDescription,
                PlannedStartDate = activity.PlannedStartDate,
                PlannedEndDate = activity.PlannedEndDate,
                ActualEndDate = activity.ActualEndDate,
                BaselineStartDate = activity.BaselineStartDate,
                BaselineEndDate = activity.BaselineEndDate,
                ProgressPercentage = activity.ProgressPercentage,
                Order = activity.Order,
                HierarchyLevel = activity.HierarchyLevel,
                ParentId = activity.ParentId,
                EsPadre = false,
                TipoCronograma = activity.TipoCronograma
            };
        }

        // ─────────────────────────── Dashboard ───────────────────────────

        public async Task<CronogramaDashboardResponseDto> GetDashboardAsync(int? responsableId, string? estado)
        {
            using var ctx = _factory.CreateDbContext();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // Query 1: todos los proyectos UDP activos
            var proyectos = await ctx.Project
                .Where(p => p.TieneUnidadDeProyectos && p.State && p.Active && !ctx.ProyectoFiltro.Any(f => f.ProjectId == p.ProjectId && f.FuncionalidadId == ProyectoFiltroFuncionalidades.UdpCronograma && !f.Active))
                .Select(p => new { p.ProjectId, p.ProjectDescription, p.ResponsableUdp, p.ResponsableUdpId })
                .ToListAsync();

            if (proyectos.Count == 0)
                return new CronogramaDashboardResponseDto();

            var projectIds = proyectos.Select(p => p.ProjectId).ToList();

            // Query 2: todas las actividades activas de esos proyectos
            var actividades = await ctx.ProjectActivity
                .Where(a => projectIds.Contains(a.ProjectId) && a.State && a.Active)
                .Select(a => new ActividadAvance
                {
                    ProjectActivityId = a.ProjectActivityId,
                    ProjectId = a.ProjectId,
                    ParentId = a.ParentId,
                    HierarchyLevel = a.HierarchyLevel,
                    PlannedStartDate = a.PlannedStartDate,
                    PlannedEndDate = a.PlannedEndDate,
                    ActualEndDate = a.ActualEndDate,
                    ProgressPercentage = a.ProgressPercentage,
                    TipoCronograma = a.TipoCronograma
                })
                .ToListAsync();

            // Query 3: datos de usuario de los responsables
            var responsableIds = proyectos
                .Where(p => p.ResponsableUdpId.HasValue)
                .Select(p => p.ResponsableUdpId!.Value)
                .Distinct()
                .ToList();

            List<CronogramaDashboardResponsableDto> listaResponsables = new();
            if (responsableIds.Count > 0)
            {
                listaResponsables = await ctx.User
                    .Where(u => responsableIds.Contains(u.UserId) && u.State)
                    .Select(u => new CronogramaDashboardResponsableDto
                    {
                        UserId = u.UserId,
                        NombreCompleto = u.Person.FullName ?? string.Empty
                    })
                    .OrderBy(r => r.NombreCompleto)
                    .ToListAsync();
            }

            // Agrupación en memoria (sin N+1)
            var actividadesPorProyecto = actividades
                .GroupBy(a => a.ProjectId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Semana ISO: lunes a domingo
            int dow = (int)today.DayOfWeek;
            var semanaStart = today.AddDays(dow == 0 ? -6 : -(dow - 1));
            var semanaEnd = semanaStart.AddDays(6);

            // Fila por proyecto (todas, para calcular KPIs globales)
            var allRows = proyectos.Select(p =>
            {
                var acts = actividadesPorProyecto.GetValueOrDefault(p.ProjectId, new List<ActividadAvance>());

                var culminadas = acts.Count(a => a.ActualEndDate.HasValue);
                var vencidas = acts.Count(a => !a.ActualEndDate.HasValue
                                               && a.PlannedEndDate.HasValue
                                               && a.PlannedEndDate.Value < today);
                var enProceso = acts.Count(a => !a.ActualEndDate.HasValue
                                                && a.ProgressPercentage > 0
                                                && !(a.PlannedEndDate.HasValue && a.PlannedEndDate.Value < today));
                var pendientes = acts.Count(a => !a.ActualEndDate.HasValue
                                                 && a.ProgressPercentage == 0
                                                 && !(a.PlannedEndDate.HasValue && a.PlannedEndDate.Value < today));

                int porcentajeAvance = acts.Count > 0 ? CalcularAvanceNivel0(acts) : 0;

                int diasRetraso = vencidas > 0
                    ? acts.Where(a => !a.ActualEndDate.HasValue && a.PlannedEndDate.HasValue && a.PlannedEndDate.Value < today)
                          .Max(a => today.DayNumber - a.PlannedEndDate!.Value.DayNumber)
                    : 0;

                string semaforo = diasRetraso == 0 ? "VERDE" : diasRetraso <= 7 ? "AMARILLO" : "ROJO";
                string estadoProy = acts.Count == 0 ? "SIN_ACTIVIDADES" : vencidas > 0 ? "CON_RETRASO" : "AL_DIA";

                // Cálculo SPI
                decimal spi = 1.0m;
                if (acts.Count > 0)
                {
                    decimal sumEv = 0, sumPv = 0;
                    foreach (var act in acts)
                    {
                        decimal ev = act.ActualEndDate.HasValue ? 100m : act.ProgressPercentage;

                        decimal pv = 0m;
                        if (act.PlannedStartDate.HasValue && act.PlannedEndDate.HasValue)
                        {
                            if (today >= act.PlannedEndDate.Value)
                                pv = 100m;
                            else if (today <= act.PlannedStartDate.Value)
                                pv = 0m;
                            else
                            {
                                double totalDays = act.PlannedEndDate.Value.DayNumber - act.PlannedStartDate.Value.DayNumber;
                                double elapsed   = today.DayNumber - act.PlannedStartDate.Value.DayNumber;
                                pv = totalDays > 0 ? (decimal)(elapsed / totalDays) * 100m : 0m;
                            }
                        }
                        sumEv += ev;
                        sumPv += pv;
                    }
                    spi = sumPv > 0 ? Math.Round(sumEv / sumPv, 2) : 1.0m;
                }

                return new CronogramaDashboardProyectoDto
                {
                    ProjectId = p.ProjectId,
                    ProjectDescription = p.ProjectDescription,
                    ResponsableUdp = p.ResponsableUdp,
                    TotalActividades = acts.Count,
                    Culminadas = culminadas,
                    EnProceso = enProceso,
                    Vencidas = vencidas,
                    Pendientes = pendientes,
                    PorcentajeAvance = porcentajeAvance,
                    DiasRetraso = diasRetraso,
                    Semaforo = semaforo,
                    Estado = estadoProy,
                    Spi = spi
                };
            }).ToList();

            // KPIs globales
            var conActividades = allRows.Where(r => r.TotalActividades > 0).ToList();
            var kpis = new CronogramaDashboardKpisDto
            {
                TotalProyectos = conActividades.Count,
                PorcentajeAvancePromedio = conActividades.Count > 0
                    ? (int)Math.Round(conActividades.Average(r => (double)r.PorcentajeAvance), MidpointRounding.AwayFromZero)
                    : 0,
                ProyectosAlDia = allRows.Count(r => r.Estado == "AL_DIA"),
                ProyectosConRetraso = allRows.Count(r => r.Estado == "CON_RETRASO"),
                ProyectosSinActividades = allRows.Count(r => r.Estado == "SIN_ACTIVIDADES"),
                ActividadesVencidas = actividades.Count(a => !a.ActualEndDate.HasValue
                                                             && a.PlannedEndDate.HasValue
                                                             && a.PlannedEndDate.Value < today),
                ActividadesCulminadasEstaSemana = actividades.Count(a => a.ActualEndDate.HasValue
                                                                         && a.ActualEndDate.Value >= semanaStart
                                                                         && a.ActualEndDate.Value <= semanaEnd),
                ActividadesCulminadasEsteMes = actividades.Count(a => a.ActualEndDate.HasValue
                                                                      && a.ActualEndDate.Value.Year == today.Year
                                                                      && a.ActualEndDate.Value.Month == today.Month)
            };

            // Aplicar filtros a la lista de proyectos
            IEnumerable<CronogramaDashboardProyectoDto> filtrados = allRows;

            if (responsableId.HasValue)
            {
                var proyectosDelResponsable = proyectos
                    .Where(p => p.ResponsableUdpId == responsableId.Value)
                    .Select(p => p.ProjectId)
                    .ToHashSet();
                filtrados = filtrados.Where(r => proyectosDelResponsable.Contains(r.ProjectId));
            }

            if (!string.IsNullOrEmpty(estado))
                filtrados = filtrados.Where(r => r.Estado == estado);

            return new CronogramaDashboardResponseDto
            {
                Kpis = kpis,
                Proyectos = filtrados.ToList(),
                Responsables = listaResponsables
            };
        }

        // ─────────────────────────── Creación masiva ───────────────────────────

        public async Task<CrearActividadesMasivoResultDto> CrearActividadesMasivoAsync(int proyectoId, CrearActividadesMasivoRequest request, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var maxOrder = await ctx.ProjectActivity
                .Where(a => a.ProjectId == proyectoId && a.State)
                .Select(a => (int?)a.Order)
                .MaxAsync() ?? 0;

            var nuevas = request.Actividades.Select((item, idx) => new ProjectActivity
            {
                ProjectId = proyectoId,
                ActivityDescription = item.Nombre,
                PlannedStartDate = item.InicioProgramado,
                PlannedEndDate = item.FinProgramado,
                ActualEndDate = null,
                ProgressPercentage = 0,
                Order = maxOrder + idx + 1,
                HierarchyLevel = 0,
                ParentId = null,
                IsManual = true,
                TipoCronograma = item.TipoCronograma,
                CreatedDateTime = DateTime.UtcNow,
                CreatedUserId = userId,
                Active = true,
                State = true
            }).ToList();

            ctx.ProjectActivity.AddRange(nuevas);
            await ctx.SaveChangesAsync();

            return new CrearActividadesMasivoResultDto { ActividadesCreadas = nuevas.Count };
        }

        private static void ActualizarHijosRecursivo(int parentId, int levelDelta, List<ProjectActivity> todas)
        {
            foreach (var hijo in todas.Where(a => a.ParentId == parentId))
            {
                hijo.HierarchyLevel += levelDelta;
                ActualizarHijosRecursivo(hijo.ProjectActivityId, levelDelta, todas);
            }
        }

        private static DateOnly? AplicarOffset(DateTime? fecha, int offsetDias)
        {
            if (!fecha.HasValue) return null;
            return DateOnly.FromDateTime(fecha.Value).AddDays(offsetDias);
        }

        // ─────────────────────────── Última pestaña ───────────────────────────

        public async Task<string?> GetUltimaPestanaAsync(int proyectoId, int userId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.UserCronogramaPreferences
                .Where(p => p.UserId == userId && p.ProjectId == proyectoId)
                .Select(p => p.TipoCronograma)
                .FirstOrDefaultAsync();
        }

        public async Task ActualizarUltimaPestanaAsync(int proyectoId, int userId, string tipoCronograma)
        {
            using var ctx = _factory.CreateDbContext();

            var preferencia = await ctx.UserCronogramaPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId && p.ProjectId == proyectoId);

            if (preferencia == null)
            {
                ctx.UserCronogramaPreferences.Add(new UserCronogramaPreference
                {
                    UserId = userId,
                    ProjectId = proyectoId,
                    TipoCronograma = tipoCronograma,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                preferencia.TipoCronograma = tipoCronograma;
                preferencia.UpdatedAt = DateTime.UtcNow;
            }

            await ctx.SaveChangesAsync();
        }

        // ─────────────────────────── Plantilla ───────────────────────────

        private static readonly string PlantillaProyectoPath = Path.Combine(
            AppContext.BaseDirectory,
            "Features", "UnidadDeProyectosModule", "Features", "CronogramaActividades",
            "Seeds", "plantilla_proyecto_seed.json");

        private static readonly string PlantillaAnteproyectoPath = Path.Combine(
            AppContext.BaseDirectory,
            "Features", "UnidadDeProyectosModule", "Features", "CronogramaActividades",
            "Seeds", "plantilla_anteproyecto_seed.json");

        private static readonly JsonSerializerOptions PlantillaJsonOptions = new() { PropertyNameCaseInsensitive = true };

        private sealed class PlantillaItem
        {
            public string Codigo { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
            public int Nivel { get; set; }
            public bool EsPadre { get; set; }
            public string? ParentCodigo { get; set; }
            public string? PredecesoraCodigo { get; set; }
        }

        public async Task<AplicarPlantillaResultDto> AplicarPlantillaAsync(int proyectoId, string tipoCronograma, int userId)
        {
            var plantillaPath = tipoCronograma == "ANTEPROYECTO" ? PlantillaAnteproyectoPath : PlantillaProyectoPath;
            var json = await File.ReadAllTextAsync(plantillaPath);
            var items = JsonSerializer.Deserialize<List<PlantillaItem>>(json, PlantillaJsonOptions)
                ?? throw new AbrilException("La plantilla de proyecto está vacía o es inválida.", 500);

            using var ctx = _factory.CreateDbContext();

            var strategy = ctx.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await ctx.Database.BeginTransactionAsync();

                var maxOrder = await ctx.ProjectActivity
                    .Where(a => a.ProjectId == proyectoId && a.State)
                    .Select(a => (int?)a.Order)
                    .MaxAsync() ?? 0;

                // Pasada 1: insertar todas las actividades sin ParentId (los IDs recién existen tras SaveChanges).
                var codigoAEntidad = new Dictionary<string, ProjectActivity>();
                int orden = 0;
                foreach (var item in items)
                {
                    var activity = new ProjectActivity
                    {
                        ProjectId = proyectoId,
                        ActivityDescription = item.Nombre,
                        PlannedStartDate = null,
                        PlannedEndDate = null,
                        ProgressPercentage = 0,
                        Order = maxOrder + (++orden),
                        HierarchyLevel = item.Nivel - 1,
                        ParentId = null,
                        IsManual = true,
                        TipoCronograma = tipoCronograma,
                        CreatedDateTime = DateTime.UtcNow,
                        CreatedUserId = userId,
                        Active = true,
                        State = true
                    };
                    ctx.ProjectActivity.Add(activity);
                    codigoAEntidad[item.Codigo] = activity;
                }
                await ctx.SaveChangesAsync(); // un solo INSERT masivo — genera todos los IDs

                // Pasada 2: resolver ParentId y predecesoras usando los IDs ya generados.
                foreach (var item in items)
                {
                    if (item.ParentCodigo != null)
                        codigoAEntidad[item.Codigo].ParentId = codigoAEntidad[item.ParentCodigo].ProjectActivityId;

                    if (item.PredecesoraCodigo != null)
                    {
                        ctx.ActivityPredecessors.Add(new ActivityPredecessor
                        {
                            ActivityId = codigoAEntidad[item.Codigo].ProjectActivityId,
                            PredecessorId = codigoAEntidad[item.PredecesoraCodigo].ProjectActivityId
                        });
                    }
                }
                await ctx.SaveChangesAsync();

                await transaction.CommitAsync();
            });

            return new AplicarPlantillaResultDto { ActividadesCreadas = items.Count };
        }
    }
}

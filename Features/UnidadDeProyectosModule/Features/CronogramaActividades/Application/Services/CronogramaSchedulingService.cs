using Abril_Backend.Features.UnidadDeProyectosModule.Features.CronogramaActividades.Application.Dtos;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.CronogramaActividades.Application.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.CronogramaActividades.Application.Services
{
    public class CronogramaSchedulingService : ICronogramaSchedulingService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public CronogramaSchedulingService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        // ─────────────────────────── Días hábiles ───────────────────────────

        private static bool EsHabil(DateTime dia, HashSet<DateTime> feriados)
            => dia.DayOfWeek != DayOfWeek.Saturday
               && dia.DayOfWeek != DayOfWeek.Sunday
               && !feriados.Contains(dia.Date);

        public DateTime AddBusinessDays(DateTime start, int days, List<DateTime> feriados)
        {
            var set = feriados.Select(f => f.Date).ToHashSet();
            var current = start.Date;
            if (days <= 0) return current;
            int added = 0;
            while (added < days)
            {
                current = current.AddDays(1);
                if (EsHabil(current, set)) added++;
            }
            return current;
        }

        public DateTime NextBusinessDay(DateTime date, List<DateTime> feriados)
        {
            var set = feriados.Select(f => f.Date).ToHashSet();
            var current = date.Date.AddDays(1);
            while (!EsHabil(current, set)) current = current.AddDays(1);
            return current;
        }

        /// <summary>Días hábiles entre dos fechas, ambos extremos inclusive (mínimo 1).</summary>
        private static int DuracionHabilInclusive(DateOnly inicio, DateOnly fin, HashSet<DateTime> feriados)
        {
            var i = inicio.ToDateTime(TimeOnly.MinValue);
            var f = fin.ToDateTime(TimeOnly.MinValue);
            if (f < i) (i, f) = (f, i);
            int count = 0;
            for (var c = i; c <= f; c = c.AddDays(1))
                if (EsHabil(c, feriados)) count++;
            return Math.Max(1, count);
        }

        // ─────────────────────────── Detección de ciclos ───────────────────────────

        public async Task<bool> DetectCycleAsync(int proyectoId, int activityId, List<int> nuevasPredecesoras)
        {
            // Una actividad no puede ser su propia predecesora
            if (nuevasPredecesoras.Contains(activityId)) return true;

            using var ctx = _factory.CreateDbContext();
            var actividadIds = await ctx.ProjectActivity
                .Where(a => a.ProjectId == proyectoId && a.State && a.Active)
                .Select(a => a.ProjectActivityId)
                .ToListAsync();
            var idSet = actividadIds.ToHashSet();

            var relaciones = await ctx.ActivityPredecessors
                .Where(r => idSet.Contains(r.ActivityId))
                .Select(r => new { r.ActivityId, r.PredecessorId })
                .ToListAsync();

            // Grafo predecesora → sucesora, reemplazando las predecesoras de activityId
            var sucesores = new Dictionary<int, List<int>>();
            void AddEdge(int pred, int succ)
            {
                if (!sucesores.TryGetValue(pred, out var list))
                    sucesores[pred] = list = new List<int>();
                list.Add(succ);
            }

            foreach (var r in relaciones)
            {
                if (r.ActivityId == activityId) continue; // se reemplazan
                AddEdge(r.PredecessorId, r.ActivityId);
            }
            foreach (var pred in nuevasPredecesoras.Distinct())
                AddEdge(pred, activityId);

            return TieneCiclo(sucesores);
        }

        /// <summary>Detección de ciclo por DFS con colores (blanco/gris/negro).</summary>
        private static bool TieneCiclo(Dictionary<int, List<int>> sucesores)
        {
            var estado = new Dictionary<int, int>(); // 0=sin visitar, 1=en pila, 2=terminado

            bool Dfs(int nodo)
            {
                estado[nodo] = 1;
                if (sucesores.TryGetValue(nodo, out var hijos))
                {
                    foreach (var h in hijos)
                    {
                        var e = estado.GetValueOrDefault(h, 0);
                        if (e == 1) return true;           // arista hacia nodo en pila → ciclo
                        if (e == 0 && Dfs(h)) return true;
                    }
                }
                estado[nodo] = 2;
                return false;
            }

            foreach (var nodo in sucesores.Keys)
                if (estado.GetValueOrDefault(nodo, 0) == 0 && Dfs(nodo))
                    return true;
            return false;
        }

        // ─────────────────────────── Cascada FS ───────────────────────────

        public async Task<CascadaResultDto> RecalcularCascadaAsync(int proyectoId)
        {
            using var ctx = _factory.CreateDbContext();
            var (cambios, _) = await CalcularCascadaAsync(ctx, proyectoId);
            return new CascadaResultDto { HayCambios = cambios.Count > 0, Cambios = cambios };
        }

        public async Task<CascadaResultDto> AplicarCascadaAsync(int proyectoId)
        {
            using var ctx = _factory.CreateDbContext();
            var (cambios, actividades) = await CalcularCascadaAsync(ctx, proyectoId);

            foreach (var c in cambios)
            {
                var act = actividades[c.ProjectActivityId];
                act.PlannedStartDate = c.InicioNuevo;
                act.PlannedEndDate = c.FinNuevo;
                act.UpdatedDateTime = DateTime.UtcNow;
            }
            await ctx.SaveChangesAsync();

            // Tras mover actividades (hojas y padres), sincronizar fechas padre ↔ hijos
            _ = await RecalcularFechasPadresInternoAsync(ctx, proyectoId);

            return new CascadaResultDto { HayCambios = cambios.Count > 0, Cambios = cambios };
        }

        /// <summary>
        /// Núcleo del cálculo de cascada. Devuelve los cambios y el diccionario de
        /// entidades (rastreadas por <paramref name="ctx"/>) para permitir persistir.
        /// Soporta predecesoras tanto para hojas como para nodos padre.
        /// </summary>
        private async Task<(List<CascadaCambioDto> cambios, Dictionary<int, ProjectActivity> actividades)>
            CalcularCascadaAsync(AppDbContext ctx, int proyectoId)
        {
            var feriados = await ctx.Feriados
                .Select(f => f.Fecha.ToDateTime(TimeOnly.MinValue))
                .ToListAsync();
            var feriadoSet = feriados.Select(f => f.Date).ToHashSet();

            var lista = await ctx.ProjectActivity
                .Where(a => a.ProjectId == proyectoId && a.State && a.Active)
                .ToListAsync();
            var actividades = lista.ToDictionary(a => a.ProjectActivityId);
            var idSet = actividades.Keys.ToHashSet();

            var relaciones = await ctx.ActivityPredecessors
                .Where(r => idSet.Contains(r.ActivityId))
                .Select(r => new { r.ActivityId, r.PredecessorId })
                .ToListAsync();

            // Jerarquía padre → hijos directos (para desplazar subárboles cuando el sucesor es padre)
            var hijosDe = lista
                .Where(a => a.ParentId.HasValue && idSet.Contains(a.ParentId.Value))
                .GroupBy(a => a.ParentId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(a => a.ProjectActivityId).ToList());
            var idsPadre = hijosDe.Keys.ToHashSet();

            // Predecesoras por sucesora + adyacencia sucesora (para Kahn) + grado de entrada
            var predecesorasDe = new Dictionary<int, List<int>>();
            var sucesoresDe = new Dictionary<int, List<int>>();
            var inDegree = new Dictionary<int, int>();
            foreach (var id in idSet) inDegree[id] = 0;

            foreach (var r in relaciones)
            {
                if (!idSet.Contains(r.PredecessorId)) continue;
                if (!predecesorasDe.TryGetValue(r.ActivityId, out var ps))
                    predecesorasDe[r.ActivityId] = ps = new List<int>();
                ps.Add(r.PredecessorId);

                if (!sucesoresDe.TryGetValue(r.PredecessorId, out var ss))
                    sucesoresDe[r.PredecessorId] = ss = new List<int>();
                ss.Add(r.ActivityId);

                inDegree[r.ActivityId]++;
            }

            // Duración hábil original (solo hojas) y fin "vigente" (se actualiza en cascada)
            var duracion = new Dictionary<int, int>();
            var finVigente = new Dictionary<int, DateTime?>();
            foreach (var a in lista)
            {
                if (a.PlannedStartDate.HasValue && a.PlannedEndDate.HasValue)
                    duracion[a.ProjectActivityId] = DuracionHabilInclusive(
                        a.PlannedStartDate.Value, a.PlannedEndDate.Value, feriadoSet);
                else
                    duracion[a.ProjectActivityId] = 1;

                finVigente[a.ProjectActivityId] = a.PlannedEndDate.HasValue
                    ? a.PlannedEndDate.Value.ToDateTime(TimeOnly.MinValue)
                    : null;
            }

            var cambios = new List<CascadaCambioDto>();

            // Orden topológico (Kahn) sobre las relaciones de predecesoras
            var queue = new Queue<int>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
            while (queue.Count > 0)
            {
                var id = queue.Dequeue();

                if (predecesorasDe.TryGetValue(id, out var preds) && preds.Count > 0)
                {
                    var finesPred = preds
                        .Select(p => finVigente.GetValueOrDefault(p))
                        .Where(f => f.HasValue)
                        .Select(f => f!.Value)
                        .ToList();

                    if (finesPred.Count > 0)
                    {
                        var maxFin = finesPred.Max();
                        var nuevoInicio = NextBusinessDay(maxFin, feriados);
                        var inicioNuevoDo = DateOnly.FromDateTime(nuevoInicio);
                        var act = actividades[id];

                        if (idsPadre.Contains(id))
                        {
                            // Sucesor es nodo padre: desplazar todo el subárbol por el mismo delta calendario.
                            // Preserva las duraciones y offsets relativos entre los hijos.
                            if (act.PlannedStartDate.HasValue)
                            {
                                int deltaCalDias = inicioNuevoDo.DayNumber - act.PlannedStartDate.Value.DayNumber;
                                if (deltaCalDias != 0)
                                    DesplazarSubarbol(id, deltaCalDias, actividades, hijosDe, cambios, finVigente);
                            }
                        }
                        else
                        {
                            // Sucesor es hoja: reposicionar preservando duración hábil original.
                            // Si la actividad nunca tuvo fecha fin, no se autocompleta con inicio+1:
                            // se deja en null hasta que el usuario la ingrese a mano.
                            DateOnly? finNuevoDo = act.PlannedEndDate.HasValue
                                ? DateOnly.FromDateTime(AddBusinessDays(nuevoInicio, duracion[id] - 1, feriados))
                                : null;

                            if (act.PlannedStartDate != inicioNuevoDo || act.PlannedEndDate != finNuevoDo)
                            {
                                // Primera vez que la actividad recibe fecha (ahora por cascada) y su LB
                                // sigue vacía: copiarla. Igual que en Crear/EditarActividad — no mezclar,
                                // no pisar si ya tiene valor.
                                if (act.BaselineStartDate == null)
                                    act.BaselineStartDate = inicioNuevoDo;
                                if (act.BaselineEndDate == null)
                                    act.BaselineEndDate = finNuevoDo;

                                cambios.Add(new CascadaCambioDto
                                {
                                    ProjectActivityId = id,
                                    ActivityDescription = act.ActivityDescription,
                                    InicioAnterior = act.PlannedStartDate,
                                    InicioNuevo = inicioNuevoDo,
                                    FinAnterior = act.PlannedEndDate,
                                    FinNuevo = finNuevoDo,
                                    BaselineStartDate = act.BaselineStartDate,
                                    BaselineEndDate = act.BaselineEndDate
                                });
                            }
                            finVigente[id] = finNuevoDo?.ToDateTime(TimeOnly.MinValue);
                        }
                    }
                }

                if (sucesoresDe.TryGetValue(id, out var succs))
                {
                    foreach (var s in succs)
                    {
                        inDegree[s]--;
                        if (inDegree[s] == 0) queue.Enqueue(s);
                    }
                }
            }

            return (cambios, actividades);
        }

        /// <summary>
        /// Desplaza el nodo raíz y todos sus descendientes por <paramref name="deltaCalDias"/>
        /// días calendario, manteniendo las duraciones y offsets internos del subárbol.
        /// Registra los cambios en <paramref name="cambios"/> y actualiza
        /// <paramref name="finVigente"/> para que los sucesores del subárbol vean las fechas correctas.
        /// </summary>
        private static void DesplazarSubarbol(
            int rootId,
            int deltaCalDias,
            Dictionary<int, ProjectActivity> actividades,
            Dictionary<int, List<int>> hijosDe,
            List<CascadaCambioDto> cambios,
            Dictionary<int, DateTime?> finVigente)
        {
            var nodo = actividades[rootId];
            var inicioAnterior = nodo.PlannedStartDate;
            var finAnterior = nodo.PlannedEndDate;

            DateOnly? nuevoInicio = inicioAnterior.HasValue
                ? inicioAnterior.Value.AddDays(deltaCalDias)
                : (DateOnly?)null;
            DateOnly? nuevoFin = finAnterior.HasValue
                ? finAnterior.Value.AddDays(deltaCalDias)
                : (DateOnly?)null;

            if (inicioAnterior != nuevoInicio || finAnterior != nuevoFin)
            {
                // Misma regla que en Crear/EditarActividad: LB automática solo para hojas
                // (los nodos padre no reciben LB automática), y solo si sigue vacía.
                bool esHoja = !hijosDe.ContainsKey(rootId);
                if (esHoja)
                {
                    if (nodo.BaselineStartDate == null)
                        nodo.BaselineStartDate = nuevoInicio;
                    if (nodo.BaselineEndDate == null)
                        nodo.BaselineEndDate = nuevoFin;
                }

                cambios.Add(new CascadaCambioDto
                {
                    ProjectActivityId = rootId,
                    ActivityDescription = nodo.ActivityDescription,
                    InicioAnterior = inicioAnterior,
                    InicioNuevo = nuevoInicio,
                    FinAnterior = finAnterior,
                    FinNuevo = nuevoFin,
                    BaselineStartDate = nodo.BaselineStartDate,
                    BaselineEndDate = nodo.BaselineEndDate
                });
            }

            // Actualizar fin vigente para que los sucesores directos de este nodo vean el fin correcto
            finVigente[rootId] = nuevoFin.HasValue
                ? nuevoFin.Value.ToDateTime(TimeOnly.MinValue)
                : (DateTime?)null;

            if (hijosDe.TryGetValue(rootId, out var hijos))
                foreach (var hijoId in hijos)
                    DesplazarSubarbol(hijoId, deltaCalDias, actividades, hijosDe, cambios, finVigente);
        }

        // ─────────────────────────── Fechas de nodos padre ───────────────────────────

        public async Task<List<ActividadDto>> RecalcularFechasPadresAsync(int proyectoId)
        {
            using var ctx = _factory.CreateDbContext();
            var modificados = await RecalcularFechasPadresInternoAsync(ctx, proyectoId);
            return modificados.Select(a => new ActividadDto
            {
                ProjectActivityId = a.ProjectActivityId,
                ProjectId         = a.ProjectId,
                ActivityDescription = a.ActivityDescription,
                PlannedStartDate  = a.PlannedStartDate,
                PlannedEndDate    = a.PlannedEndDate,
                ActualEndDate     = a.ActualEndDate,
                BaselineStartDate = a.BaselineStartDate,
                BaselineEndDate   = a.BaselineEndDate,
                ProgressPercentage = a.ProgressPercentage,
                Order             = a.Order,
                HierarchyLevel    = a.HierarchyLevel,
                ParentId          = a.ParentId,
                EsPadre           = true
            }).ToList();
        }

        private static async Task<List<ProjectActivity>> RecalcularFechasPadresInternoAsync(AppDbContext ctx, int proyectoId)
        {
            // 1. TODAS las actividades activas del proyecto, sin filtro por nivel.
            var todas = await ctx.ProjectActivity
                .Where(a => a.ProjectId == proyectoId && a.State && a.Active)
                .ToListAsync();

            if (todas.Count == 0) return new List<ProjectActivity>();

            // 2. esPadre ≡ aparece como clave en hijosDe
            //    (existe ≥1 actividad activa con ParentId = ese id).
            var hijosDe = todas
                .Where(a => a.ParentId.HasValue)
                .GroupBy(a => a.ParentId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var modificados = new List<ProjectActivity>();

            // 3. Orden bottom-up REAL: HierarchyLevel DESCENDENTE.
            //    Los nodos más profundos se procesan primero; cuando llegamos a un padre
            //    en nivel L, todos sus hijos (nivel > L) ya tienen fechas actualizadas en memoria.
            //    No depende de la coherencia de ParentId para el orden — solo para detectar hijos.
            foreach (var nodo in todas.OrderByDescending(a => a.HierarchyLevel))
            {
                if (!hijosDe.TryGetValue(nodo.ProjectActivityId, out var hijos) || hijos.Count == 0)
                    continue; // hoja: conserva sus fechas

                // 4. Fecha padre = MIN inicio / MAX fin de sus hijos directos.
                //    Los hijos ya fueron actualizados (están a nivel > nodo.HierarchyLevel).
                var inicios = hijos.Where(h => h.PlannedStartDate.HasValue)
                                   .Select(h => h.PlannedStartDate!.Value).ToList();
                var fines   = hijos.Where(h => h.PlannedEndDate.HasValue)
                                   .Select(h => h.PlannedEndDate!.Value).ToList();

                var nuevoInicio = inicios.Count > 0 ? inicios.Min() : (DateOnly?)null;
                var nuevoFin    = fines.Count   > 0 ? fines.Max()   : (DateOnly?)null;

                if (nodo.PlannedStartDate != nuevoInicio || nodo.PlannedEndDate != nuevoFin)
                {
                    nodo.PlannedStartDate = nuevoInicio;
                    nodo.PlannedEndDate = nuevoFin;
                    nodo.UpdatedDateTime = DateTime.UtcNow;
                    modificados.Add(nodo);
                }
            }

            await ctx.SaveChangesAsync();
            return modificados;
        }
    }
}

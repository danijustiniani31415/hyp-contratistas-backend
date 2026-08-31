using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Services.Revisores.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Shared.Services.Revisores.Services
{
    /// <summary>
    /// Resolución del jefe/revisor de un trabajador en tres pasos:
    ///   1) <c>workers_revisores</c>: el jefe personalizado del trabajador (el checkbox del
    ///      formulario de trabajadores guarda uno; la tabla admite n por prioridad).
    ///   2) <c>area_revisores</c>: n revisores por área, por prioridad, partiendo del
    ///      nodo area_scope del trabajador (workers.area_scope_id) y subiendo por el
    ///      árbol hasta el primer nodo con revisores (los revisores se configuran solo
    ///      en el primer nodo "Área de Gerencia" y el primer "Área Estándar" de cada
    ///      rama, así que un trabajador cae en su área estándar y, si esa no tiene
    ///      revisores, en la gerencia de la que cuelga).
    ///   3) Fallback: el área de GTH (area_scope.email).
    ///
    /// En los tres pasos rige que nadie puede ser su propio jefe: ver <see cref="EsLaMismaPersona"/>.
    ///
    /// Todo se resuelve por lotes: <see cref="ResolveManyAsync"/> hace un número FIJO de
    /// consultas sea para 1 o para 500 trabajadores, y <see cref="ResolveAsync"/> es un
    /// atajo sobre ella (una sola ruta de código, sin lógica duplicada).
    /// </summary>
    public class JefeRevisorResolver : IJefeRevisorResolver
    {
        private const string EmailDomainCorp = "@abril.pe";
        /// <summary>Nombre exacto del área en area_item cuyo area_scope.email es el fallback.</summary>
        private const string AreaGthNombre = "Gestión del Talento Humano";

        private readonly IDbContextFactory<AppDbContext> _factory;

        public JefeRevisorResolver(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<JefeRevisorResolution?> ResolveAsync(int workerId)
        {
            var resueltos = await ResolveManyAsync(new[] { workerId });
            return resueltos.TryGetValue(workerId, out var jefe) ? jefe : null;
        }

        public async Task<Dictionary<int, JefeRevisorResolution>> ResolveManyAsync(IReadOnlyCollection<int> workerIds)
        {
            var resultado = new Dictionary<int, JefeRevisorResolution>();

            var ids = workerIds.Where(id => id > 0).Distinct().ToList();
            if (ids.Count == 0) return resultado;

            using var ctx = _factory.CreateDbContext();

            // Ficha de cada trabajador pedido: su persona (para descartarse a sí mismo como jefe)
            // y su nodo de área (paso 2). Se trae una sola vez y la usan los dos pasos.
            var fichas = (await ctx.Worker.AsNoTracking()
                    .Where(w => ids.Contains(w.Id))
                    .Select(w => new { w.Id, w.PersonId, w.AreaScopeId })
                    .ToListAsync())
                .ToDictionary(w => w.Id, w => (w.PersonId, w.AreaScopeId));

            // ── Paso 1: jefe personalizado (workers_revisores) ─────────────────────
            var directos = await (
                from r in ctx.WorkersRevisores.AsNoTracking()
                where r.State && r.Active && ids.Contains(r.SolicitanteId)
                join w in ctx.Worker.AsNoTracking() on r.RevisorId equals w.Id
                where w.EmailCorporativo != null
                      && w.EmailCorporativo.Trim().ToLower().EndsWith(EmailDomainCorp)
                select new
                {
                    r.SolicitanteId,
                    r.OrdenPrioridad,
                    r.WorkersRevisoresId,
                    RevisorWorkerId = w.Id,
                    RevisorPersonId = w.PersonId,
                    w.EmailCorporativo,
                    Nombre = w.Person != null ? w.Person.FullName : null,
                }
            ).ToListAsync();

            foreach (var grupo in directos.GroupBy(d => d.SolicitanteId))
            {
                // El propio trabajador no puede ser su revisor.
                var personaSolicitante = PersonaDe(fichas, grupo.Key);
                var elegido = grupo
                    .Where(d => !EsLaMismaPersona(d.RevisorWorkerId, d.RevisorPersonId, grupo.Key, personaSolicitante))
                    .OrderBy(d => d.OrdenPrioridad)
                    .ThenBy(d => d.WorkersRevisoresId)
                    .FirstOrDefault();

                if (elegido != null)
                    resultado[grupo.Key] = new JefeRevisorResolution(
                        elegido.RevisorWorkerId, null, elegido.EmailCorporativo!.Trim(),
                        elegido.Nombre, elegido.RevisorPersonId);
            }

            var pendientes = ids.Where(id => !resultado.ContainsKey(id)).ToList();
            if (pendientes.Count == 0) return resultado;

            // ── Paso 2: revisores del área (area_revisores, subiendo por el árbol) ──
            await ResolveByAreaAsync(ctx, pendientes, fichas, resultado);

            pendientes = ids.Where(id => !resultado.ContainsKey(id)).ToList();
            if (pendientes.Count == 0) return resultado;

            // ── Paso 3: fallback al área de GTH ───────────────────────────────────
            var gth = await GetFallbackGthAsync(ctx);

            if (gth != null)
                foreach (var id in pendientes)
                    resultado[id] = gth;

            return resultado;
        }

        public async Task<Dictionary<int, AreaScopeRevisorPreview>> ResolveByAreaScopeManyAsync(
            IReadOnlyCollection<int> areaScopeIds)
        {
            var resultado = new Dictionary<int, AreaScopeRevisorPreview>();

            var ids = areaScopeIds.Where(id => id > 0).Distinct().ToList();
            if (ids.Count == 0) return resultado;

            using var ctx = _factory.CreateDbContext();

            var scopes = await ctx.AreaScope.AsNoTracking()
                .Where(s => s.State)
                .Select(s => new { s.AreaScopeId, s.AreaScopeParentId })
                .ToListAsync();
            var parentById = scopes.ToDictionary(s => s.AreaScopeId, s => s.AreaScopeParentId);

            var cadenaPorNodo = ConstruirCadenas(ids, parentById);
            var nodos = cadenaPorNodo.Values.SelectMany(c => c).Distinct().ToList();

            var nodosFiltranProyecto = (await ctx.GaSalidasAreaConfig.AsNoTracking()
                .Where(f => f.State && f.FiltraPorProyecto && nodos.Contains(f.AreaScopeId))
                .Select(f => f.AreaScopeId)
                .ToListAsync()).ToHashSet();

            var candidatos = await CargarCandidatosAsync(ctx, nodos);
            var porNodo = candidatos.ToLookup(c => c.AreaScopeId);
            var gth = await GetFallbackGthAsync(ctx);

            foreach (var (nodoId, cadena) in cadenaPorNodo)
            {
                // Sin proyecto: en todo nodo (filtrado o no) aplica el revisor a nivel de área,
                // igual que hace la resolución por trabajador cuando el trabajador no tiene proyecto.
                var area = Ordenar(cadena, nodo => PorPrioridad(porNodo[nodo].Where(c => c.ProjectId == null)), gth);

                // Por proyecto: solo tiene sentido para los proyectos que aparecen configurados en
                // algún nodo filtrado de la cadena; en el resto el revisor es el de área.
                var proyectos = cadena
                    .Where(nodosFiltranProyecto.Contains)
                    .SelectMany(nodo => porNodo[nodo])
                    .Where(c => c.ProjectId != null)
                    .Select(c => c.ProjectId!.Value)
                    .Distinct()
                    .ToList();

                var porProyecto = new Dictionary<int, IReadOnlyList<JefeRevisorResolution>>();
                foreach (var projectId in proyectos)
                {
                    var lista = Ordenar(cadena, nodo =>
                    {
                        var delNodo = porNodo[nodo];
                        if (!nodosFiltranProyecto.Contains(nodo))
                            return PorPrioridad(delNodo.Where(c => c.ProjectId == null));

                        // Nodo filtrado: primero los revisores del proyecto y detrás los de área
                        // (project_id NULL) del mismo nodo. Es el mismo orden de preferencia que
                        // aplica la resolución por trabajador, que cae al revisor de área del nodo
                        // cuando el del proyecto queda descartado — así la previsualización no
                        // puede mostrar a alguien distinto de quien va a recibir el correo.
                        return PorPrioridad(delNodo.Where(c => c.ProjectId == projectId))
                            .Concat(PorPrioridad(delNodo.Where(c => c.ProjectId == null)));
                    }, gth);
                    if (lista.Count > 0) porProyecto[projectId] = lista;
                }

                resultado[nodoId] = new AreaScopeRevisorPreview(area, porProyecto);
            }

            return resultado;
        }

        /// <summary>
        /// Regla transversal del servicio: nadie puede ser su propio jefe.
        ///
        /// La comparación es por PERSONA cuando ambos lados la tienen, y por ficha
        /// (<c>workers.id</c>) como respaldo. Comparar solo por ficha dejaría pasar el caso de un
        /// reingreso: la misma persona tiene entonces varias filas en <c>workers</c> y el revisor
        /// del área puede estar configurado en una ficha distinta de la que se está resolviendo.
        /// </summary>
        private static bool EsLaMismaPersona(
            int candidatoWorkerId, int? candidatoPersonId, int workerId, int? personId)
            => candidatoWorkerId == workerId
               || (candidatoPersonId != null && personId != null && candidatoPersonId == personId);

        private static int? PersonaDe(
            IReadOnlyDictionary<int, (int? PersonId, int? AreaScopeId)> fichas, int workerId)
            => fichas.TryGetValue(workerId, out var ficha) ? ficha.PersonId : null;

        /// <summary>
        /// Los candidatos del nodo y de sus ancestros EN ORDEN de resolución (nodo más cercano
        /// primero, y dentro de cada nodo el orden que arme <paramref name="candidatosOrdenadosDe"/>),
        /// con el fallback de GTH al final. Se deja una sola entrada por persona: quien ya apareció
        /// no vuelve a aparecer más arriba del árbol (hay jefes que son revisores de su área y
        /// también de la gerencia de la que cuelga).
        ///
        /// Es lo que devuelve la previsualización por área en vez de solo el ganador: quien la
        /// consume conoce al trabajador y tiene que poder descartarlo para que no salga como su
        /// propio jefe, quedándose con el primer candidato que no sea él.
        /// </summary>
        private static List<JefeRevisorResolution> Ordenar(
            List<int> cadena,
            Func<int, IEnumerable<RevisorCandidato>> candidatosOrdenadosDe,
            JefeRevisorResolution? fallback)
        {
            var lista = new List<JefeRevisorResolution>();
            var vistos = new HashSet<string>();

            foreach (var nodo in cadena)
                foreach (var c in candidatosOrdenadosDe(nodo))
                {
                    var clave = c.RevisorPersonId != null ? $"p{c.RevisorPersonId}" : $"w{c.RevisorWorkerId}";
                    if (vistos.Add(clave)) lista.Add(AResolucion(c));
                }

            if (fallback != null) lista.Add(fallback);
            return lista;
        }

        /// <summary>Los candidatos de un nodo en el orden con el que se elige entre ellos.</summary>
        private static IEnumerable<RevisorCandidato> PorPrioridad(IEnumerable<RevisorCandidato> candidatos)
            => candidatos.OrderBy(c => c.OrdenPrioridad).ThenBy(c => c.AreaRevisoresId);

        private static JefeRevisorResolution AResolucion(RevisorCandidato c) => new(
            c.RevisorWorkerId, null, c.EmailCorporativo.Trim(), c.Nombre, c.RevisorPersonId);

        /// <summary>Cadena nodo → raíz de cada nodo pedido, cortando ciclos por si el árbol quedó mal.</summary>
        private static Dictionary<int, List<int>> ConstruirCadenas(
            IEnumerable<int> desdeIds,
            Dictionary<int, int?> parentById)
        {
            var cadenas = new Dictionary<int, List<int>>();
            foreach (var desde in desdeIds)
            {
                var cadena = new List<int>();
                var visitados = new HashSet<int>();
                int? actual = desde;
                while (actual != null && visitados.Add(actual.Value))
                {
                    cadena.Add(actual.Value);
                    parentById.TryGetValue(actual.Value, out actual);
                }
                if (cadena.Count > 0) cadenas[desde] = cadena;
            }
            return cadenas;
        }

        /// <summary>Revisor de área candidato: fila viva y activa de area_revisores con correo corporativo válido.</summary>
        private sealed record RevisorCandidato(
            int AreaScopeId, int? ProjectId, int OrdenPrioridad, int AreaRevisoresId,
            int RevisorWorkerId, int? RevisorPersonId, string EmailCorporativo, string? Nombre);

        private static async Task<List<RevisorCandidato>> CargarCandidatosAsync(AppDbContext ctx, List<int> nodos)
        {
            return await (
                from r in ctx.AreaRevisores.AsNoTracking()
                where r.State && r.Active && nodos.Contains(r.AreaScopeId)
                join w in ctx.Worker.AsNoTracking() on r.RevisorId equals w.Id
                where w.EmailCorporativo != null
                      && w.EmailCorporativo.Trim().ToLower().EndsWith(EmailDomainCorp)
                select new RevisorCandidato(
                    r.AreaScopeId, r.ProjectId, r.OrdenPrioridad, r.AreaRevisoresId,
                    w.Id, w.PersonId, w.EmailCorporativo!, w.Person != null ? w.Person.FullName : null)
            ).ToListAsync();
        }

        private static async Task<JefeRevisorResolution?> GetFallbackGthAsync(AppDbContext ctx)
        {
            var gth = await (
                from s in ctx.AreaScope.AsNoTracking()
                join ai in ctx.AreaItem.AsNoTracking() on s.AreaItemId equals ai.AreaItemId
                where s.State && ai.State
                      && ai.AreaItemName == AreaGthNombre
                      && s.Email != null && s.Email != ""
                orderby s.AreaScopeId
                select new { s.AreaScopeId, s.Email, ai.AreaItemName }
            ).FirstOrDefaultAsync();

            return gth == null
                ? null
                : new JefeRevisorResolution(null, gth.AreaScopeId, gth.Email!.Trim(), gth.AreaItemName);
        }

        /// <summary>
        /// Busca revisores de área para los trabajadores indicados: para cada uno arma la
        /// cadena de nodos desde su area_scope hacia la raíz y toma, del primer nodo con
        /// revisores válidos, el de mayor prioridad. El propio trabajador no puede ser su
        /// revisor: si es el único revisor de su área se sigue subiendo por el árbol (el jefe
        /// de un área acaba dependiendo del revisor de la gerencia de la que cuelga). Escribe
        /// en <paramref name="resultado"/> solo los que resuelve.
        ///
        /// Si un nodo está marcado como "filtrar por proyecto" (ga_salidas_area_config),
        /// se usa el revisor del proyecto al que pertenece el trabajador
        /// (ga_salidas_workers_project → area_revisores.project_id); si ese nodo no tiene
        /// revisor para ese proyecto (o el trabajador no tiene proyecto), se cae al
        /// revisor a nivel de área del mismo nodo (project_id NULL). Los nodos no filtrados
        /// usan siempre el revisor a nivel de área. Todo se generaliza por configuración,
        /// sin reglas especiales por nombre de área.
        /// </summary>
        private static async Task ResolveByAreaAsync(
            AppDbContext ctx,
            List<int> workerIds,
            IReadOnlyDictionary<int, (int? PersonId, int? AreaScopeId)> fichas,
            Dictionary<int, JefeRevisorResolution> resultado)
        {
            var areaScopePorWorker = workerIds
                .Where(id => fichas.TryGetValue(id, out var ficha) && ficha.AreaScopeId != null)
                .ToDictionary(id => id, id => fichas[id].AreaScopeId!.Value);
            if (areaScopePorWorker.Count == 0) return;

            // Árbol vivo (tabla pequeña) para armar las cadenas trabajador → raíz en memoria.
            var scopes = await ctx.AreaScope.AsNoTracking()
                .Where(s => s.State)
                .Select(s => new { s.AreaScopeId, s.AreaScopeParentId })
                .ToListAsync();
            var parentById = scopes.ToDictionary(s => s.AreaScopeId, s => s.AreaScopeParentId);

            // Cadena por nodo (misma rutina que usa la previsualización por área) y de ahí por worker.
            var cadenaPorScope = ConstruirCadenas(areaScopePorWorker.Values.Distinct(), parentById);
            var cadenaPorWorker = areaScopePorWorker
                .Where(kv => cadenaPorScope.ContainsKey(kv.Value))
                .ToDictionary(kv => kv.Key, kv => cadenaPorScope[kv.Value]);
            if (cadenaPorWorker.Count == 0) return;

            var nodos = cadenaPorWorker.Values.SelectMany(c => c).Distinct().ToList();

            // Proyecto de cada trabajador (si pertenece a alguno) y nodos que filtran por proyecto.
            var proyectoPorWorker = await ctx.GaSalidasWorkersProject.AsNoTracking()
                .Where(wp => wp.State && workerIds.Contains(wp.WorkerId))
                .Select(wp => new { wp.WorkerId, wp.ProjectId })
                .ToListAsync();
            var proyectoDe = proyectoPorWorker
                .GroupBy(wp => wp.WorkerId)
                .ToDictionary(g => g.Key, g => (int?)g.First().ProjectId);

            var nodosFiltranProyecto = (await ctx.GaSalidasAreaConfig.AsNoTracking()
                .Where(f => f.State && f.FiltraPorProyecto && nodos.Contains(f.AreaScopeId))
                .Select(f => f.AreaScopeId)
                .ToListAsync()).ToHashSet();

            // Revisores vivos + activos con correo válido de cualquier nodo involucrado.
            var candidatos = await CargarCandidatosAsync(ctx, nodos);
            if (candidatos.Count == 0) return;

            var porNodo = candidatos.ToLookup(c => c.AreaScopeId);

            foreach (var (workerId, cadena) in cadenaPorWorker)
            {
                proyectoDe.TryGetValue(workerId, out var proyectoTrabajador);
                var personaTrabajador = PersonaDe(fichas, workerId);

                // Conjunto efectivo de candidatos por nodo según el filtro por proyecto.
                var efectivos = cadena
                    .SelectMany(nodo =>
                    {
                        var delNodo = porNodo[nodo]
                            .Where(c => !EsLaMismaPersona(
                                c.RevisorWorkerId, c.RevisorPersonId, workerId, personaTrabajador))
                            .ToList();

                        // Nodo no filtrado: revisor a nivel de área (project_id NULL).
                        if (!nodosFiltranProyecto.Contains(nodo))
                            return delNodo.Where(c => c.ProjectId == null);

                        // Nodo filtrado: preferir revisor del proyecto del trabajador; si no hay, área (NULL).
                        var porProyecto = delNodo
                            .Where(c => proyectoTrabajador != null && c.ProjectId == proyectoTrabajador)
                            .ToList();
                        return porProyecto.Count > 0
                            ? porProyecto.AsEnumerable()
                            : delNodo.Where(c => c.ProjectId == null);
                    })
                    .ToList();

                var elegido = efectivos
                    .OrderBy(c => cadena.IndexOf(c.AreaScopeId)) // nodo más cercano al trabajador primero
                    .ThenBy(c => c.OrdenPrioridad)
                    .ThenBy(c => c.AreaRevisoresId)
                    .FirstOrDefault();

                if (elegido != null) resultado[workerId] = AResolucion(elegido);
            }
        }
    }
}

using Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Application.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Application.Services
{
    // ══════════════════════════════════════════════════════════════════════════
    // JefeResolver (ApproverResolver) — SIN USO desde 2026-07-13.
    //
    // Este algoritmo de jerarquía (árbol area_scope + categoría del trabajador)
    // fue reemplazado por la tabla `workers_revisores` (n revisores por trabajador,
    // por prioridad) con fallback al área de GTH (area_scope.email). Ver
    // SalidaRevisorResolver. Se conserva el código por si se necesita retomar el
    // algoritmo en el futuro — buscar "JefeResolver" para llegar rápido aquí.
    // ══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Resuelve el correo del aprobador para una solicitud de salida usando el árbol
    /// <c>area_scope</c> y la categoría del trabajador.
    ///
    /// Reglas:
    ///   1) Gerente                  → no necesita aprobador (null).
    ///   2) Jefe / Sub Gerente        → directo al Gerente del macro-área (mismo root).
    ///   3) Resto                    → walk-up por la cadena ancestral
    ///                                 buscando Jefe → Sub Gerente → Coordinador.
    ///                                 Si la cadena no devuelve nada → fallback al
    ///                                 Gerente del macro-área.
    ///
    /// Sólo se consideran como aprobadores trabajadores con <c>email_corporativo</c>
    /// que termine en <c>@abril.pe</c> (correo corporativo).
    /// </summary>
    public class ApproverResolver : IApproverResolver
    {
        private const string EmailDomainCorp = "@abril.pe";


        // Categorías que pueden aprobar a un trabajador "regular" (regla C).
        private static readonly int[] CategoriasWalkUp = CategoriaIds.AprobadoresWalkUp;

        private readonly IDbContextFactory<AppDbContext> _factory;

        public ApproverResolver(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<ApproverResolution?> ResolveApproverAsync(Worker user)
        {
            using var ctx = _factory.CreateDbContext();

            // La categoría del trabajador ya no vive en workers: se resuelve por su puesto
            // (workers.puesto_id -> puesto.categoria_id). Sin puesto no hay categoría, y sin
            // categoría el trabajador cae al walk-up por el árbol como cualquier otro.
            var userCategoriaId = user.PuestoId == null
                ? null
                : await ctx.Puesto.AsNoTracking()
                    .Where(p => p.PuestoId == user.PuestoId.Value)
                    .Select(p => (int?)p.CategoriaId)
                    .FirstOrDefaultAsync();

            // Regla 0 (override manual): si el trabajador tiene un revisor de salidas asignado
            // (workers.worker_salida_jefe_id, sección "Revisor de Salidas") y ese jefe tiene
            // correo corporativo @abril.pe, se usa directamente. Tiene prioridad sobre todo el
            // algoritmo del árbol. Si el campo es null o el jefe no tiene correo válido, se cae
            // al algoritmo de jerarquía (fallback) definido más abajo.
            if (user.WorkerSalidaJefeId.HasValue)
            {
                var jefe = await ctx.Worker
                    .AsNoTracking()
                    .Where(w => w.Id == user.WorkerSalidaJefeId.Value && w.Id != user.Id)
                    .Select(w => new { w.Id, w.EmailCorporativo })
                    .FirstOrDefaultAsync();

                if (jefe != null && jefe.EmailCorporativo != null &&
                    jefe.EmailCorporativo.Trim().EndsWith(EmailDomainCorp, StringComparison.OrdinalIgnoreCase))
                {
                    return new ApproverResolution(jefe.Id, jefe.EmailCorporativo.Trim());
                }
                // Sin correo válido → continúa al fallback por jerarquía.
            }

            // Regla A: el Gerente no necesita aprobador
            if (userCategoriaId == CategoriaIds.Gerente)
                return null;

            // Si el trabajador no tiene área en el árbol, no se puede resolver por jerarquía
            if (!user.AreaScopeId.HasValue)
                return null;

            // Cargamos la topología completa del árbol una sola vez (es una tabla
            // chica, decenas de filas) y caminamos en memoria.
            var parentByScope = await ctx.AreaScope
                .AsNoTracking()
                .Select(s => new { s.AreaScopeId, s.AreaScopeParentId })
                .ToDictionaryAsync(s => s.AreaScopeId, s => s.AreaScopeParentId);

            var ancestros = BuildAncestorsChain(user.AreaScopeId.Value, parentByScope);
            var rootId    = ancestros[^1]; // último = raíz

            // Regla B: Jefe / Sub Gerente → salta directo al Gerente del macro-área
            if (userCategoriaId == CategoriaIds.Jefe ||
                userCategoriaId == CategoriaIds.SubGerente)
            {
                return await FindGerenteByRootAsync(ctx, rootId, user.Id, parentByScope);
            }

            // Regla C: walk-up Jefe>SubGer>Coord por la cadena ancestral
            foreach (var scopeId in ancestros)
            {
                var candidatos = await (
                    from w in ctx.Worker.AsNoTracking()
                    where w.AreaScopeId == scopeId
                          && w.Id != user.Id
                          && w.PuestoCatalogo != null
                          && CategoriasWalkUp.Contains(w.PuestoCatalogo.CategoriaId)
                          && w.EmailCorporativo != null
                          && w.EmailCorporativo.EndsWith(EmailDomainCorp)
                    select new { w.Id, CategoriaId = (int?)w.PuestoCatalogo!.CategoriaId, w.EmailCorporativo }
                ).ToListAsync();

                if (candidatos.Count == 0) continue;

                var elegido = candidatos
                    .OrderBy(c => CategoriaPriority(c.CategoriaId))
                    .First();

                return new ApproverResolution(elegido.Id, elegido.EmailCorporativo!.Trim());
            }

            // Fallback: ningún Jefe/SubGer/Coord en la cadena → Gerente del macro-área
            return await FindGerenteByRootAsync(ctx, rootId, user.Id, parentByScope);
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        /// <summary>
        /// Devuelve la cadena (scope propio, padre, abuelo, …, raíz) caminando hacia arriba.
        /// Si hay un ciclo de datos (no debería existir), se corta defensivamente.
        /// </summary>
        private static List<int> BuildAncestorsChain(int startScopeId, IDictionary<int, int?> parentByScope)
        {
            var chain = new List<int>();
            var seen  = new HashSet<int>();
            int? curr = startScopeId;
            while (curr.HasValue && seen.Add(curr.Value))
            {
                chain.Add(curr.Value);
                parentByScope.TryGetValue(curr.Value, out var parent);
                curr = parent;
            }
            return chain;
        }

        /// <summary>
        /// Busca el Gerente entre todos los workers cuyo <c>area_scope_id</c> resuelva
        /// al mismo root que el del solicitante. Excluye self.
        /// </summary>
        private static async Task<ApproverResolution?> FindGerenteByRootAsync(
            AppDbContext ctx,
            int rootId,
            int excludeWorkerId,
            IDictionary<int, int?> parentByScope)
        {
            // Pre-calcular qué scopes cuelgan de ese root (incluido él mismo)
            var scopesEnRaiz = parentByScope.Keys
                .Where(scopeId => RootOf(scopeId, parentByScope) == rootId)
                .ToHashSet();

            var gerentes = await (
                from w in ctx.Worker.AsNoTracking()
                where w.AreaScopeId.HasValue
                      && w.Id != excludeWorkerId
                      && w.PuestoCatalogo != null
                      && w.PuestoCatalogo.CategoriaId == CategoriaIds.Gerente
                      && w.EmailCorporativo != null
                      && w.EmailCorporativo.EndsWith(EmailDomainCorp)
                select new { w.Id, w.AreaScopeId, w.EmailCorporativo }
            ).ToListAsync();

            var gerente = gerentes.FirstOrDefault(g => g.AreaScopeId.HasValue && scopesEnRaiz.Contains(g.AreaScopeId.Value));
            return gerente == null ? null : new ApproverResolution(gerente.Id, gerente.EmailCorporativo!.Trim());
        }

        /// <summary>Camina hacia arriba devolviendo el id de la raíz de un scope.</summary>
        private static int RootOf(int scopeId, IDictionary<int, int?> parentByScope)
        {
            var seen = new HashSet<int>();
            int curr = scopeId;
            while (seen.Add(curr) && parentByScope.TryGetValue(curr, out var parent) && parent.HasValue)
            {
                curr = parent.Value;
            }
            return curr;
        }

        /// <summary>
        /// Prioridad del walk-up: Jefe &gt; Sub Gerente &gt; Coordinador. Se deriva del orden
        /// de <see cref="CategoriaIds.AprobadoresWalkUp"/>, así que cambiar ese orden cambia
        /// la prioridad en un solo lugar.
        /// </summary>
        private static int CategoriaPriority(int? categoriaId)
        {
            var i = Array.IndexOf(CategoriasWalkUp, categoriaId ?? 0);
            return i < 0 ? 99 : i + 1;
        }
    }
}

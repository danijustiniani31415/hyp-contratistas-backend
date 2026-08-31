using Abril_Backend.Features.GestionAdministrativa.GestionSalidas.Application.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.GestionAdministrativa.GestionSalidas.Application.Services
{
    /// <summary>
    /// Implementa la resolución de visibilidad. Ver <see cref="ISalidaVisibilityResolver"/>.
    ///
    /// Piso obligatorio (area_revisores): si el usuario está designado como revisor de un
    /// nodo en "Revisores de Áreas", ve ese nodo y todo su subárbol SIEMPRE, sin importar su
    /// categoría de trabajador. No es un caso más del algoritmo: se suma tanto al override
    /// manual como al algoritmo, porque a ese revisor le llegan para aprobar las solicitudes
    /// de toda esa rama y tiene que poder gestionarlas.
    ///
    /// Override (ga_salida_visibilidad_area): si el usuario (a través de su/sus workers)
    /// tiene filas vivas, esas definen su visibilidad — cada fila aporta su nodo y, si
    /// <c>incluye_descendientes</c>, todo el subárbol. El algoritmo NO se aplica en ese caso
    /// (el piso de revisor sí se suma igual).
    ///
    /// Algoritmo (fallback, cuando no hay override):
    ///   • GTH (área "Gestión del Talento Humano" en su cadena)      → ve todo.
    ///   • Gerente (<see cref="CategoriaIds.Gerente"/>)                → su gerencia (raíz Área
    ///                                                                  de Gerencia) + descendientes.
    ///   • Administración de Obra ("Administración de Obra" en cadena)→ las áreas donde hay
    ///                                                                  personal de Obra o Staff
    ///                                                                  (workers.obra_oficina_staff_id).
    /// Las áreas se resuelven por texto (el árbol es administrable por UI); la categoría, por
    /// id, para que renombrarla desde Configuración no apague la regla.
    /// </summary>
    public class SalidaVisibilityResolver : ISalidaVisibilityResolver
    {
        private const string AreaGth          = "Gestión del Talento Humano";
        private const string AreaAdminObra    = "Administración de Obra";

        private readonly IDbContextFactory<AppDbContext> _factory;

        public SalidaVisibilityResolver(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<SalidaVisibility> ResolveAsync(int userId)
        {
            using var ctx = _factory.CreateDbContext();

            // 1. Worker(s) del usuario (un user puede mapear a más de un worker).
            var workers = await (
                from w in ctx.Worker
                join p in ctx.Person on w.PersonId equals p.PersonId
                where p.UserId == userId
                select new
                {
                    w.Id,
                    w.AreaScopeId,
                    // La categoría sale del puesto: workers ya no la guarda.
                    CategoriaId = w.PuestoCatalogo != null ? w.PuestoCatalogo.CategoriaId : (int?)null
                }
            ).ToListAsync();

            if (workers.Count == 0) return new SalidaVisibility(false, new HashSet<int>());

            // Mitad de la condición de tesorero: el puesto. La otra mitad (el rol) sale del token.
            var esCategoriaTesorero = workers.Any(w => w.CategoriaId == CategoriaIds.Tesorero);

            var workerIds = workers.Select(w => w.Id).ToList();

            // 2. Topología del árbol (tabla chica) para expandir descendientes y correr el algoritmo.
            var nodos = await (
                from s in ctx.AreaScope
                join ai in ctx.AreaItem on s.AreaItemId equals ai.AreaItemId
                join at in ctx.AreaType on ai.AreaTypeId equals at.AreaTypeId
                where s.State
                select new { s.AreaScopeId, s.AreaScopeParentId, ItemName = ai.AreaItemName, TypeName = at.AreaTypeName }
            ).ToListAsync();

            var parentById = nodos.ToDictionary(n => n.AreaScopeId, n => n.AreaScopeParentId);
            var itemNameById = nodos.ToDictionary(n => n.AreaScopeId, n => n.ItemName);
            var typeNameById = nodos.ToDictionary(n => n.AreaScopeId, n => n.TypeName);
            var childrenByParent = nodos
                .Where(n => n.AreaScopeParentId.HasValue)
                .GroupBy(n => n.AreaScopeParentId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(x => x.AreaScopeId).ToList());

            // 3. Piso obligatorio: nodos donde el usuario está designado como revisor de área
            //    (area_revisores) → ese nodo + su subárbol, sin importar categoría ni override.
            //    Se exige Active además de State, igual que JefeRevisorResolver al elegir al
            //    revisor de una solicitud: un revisor desactivado no recibe solicitudes, así
            //    que tampoco gana visibilidad. Las filas por proyecto (project_id con valor)
            //    cuentan igual que las de área: la visibilidad se expresa por area_scope, no
            //    tiene dimensión de proyecto, y el revisor del proyecto es revisor de esa área.
            var nodosComoRevisor = await ctx.AreaRevisores
                .Where(r => r.State && r.Active && workerIds.Contains(r.RevisorId))
                .Select(r => r.AreaScopeId)
                .Distinct()
                .ToListAsync();

            var comoRevisor = new HashSet<int>();
            foreach (var nodo in nodosComoRevisor)
            {
                // parentById solo tiene los nodos vivos; se ignoran los de áreas dadas de baja.
                if (!parentById.ContainsKey(nodo)) continue;
                comoRevisor.Add(nodo);
                AddDescendants(nodo, childrenByParent, comoRevisor);
            }

            // 4. Override manual: si existe, define la visibilidad y el algoritmo NO corre
            //    (el piso de revisor de área se suma de todas formas).
            var overrides = await ctx.GaSalidaVisibilidadArea
                .Where(v => v.State && workerIds.Contains(v.WorkerId))
                .Select(v => new { v.AreaScopeId, v.IncluyeDescendientes })
                .ToListAsync();

            if (overrides.Count > 0)
            {
                var set = new HashSet<int>(comoRevisor);
                foreach (var o in overrides)
                {
                    set.Add(o.AreaScopeId);
                    if (o.IncluyeDescendientes)
                        AddDescendants(o.AreaScopeId, childrenByParent, set);
                }
                // Si lo acumulado cubre TODOS los nodos del árbol, equivale a "ver todo"
                // (así también se ven las solicitudes de trabajadores sin area_scope asignado).
                if (nodos.Count > 0 && set.Count >= nodos.Count)
                    return new SalidaVisibility(true, set, esCategoriaTesorero);
                return new SalidaVisibility(false, set, esCategoriaTesorero);
            }

            // 5. Algoritmo (fallback), partiendo del piso de revisor de área.
            var visible = new HashSet<int>(comoRevisor);
            // Se carga una sola vez y solo si algún worker del usuario es de Administración de Obra.
            List<int>? areasConPersonalDeObra = null;
            var todosLosNodos = new Lazy<HashSet<int>>(() => nodos.Select(n => n.AreaScopeId).ToHashSet());

            foreach (var w in workers)
            {
                var cadena = w.AreaScopeId.HasValue ? AncestorsChain(w.AreaScopeId.Value, parentById) : new List<int>();

                // GTH → ve todo.
                if (cadena.Any(id => itemNameById.TryGetValue(id, out var name) &&
                                     string.Equals(name, AreaGth, StringComparison.OrdinalIgnoreCase)))
                {
                    return new SalidaVisibility(true, todosLosNodos.Value, esCategoriaTesorero);
                }

                // Gerente → su gerencia (raíz) + descendientes.
                if (w.CategoriaId == CategoriaIds.Gerente && cadena.Count > 0)
                {
                    var root = cadena[^1];
                    visible.Add(root);
                    AddDescendants(root, childrenByParent, visible);
                }

                // Administración de Obra → las áreas con personal de Obra o Staff.
                //
                // Antes esto se resolvía listando los nodos de tipo "Área Obra_Oficina" del
                // árbol; ese tipo de área se eliminó y la distinción Obra / Staff / Oficina
                // Central pasó a workers.obra_oficina_staff_id, así que ahora el conjunto se
                // deriva de dónde está asignado ese personal.
                if (cadena.Any(id => itemNameById.TryGetValue(id, out var name) &&
                                     string.Equals(name, AreaAdminObra, StringComparison.OrdinalIgnoreCase)))
                {
                    areasConPersonalDeObra ??= await ctx.Worker
                        .Where(x => x.AreaScopeId != null
                                    && (x.ObraOficinaStaffId == ObraOficinaStaffIds.Obra
                                        || x.ObraOficinaStaffId == ObraOficinaStaffIds.Staff))
                        .Select(x => x.AreaScopeId!.Value)
                        .Distinct()
                        .ToListAsync();

                    foreach (var id in areasConPersonalDeObra)
                        if (parentById.ContainsKey(id)) visible.Add(id);
                }
            }

            return new SalidaVisibility(false, visible, esCategoriaTesorero);
        }

        /// <summary>Cadena (self, padre, abuelo, …, raíz) caminando hacia arriba. Corta ciclos.</summary>
        private static List<int> AncestorsChain(int startScopeId, IDictionary<int, int?> parentById)
        {
            var chain = new List<int>();
            var seen = new HashSet<int>();
            int? curr = startScopeId;
            while (curr.HasValue && seen.Add(curr.Value))
            {
                chain.Add(curr.Value);
                parentById.TryGetValue(curr.Value, out var parent);
                curr = parent;
            }
            return chain;
        }

        /// <summary>Agrega recursivamente todos los descendientes de un nodo al conjunto.</summary>
        private static void AddDescendants(int scopeId, IDictionary<int, List<int>> childrenByParent, HashSet<int> set)
        {
            if (!childrenByParent.TryGetValue(scopeId, out var children)) return;
            foreach (var child in children)
            {
                if (set.Add(child))
                    AddDescendants(child, childrenByParent, set);
            }
        }
    }
}

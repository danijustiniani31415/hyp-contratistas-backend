using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Shared.Helpers
{
    /// <summary>
    /// Resuelve el subárbol (el nodo elegido + todos sus descendientes) del árbol
    /// <c>area_scope</c> para filtrar trabajadores por área/gerencia. El filtro de "Área" en
    /// las pantallas de Habilitación/EMOs/Programaciones/Bandeja/SCTR-VidaLey elige un nodo de
    /// ese árbol (p.ej. "Ventas", que hoy cuelga de "Gerencia de Administración"), y debe
    /// incluir a todo trabajador de ese nodo o de cualquier hijo suyo — no solo coincidencia
    /// exacta de area_scope_id.
    /// </summary>
    public static class AreaScopeTree
    {
        public static async Task<HashSet<int>> ResolveDescendantsAsync(this AppDbContext ctx, int areaScopeId)
        {
            var nodos = await ctx.AreaScope.AsNoTracking()
                .Where(s => s.State)
                .Select(s => new { s.AreaScopeId, s.AreaScopeParentId })
                .ToListAsync();

            return DescendantsIncludingSelf(nodos.Select(n => (n.AreaScopeId, n.AreaScopeParentId)), areaScopeId);
        }

        public static HashSet<int> DescendantsIncludingSelf(IEnumerable<(int Id, int? ParentId)> nodos, int rootId)
        {
            // Los nodos raíz (Gerencias) tienen ParentId null — Dictionary no acepta null como
            // key aunque TKey sea int?, así que se excluyen antes de agrupar (nunca se los busca
            // de todas formas: nadie hace TryGetValue(null) acá).
            var hijos = nodos
                .Where(n => n.ParentId.HasValue)
                .GroupBy(n => n.ParentId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(n => n.Id).ToList());

            var result = new HashSet<int> { rootId };
            var queue = new Queue<int>();
            queue.Enqueue(rootId);

            while (queue.Count > 0)
            {
                var actual = queue.Dequeue();
                if (!hijos.TryGetValue(actual, out var kids)) continue;
                foreach (var k in kids)
                    if (result.Add(k)) queue.Enqueue(k);
            }

            return result;
        }
    }
}

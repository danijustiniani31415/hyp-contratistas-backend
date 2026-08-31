using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Services.AreaScope.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Shared.Services.AreaScope.Services
{
    /// <inheritdoc cref="IAreaScopeLegacyResolver"/>
    public class AreaScopeLegacyResolver : IAreaScopeLegacyResolver
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public AreaScopeLegacyResolver(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        /// <summary>Un nodo del árbol, con lo mínimo para subir por él.</summary>
        private sealed record Nodo(int AreaScopeId, int? ParentId, string Nombre);

        public async Task<AreaLegacyDatos?> ResolveAsync(int? areaScopeId)
        {
            if (areaScopeId is null or <= 0) return null;
            var todos = await ResolveTodosAsync();
            return todos.TryGetValue(areaScopeId.Value, out var datos) ? datos : null;
        }

        public async Task<Dictionary<int, AreaLegacyDatos>> ResolveTodosAsync()
        {
            using var ctx = _factory.CreateDbContext();

            // Ambas tablas son chicas (decenas de filas): se traen completas y se cruza en memoria,
            // que es más barato que una consulta recursiva por nodo.
            var nodos = await (
                from s in ctx.AreaScope.AsNoTracking()
                join ai in ctx.AreaItem.AsNoTracking() on s.AreaItemId equals ai.AreaItemId
                where s.State && ai.State
                select new Nodo(s.AreaScopeId, s.AreaScopeParentId, ai.AreaItemName)
            ).ToListAsync();

            var catSubareas = await ctx.CatSubarea.AsNoTracking()
                .Where(x => x.Activo)
                .Select(x => new { x.Subarea, x.Area, x.Jefatura })
                .ToListAsync();

            var nodoPorId = nodos.ToDictionary(n => n.AreaScopeId);

            // subárea normalizada -> fila del catálogo. Si el catálogo trae la misma subárea en dos
            // áreas distintas gana la primera: el área queda implícita en la subárea (mismo criterio
            // que el mapa curado de AreaScopeMatcher).
            var catPorSubarea = new Dictionary<string, AreaLegacyDatos>();
            foreach (var c in catSubareas)
            {
                var key = AreaScopeMatcher.Normalize(c.Subarea);
                if (key.Length > 0 && !catPorSubarea.ContainsKey(key))
                    catPorSubarea[key] = new AreaLegacyDatos(c.Area, c.Subarea, c.Jefatura);
            }

            var resultado = new Dictionary<int, AreaLegacyDatos>();
            foreach (var nodo in nodos)
            {
                var datos = ResolverSubiendo(nodo, nodoPorId, catPorSubarea);
                if (datos != null) resultado[nodo.AreaScopeId] = datos;
            }
            return resultado;
        }

        /// <summary>
        /// Sube desde <paramref name="desde"/> hasta la raíz y devuelve la equivalencia del primer
        /// nodo que tenga una: primero por el mapa curado (por id) y si no por el nombre del nodo
        /// contra el catálogo. Devuelve null si ningún ancestro tiene equivalente.
        /// </summary>
        private static AreaLegacyDatos? ResolverSubiendo(
            Nodo desde,
            IReadOnlyDictionary<int, Nodo> nodoPorId,
            IReadOnlyDictionary<string, AreaLegacyDatos> catPorSubarea)
        {
            var visitados = new HashSet<int>();
            Nodo? actual = desde;

            while (actual != null && visitados.Add(actual.AreaScopeId))
            {
                // 1) mapa curado: id del nodo -> subárea normalizada.
                if (AreaScopeMatcher.ScopeToSubarea.TryGetValue(actual.AreaScopeId, out var keyCurada)
                    && catPorSubarea.TryGetValue(keyCurada, out var porMapa))
                    return porMapa;

                // 2) nombre del nodo contra el catálogo (cubre los nodos agregados por UI).
                var keyNombre = AreaScopeMatcher.Normalize(actual.Nombre);
                if (keyNombre.Length > 0 && catPorSubarea.TryGetValue(keyNombre, out var porNombre))
                    return porNombre;

                actual = actual.ParentId != null && nodoPorId.TryGetValue(actual.ParentId.Value, out var padre)
                    ? padre
                    : null;
            }

            return null;
        }
    }
}

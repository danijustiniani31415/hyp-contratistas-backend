using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Services
{
    /// <inheritdoc cref="IAprobacionScopeResolver"/>
    ///
    /// <remarks>
    /// Reglas, en este orden:
    ///   1. <b>Gerente General</b> (<see cref="CategoriaIds.GerenteGeneral"/>) → ve todo. Gana sobre
    ///      cualquier otra: si además figurara como gerente de un área, sigue viendo todo.
    ///   2. <b>Gerente</b> (<see cref="CategoriaIds.Gerente"/>) → su <c>area_scope</c> y todo el
    ///      subárbol que cuelga de él. Es el mismo criterio con el que
    ///      <c>ReclutamientoRepository.GetGerenteDeArea</c> lo elige como destinatario del correo,
    ///      pero al revés: allá se sube desde el solicitante hasta encontrarlo, acá se baja desde él.
    ///      Por eso alcanza a las solicitudes de las áreas que cuelgan de su gerencia.
    ///   3. <b>GTH</b> → cualquier ficha cuyo <c>area_scope_id</c> sea el nodo de Gestión del
    ///      Talento Humano. Ve todo, como el GG, porque los reemplazos que decide son de toda la
    ///      empresa. Es el único nivel que no mira la categoría: acá no se aprueba como jefatura
    ///      sino como el área dueña del proceso, así que sirve cualquiera de sus integrantes.
    ///   4. Cualquier otro caso → nada. Entra a la pantalla porque su rol se lo permite, pero no hay
    ///      solicitudes bajo su alcance.
    ///
    /// El orden importa cuando alguien cae en dos reglas — un gerente que además esté registrado en
    /// el área de GTH: gana la jefatura, que es el nivel con más alcance de los dos.
    ///
    /// La categoría se compara por id y las áreas por árbol, nunca por nombre: renombrar una
    /// categoría desde Configuración no puede apagar esta regla en silencio.
    /// </remarks>
    public class AprobacionScopeResolver : IAprobacionScopeResolver
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public AprobacionScopeResolver(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<AprobacionScope> ResolveAsync(int? userId)
        {
            if (!userId.HasValue) return AprobacionScope.Ninguno();

            using var ctx = _factory.CreateDbContext();

            // Fichas del usuario. Una persona puede tener más de una (reingreso), así que se toman
            // todas las vigentes y se suman sus alcances. Se exige ACTIVO por el mismo motivo que
            // GetGerenteDeArea: quien recibe el correo para decidir y quien puede decidir tienen
            // que ser la misma persona, y una ficha cesada ya no recibe nada.
            var fichas = await (
                from w in ctx.Worker.AsNoTracking()
                join p in ctx.Person.AsNoTracking() on w.PersonId equals p.PersonId
                where p.UserId == userId.Value && w.WorkersEstadoId == WorkersEstadoIds.Activo
                // La categoría sale del puesto: workers ya no la guarda.
                select new
                {
                    CategoriaId = w.PuestoCatalogo != null ? w.PuestoCatalogo.CategoriaId : (int?)null,
                    w.AreaScopeId
                }
            ).ToListAsync();

            if (fichas.Count == 0) return AprobacionScope.Ninguno();

            // 1) Gerente General: ve todo, no hace falta ni cargar el árbol.
            if (fichas.Any(f => f.CategoriaId == CategoriaIds.GerenteGeneral))
                return new AprobacionScope(AprobacionNivel.GerenteGeneral, true, new HashSet<int>(), null);

            // 2) Gerente de área: su nodo + descendientes.
            var nodosGerente = fichas
                .Where(f => f.CategoriaId == CategoriaIds.Gerente && f.AreaScopeId.HasValue)
                .Select(f => f.AreaScopeId!.Value)
                .Distinct()
                .ToList();

            // 3) GTH: se pregunta después de la jefatura porque un gerente registrado en el área de
            //    GTH tiene que seguir entrando como gerente, que alcanza más. Ve todo (los
            //    reemplazos que decide son de toda la empresa), así que no necesita el árbol.
            if (nodosGerente.Count == 0)
            {
                return fichas.Any(f => f.AreaScopeId == AreaScopeIds.GestionDelTalentoHumano)
                    ? new AprobacionScope(AprobacionNivel.Gth, true, new HashSet<int>(), null)
                    : AprobacionScope.Ninguno();
            }

            // El árbol es una tabla chica: se arma en memoria, igual que en SalidaVisibilityResolver
            // y en GetGerenteDeArea.
            var nodos = await (
                from s in ctx.AreaScope.AsNoTracking()
                join ai in ctx.AreaItem.AsNoTracking() on s.AreaItemId equals ai.AreaItemId
                where s.State
                select new { s.AreaScopeId, s.AreaScopeParentId, ai.AreaItemName }
            ).ToListAsync();

            var hijosPorPadre = nodos
                .Where(n => n.AreaScopeParentId.HasValue)
                .GroupBy(n => n.AreaScopeParentId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(x => x.AreaScopeId).ToList());

            var visibles = new HashSet<int>();
            foreach (var nodo in nodosGerente)
            {
                // Solo nodos vivos: un área dada de baja no aporta alcance.
                if (!nodos.Any(n => n.AreaScopeId == nodo)) continue;
                visibles.Add(nodo);
                AgregarDescendientes(nodo, hijosPorPadre, visibles);
            }

            if (visibles.Count == 0) return AprobacionScope.Ninguno();

            // Etiqueta del alcance para la pantalla. Con más de un área se nombra la primera y se
            // dice cuántas más hay; no vale la pena listarlas todas en un subtítulo.
            var nombres = nodosGerente
                .Select(id => nodos.FirstOrDefault(n => n.AreaScopeId == id)?.AreaItemName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToList();
            var areaNombre = nombres.Count switch
            {
                0 => null,
                1 => nombres[0],
                _ => $"{nombres[0]} y {nombres.Count - 1} área(s) más",
            };

            return new AprobacionScope(AprobacionNivel.GerenteArea, false, visibles, areaNombre);
        }

        /// <summary>Agrega recursivamente todos los descendientes de un nodo al conjunto.</summary>
        private static void AgregarDescendientes(
            int scopeId, IDictionary<int, List<int>> hijosPorPadre, HashSet<int> set)
        {
            if (!hijosPorPadre.TryGetValue(scopeId, out var hijos)) return;
            foreach (var hijo in hijos)
            {
                if (set.Add(hijo))
                    AgregarDescendientes(hijo, hijosPorPadre, set);
            }
        }
    }
}

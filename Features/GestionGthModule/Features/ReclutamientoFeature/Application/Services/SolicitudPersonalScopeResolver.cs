using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Services
{
    /// <inheritdoc cref="ISolicitudPersonalScopeResolver"/>
    ///
    /// <remarks>
    /// Reglas, en este orden:
    ///   1. <b>Sin ficha vigente</b> → solo lo que él mismo registró, sin poder moverlo.
    ///   2. <b>Gerente General</b> (<see cref="CategoriaIds.GerenteGeneral"/>) o <b>GTH</b>
    ///      (ficha en <see cref="AreaScopeIds.GestionDelTalentoHumano"/>) → ven todo. El GG porque
    ///      autoriza las vacantes de toda la empresa (y su ficha bien puede no tener área con la
    ///      cual filtrar); GTH porque es el área dueña del proceso.
    ///   3. <b>Cualquier otro</b> → el <c>area_scope</c> de su ficha y todo el subárbol que cuelga
    ///      de él. Es lo que hace que la gerencia alcance lo que pidieron sus áreas hijas: quien
    ///      está arriba ve hacia abajo, nunca al revés.
    ///
    /// La visibilidad NO mira la categoría a propósito: el requerimiento es del área, así que
    /// cualquiera de ella tiene que poder seguirlo aunque quien lo registró ya no esté en la
    /// empresa. Lo que sí mira la categoría es <c>PuedeGestionar</c> — registrar y avanzar el
    /// proceso son de la jefatura.
    ///
    /// Una persona puede tener más de una ficha (reingreso): se suman los alcances de todas las
    /// vigentes, igual que en <see cref="AprobacionScopeResolver"/>. Y como allá, la categoría se
    /// compara por id y las áreas por árbol, nunca por nombre: renombrar una categoría desde
    /// Configuración no puede apagar esta regla en silencio.
    /// </remarks>
    public class SolicitudPersonalScopeResolver : ISolicitudPersonalScopeResolver
    {
        /// <summary>
        /// Categorías que pueden registrar una solicitud y avanzar sus requerimientos. Es la
        /// jefatura del área: quien pide personal y quien decide a quién se contrata.
        /// </summary>
        private static readonly int[] CategoriasQueGestionan =
            { CategoriaIds.Jefe, CategoriaIds.Gerente, CategoriaIds.GerenteGeneral };

        private readonly IDbContextFactory<AppDbContext> _factory;

        public SolicitudPersonalScopeResolver(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<SolicitudPersonalScope> ResolveAsync(int userId)
        {
            using var ctx = _factory.CreateDbContext();

            // Fichas ACTIVAS del usuario. Se exige el estado por lo mismo que en
            // AprobacionScopeResolver: una ficha cesada no arrastra el alcance de su área.
            var fichas = await (
                from w in ctx.Worker.AsNoTracking()
                join p in ctx.Person.AsNoTracking() on w.PersonId equals p.PersonId
                where p.UserId == userId && w.WorkersEstadoId == WorkersEstadoIds.Activo
                // La categoría sale del puesto: workers ya no la guarda.
                select new
                {
                    CategoriaId = w.PuestoCatalogo != null ? w.PuestoCatalogo.CategoriaId : (int?)null,
                    w.AreaScopeId
                }
            ).ToListAsync();

            if (fichas.Count == 0) return SolicitudPersonalScope.SoloLoSuyo(userId);

            var puedeGestionar = fichas.Any(f => f.CategoriaId.HasValue
                                                 && CategoriasQueGestionan.Contains(f.CategoriaId.Value));

            // Los que ven todo: no hace falta ni cargar el árbol.
            if (fichas.Any(f => f.CategoriaId == CategoriaIds.GerenteGeneral
                                || f.AreaScopeId == AreaScopeIds.GestionDelTalentoHumano))
                return new SolicitudPersonalScope(userId, true, new HashSet<int>(), puedeGestionar);

            var nodosPropios = fichas
                .Where(f => f.AreaScopeId.HasValue)
                .Select(f => f.AreaScopeId!.Value)
                .Distinct()
                .ToList();

            // Sin área asignada no se hereda el alcance de nadie: ve solo lo que registró él.
            if (nodosPropios.Count == 0)
                return new SolicitudPersonalScope(userId, false, new HashSet<int>(), puedeGestionar);

            // El árbol es una tabla chica: se arma en memoria, igual que en AprobacionScopeResolver
            // y en SalidaVisibilityResolver.
            var nodos = await ctx.AreaScope.AsNoTracking()
                .Where(s => s.State)
                .Select(s => new { s.AreaScopeId, s.AreaScopeParentId })
                .ToListAsync();

            var hijosPorPadre = nodos
                .Where(n => n.AreaScopeParentId.HasValue)
                .GroupBy(n => n.AreaScopeParentId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(x => x.AreaScopeId).ToList());

            var visibles = new HashSet<int>();
            foreach (var nodo in nodosPropios)
            {
                // Solo nodos vivos: un área dada de baja no aporta alcance.
                if (!nodos.Any(n => n.AreaScopeId == nodo)) continue;
                visibles.Add(nodo);
                AgregarDescendientes(nodo, hijosPorPadre, visibles);
            }

            return new SolicitudPersonalScope(userId, false, visibles, puedeGestionar);
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

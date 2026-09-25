using System.Security.Claims;
using System.Text.Json;
using Abril_Backend.Features.PersonasModule.Application.Dtos;

namespace Abril_Backend.Features.PersonasModule
{
    /// <summary>
    /// A qué proyectos aplica un permiso para el usuario actual. EsGlobal=true = sin restricción
    /// (al menos una asignación de ese permiso tiene ProyectoId null); si no, ProyectoIds trae los
    /// proyectos concretos habilitados (puede quedar vacío si el usuario tiene el permiso en
    /// lb_permisos pero por algún motivo ninguna asignación lo sustenta — no debería pasar, pero
    /// en ese caso no se le muestra nada en vez de reventar).
    /// </summary>
    public class LbScopeProyectos
    {
        public bool EsGlobal { get; init; }
        public HashSet<int> ProyectoIds { get; init; } = new();

        public bool Permite(int proyectoId) => EsGlobal || ProyectoIds.Contains(proyectoId);
    }

    /// <summary>
    /// Lee los claims "lb_permisos"/"lb_asignaciones" que emite LbJwtService.
    /// </summary>
    public static class LbClaimsExtensions
    {
        public static bool HasLbPermiso(this ClaimsPrincipal user, string codigo)
        {
            var claim = user.FindFirst("lb_permisos")?.Value;
            if (string.IsNullOrEmpty(claim)) return false;
            try
            {
                var permisos = JsonSerializer.Deserialize<List<string>>(claim) ?? new List<string>();
                return permisos.Contains(codigo);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// A qué proyectos aplica ESTE permiso puntual para el usuario — no todo lo que tenga en
        /// lb_permisos (esa lista está aplanada entre todas sus asignaciones, sin scope). Ej.:
        /// alguien con "Logística" global en Lima Y "Logística" acotada a Las Bravas tiene
        /// PEDIDO_APROBAR en ambas filas, pero acá debe resultar EsGlobal=true (la fila de Lima ya
        /// no tiene restricción) — no una lista con un solo proyecto.
        /// Usar en cada endpoint de listado que filtre por proyecto (Pedidos, Compras,
        /// Herramientas, EPP, Guías de Remisión), en vez de HasLbPermiso a secas.
        /// </summary>
        public static LbScopeProyectos GetProyectosPermitidos(this ClaimsPrincipal user, string codigoPermiso)
        {
            var claim = user.FindFirst("lb_asignaciones")?.Value;
            if (string.IsNullOrEmpty(claim)) return new LbScopeProyectos();

            List<LbAsignacionDto> asignaciones;
            try
            {
                asignaciones = JsonSerializer.Deserialize<List<LbAsignacionDto>>(claim) ?? new List<LbAsignacionDto>();
            }
            catch (JsonException)
            {
                return new LbScopeProyectos();
            }

            var relevantes = asignaciones.Where(a => a.Permisos.Contains(codigoPermiso)).ToList();
            if (relevantes.Any(a => a.ProyectoId is null))
                return new LbScopeProyectos { EsGlobal = true };

            return new LbScopeProyectos
            {
                EsGlobal = false,
                ProyectoIds = relevantes.Where(a => a.ProyectoId.HasValue).Select(a => a.ProyectoId!.Value).ToHashSet(),
            };
        }

        /// <summary>
        /// Unión de <see cref="GetProyectosPermitidos"/> sobre varios permisos que comparten el
        /// mismo recurso (ver, crear, aprobar, entregar/confirmar...). Un endpoint de listado no
        /// debe filtrar el scope por un solo permiso "principal" (ej. VER_TODOS) — quien solo
        /// tiene el permiso de aprobar/entregar/confirmar igual necesita ver lo que le toca
        /// procesar, no solo lo que ese permiso puntual cubre. Bug real 2026-09-25: un Residente
        /// con PEDIDO_VISAR pero no PEDIDO_VER_TODOS no veía ningún pedido en la lista, y otro con
        /// GUIA_REMISION_CONFIRMAR pero no GUIA_REMISION_VER no podía ni abrir la guía a confirmar.
        /// Si el usuario no tiene ninguno de los códigos, devuelve scope vacío (no global, sin
        /// proyectos) — el llamador decide si eso implica 403 o una lista vacía.
        /// </summary>
        public static LbScopeProyectos GetProyectosPermitidosUnion(this ClaimsPrincipal user, params string[] codigos)
        {
            var esGlobal = false;
            var union = new HashSet<int>();
            foreach (var codigo in codigos)
            {
                if (!user.HasLbPermiso(codigo)) continue;
                var scope = user.GetProyectosPermitidos(codigo);
                if (scope.EsGlobal) { esGlobal = true; continue; }
                union.UnionWith(scope.ProyectoIds);
            }
            return esGlobal ? new LbScopeProyectos { EsGlobal = true } : new LbScopeProyectos { ProyectoIds = union };
        }
    }
}

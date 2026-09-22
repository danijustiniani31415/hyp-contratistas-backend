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
    }
}

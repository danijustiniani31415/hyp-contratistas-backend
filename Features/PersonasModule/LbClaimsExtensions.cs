using System.Security.Claims;
using System.Text.Json;

namespace Abril_Backend.Features.PersonasModule
{
    /// <summary>
    /// Lee los claims "lb_permisos" que ya emite LbJwtService — hasta Pedidos, ningún controller
    /// de Las Bravas distinguía por rol (Fase 1 eran pantallas de administración, cualquier
    /// usuario autenticado podía todo). Pedidos es el primer flujo multi-rol real
    /// (RESIDENTE crea, GERENTE_GENERAL aprueba, ALMACENERO entrega) — sin esto, un RESIDENTE
    /// podría aprobar su propio pedido.
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
    }
}

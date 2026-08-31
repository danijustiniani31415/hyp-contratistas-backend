using System.Security.Claims;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Shared
{
    public static class ClinicaClaimsHelper
    {
        public static int? GetClinicaId(ClaimsPrincipal user)
        {
            var val = user.FindFirst("clinicaId")?.Value;
            return int.TryParse(val, out var id) ? id : null;
        }

        public static int? GetClinicaUsuarioId(ClaimsPrincipal user)
        {
            var val = user.FindFirst("clinicaUsuarioId")?.Value
                ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out var id) ? id : null;
        }

        public static bool ValidarAcceso(ClaimsPrincipal user, int clinicaId) =>
            GetClinicaId(user) == clinicaId;
    }
}

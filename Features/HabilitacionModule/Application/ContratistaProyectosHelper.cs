using System.Security.Claims;

namespace Abril_Backend.Features.Habilitacion.Application
{
    public static class ContratistaProyectosHelper
    {
        /// <summary>
        /// Si el usuario es CONTRATISTA con scope POR_PROYECTO,
        /// retorna sus proyectoIds. En cualquier otro caso retorna null (sin filtro).
        /// </summary>
        public static List<int>? GetProyectosFiltro(ClaimsPrincipal user)
        {
            var tipo = user.FindFirst("tipo")?.Value;
            if (tipo != "CONTRATISTA") return null;

            var scope = user.FindFirst("scope")?.Value;
            if (scope != "POR_PROYECTO") return null;

            var proyectosRaw = user.FindFirst("proyectoIds")?.Value;
            if (string.IsNullOrEmpty(proyectosRaw)) return new List<int>();

            return proyectosRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => int.TryParse(p, out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList();
        }
    }
}

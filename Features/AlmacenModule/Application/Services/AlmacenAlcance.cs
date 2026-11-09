using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.PersonasModule;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.AlmacenModule.Application.Services
{
    /// <summary>
    /// A qué almacenes puede entrar el usuario: quien tiene el permiso en forma global ve todos; quien lo tiene solo en ciertos
    /// proyectos ve únicamente los almacenes de esos proyectos (el almacén central, sin proyecto, queda fuera de su alcance).
    /// </summary>
    public static class AlmacenAlcance
    {
        /// <summary>null = sin restricción. Si el usuario no tiene ninguno de los permisos, lanza 403.</summary>
        public static async Task<HashSet<int>?> Resolver(IDbContextFactory<AppDbContext> factory, ClaimsPrincipal user, params string[] permisos)
        {
            if (!permisos.Any(user.HasLbPermiso)) throw new AbrilException("No tienes permiso para ver el almacén.", 403);
            var scope = user.GetProyectosPermitidosUnion(permisos);
            if (scope.EsGlobal) return null;
            using var ctx = factory.CreateDbContext();
            return (await ctx.Almacen.AsNoTracking()
                .Where(a => a.ProyectoId != null && scope.ProyectoIds.Contains(a.ProyectoId.Value))
                .Select(a => a.Id).ToListAsync()).ToHashSet();
        }

        public static void Exigir(HashSet<int>? permitidos, int almacenId)
        {
            if (permitidos != null && !permitidos.Contains(almacenId))
                throw new AbrilException("Este almacén no pertenece a tus proyectos.", 403);
        }
    }
}

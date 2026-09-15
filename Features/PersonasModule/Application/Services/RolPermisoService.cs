using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.PersonasModule.Application.Dtos;
using Abril_Backend.Features.PersonasModule.Application.Interfaces;
using Abril_Backend.Features.PersonasModule.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.PersonasModule.Application.Services
{
    /// <summary>
    /// Administración de qué permisos trae cada rol (lb_rol_permiso). Ver CONTEXT_LOGISTICA.md
    /// sección 4: agregar un módulo nuevo NO significa tocar cada persona — se declaran sus
    /// permisos en lb_permiso (parte del propio desarrollo del módulo) y desde acá el
    /// administrador decide qué rol los recibe. Todo usuario_sistema con ese rol hereda el
    /// acceso automáticamente vía la resolución de la sección 4.4, sin tocar personas.
    /// </summary>
    public class RolPermisoService : IRolPermisoService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public RolPermisoService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<RolListItemDto>> ListRoles()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Rol
                .Where(r => r.Activo)
                .OrderBy(r => r.Nombre)
                .Select(r => new RolListItemDto
                {
                    Id = r.Id,
                    Codigo = r.Codigo,
                    Nombre = r.Nombre,
                    Descripcion = r.Descripcion,
                    EsGlobal = r.EsGlobal,
                    CantidadPermisos = r.RolPermisos.Count,
                })
                .ToListAsync();
        }

        public async Task<List<PermisoDto>> ListPermisos()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Permiso
                .OrderBy(p => p.Codigo)
                .Select(p => new PermisoDto { Id = p.Id, Codigo = p.Codigo, Descripcion = p.Descripcion })
                .ToListAsync();
        }

        public async Task<RolDetalleDto> GetRolDetalle(short rolId)
        {
            using var ctx = _factory.CreateDbContext();
            var rol = await ctx.Rol.FindAsync(rolId)
                ?? throw new AbrilException("Rol no encontrado.", 404);

            var permisoIds = await ctx.RolPermiso
                .Where(rp => rp.RolId == rolId)
                .Select(rp => rp.PermisoId)
                .ToListAsync();

            return new RolDetalleDto
            {
                Id = rol.Id,
                Codigo = rol.Codigo,
                Nombre = rol.Nombre,
                Descripcion = rol.Descripcion,
                EsGlobal = rol.EsGlobal,
                PermisoIds = permisoIds,
            };
        }

        public async Task<RolDetalleDto> ActualizarPermisos(short rolId, ActualizarPermisosRolDto dto)
        {
            using var ctx = _factory.CreateDbContext();
            var rol = await ctx.Rol.FindAsync(rolId)
                ?? throw new AbrilException("Rol no encontrado.", 404);

            var idsValidos = await ctx.Permiso
                .Where(p => dto.PermisoIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync();

            var actuales = await ctx.RolPermiso.Where(rp => rp.RolId == rolId).ToListAsync();
            ctx.RolPermiso.RemoveRange(actuales);
            ctx.RolPermiso.AddRange(idsValidos.Select(pid => new RolPermiso { RolId = rolId, PermisoId = pid }));
            await ctx.SaveChangesAsync();

            return await GetRolDetalle(rolId);
        }
    }
}

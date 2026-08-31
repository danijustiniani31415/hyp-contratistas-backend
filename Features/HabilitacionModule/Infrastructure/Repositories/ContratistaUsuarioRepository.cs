using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Dtos.ContratistaUsuarios;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Repositories
{
    public class ContratistaUsuarioRepository : IContratistaUsuarioRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public ContratistaUsuarioRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<ContratistaUsuarioListDto>> GetUsuariosAsync(int contractorId)
        {
            using var ctx = _factory.CreateDbContext();

            var usuarios = await ctx.SsContratistaUsuarios
                .Include(u => u.Rol)
                .Include(u => u.Proyectos)
                .Where(u => u.ContractorId == contractorId)
                .OrderBy(u => u.RolId)
                .ToListAsync();

            var userIds = usuarios.Select(u => u.UserId).Distinct().ToList();

            var userInfo = await ctx.User
                .Where(u => userIds.Contains(u.UserId))
                .Select(u => new
                {
                    u.UserId,
                    u.Email,
                    NombreCompleto = u.Person != null ? u.Person.FullName : null
                })
                .ToDictionaryAsync(u => u.UserId);

            return usuarios.Select(u =>
            {
                userInfo.TryGetValue(u.UserId, out var info);
                return new ContratistaUsuarioListDto
                {
                    Id = u.Id,
                    UserId = u.UserId,
                    NombreCompleto = info?.NombreCompleto ?? info?.Email,
                    Email = info?.Email,
                    RolNombre = u.Rol?.Nombre,
                    Scope = u.Scope,
                    Activo = u.Activo,
                    ProyectoIds = u.Proyectos.Select(p => p.ProyectoId).ToList(),
                    Modulos = u.Modulos,
                    WorkerId = u.WorkerId
                };
            }).ToList();
        }

        public async Task<SsContratistaUsuario?> GetByIdAsync(int id, int contractorId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsContratistaUsuarios
                .Include(u => u.Rol)
                .Include(u => u.Proyectos)
                .FirstOrDefaultAsync(u => u.Id == id && u.ContractorId == contractorId);
        }

        public async Task InvitarUsuarioAsync(SsContratistaUsuario entity, List<int>? proyectoIds)
        {
            using var ctx = _factory.CreateDbContext();

            ctx.SsContratistaUsuarios.Add(entity);
            await ctx.SaveChangesAsync();

            if (proyectoIds is { Count: > 0 })
            {
                var proyectos = proyectoIds.Select(pid => new SsContratistaUsuarioProyecto
                {
                    ContratistaUsuarioId = entity.Id,
                    ProyectoId = pid
                });
                ctx.SsContratistaUsuarioProyectos.AddRange(proyectos);
                await ctx.SaveChangesAsync();
            }
        }

        public async Task ActualizarUsuarioAsync(int id, SsContratistaUsuario entity, List<int>? proyectoIds)
        {
            using var ctx = _factory.CreateDbContext();

            var existing = await ctx.SsContratistaUsuarios
                .Include(u => u.Proyectos)
                .FirstOrDefaultAsync(u => u.Id == id && u.ContractorId == entity.ContractorId)
                ?? throw new AbrilException("Usuario no encontrado.", 404);

            existing.RolId = entity.RolId;
            existing.Scope = entity.Scope;
            existing.Activo = entity.Activo;
            existing.Modulos = entity.Modulos;

            if (proyectoIds is not null)
            {
                var actuales = existing.Proyectos.ToList();
                var nuevos = proyectoIds;

                var aEliminar = actuales.Where(p => !nuevos.Contains(p.ProyectoId)).ToList();
                var aAgregar = nuevos.Where(pid => !actuales.Any(p => p.ProyectoId == pid)).ToList();

                ctx.SsContratistaUsuarioProyectos.RemoveRange(aEliminar);
                ctx.SsContratistaUsuarioProyectos.AddRange(aAgregar.Select(pid => new SsContratistaUsuarioProyecto
                {
                    ContratistaUsuarioId = id,
                    ProyectoId = pid
                }));
            }

            await ctx.SaveChangesAsync();
        }

        public async Task DesactivarUsuarioAsync(int id, int contractorId)
        {
            using var ctx = _factory.CreateDbContext();

            var usuario = await ctx.SsContratistaUsuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == id && u.ContractorId == contractorId)
                ?? throw new AbrilException("Usuario no encontrado.", 404);

            if (usuario.Rol?.Nombre == "OWNER")
                throw new AbrilException("No se puede desactivar al usuario propietario de la empresa.", 400);

            usuario.Activo = false;
            await ctx.SaveChangesAsync();
        }
    }
}

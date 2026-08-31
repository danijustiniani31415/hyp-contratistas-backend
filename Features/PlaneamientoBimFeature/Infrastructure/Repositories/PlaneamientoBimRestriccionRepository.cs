using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.PlaneamientoBimFeature.Application.Dtos;
using Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Repositories
{
    public class PlaneamientoBimRestriccionRepository : IPlaneamientoBimRestriccionRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public PlaneamientoBimRestriccionRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<RestriccionDto>> GetPaged(int projectId, bool? soloActivos)
        {
            using var ctx = _factory.CreateDbContext();

            var query = ctx.BimRestriccion.Where(b => b.ProjectId == projectId);
            if (soloActivos == true)
                query = query.Where(b => b.FechaCierre == null);

            return await query
                .OrderByDescending(b => b.FechaCreacion)
                .Select(b => new RestriccionDto
                {
                    Id = b.Id,
                    ProjectId = b.ProjectId,
                    Descripcion = b.Descripcion,
                    Estado = b.Estado,
                    FechaCreacion = b.FechaCreacion,
                    FechaActualizacion = b.FechaActualizacion,
                    FechaCierre = b.FechaCierre,
                    FechaLevantamientoPrevista = b.FechaLevantamientoPrevista,
                    ZonaId = b.ZonaId,
                    ZonaNombre = b.Zona != null ? b.Zona.Nombre : null,
                    ZonaNivelId = b.ZonaNivelId,
                    ZonaNivelNombre = b.ZonaNivel != null ? b.ZonaNivel.Nombre : null,
                    ZonaSectorId = b.ZonaSectorId,
                    ZonaSectorNombre = b.ZonaSector != null ? b.ZonaSector.Nombre : null,
                    ActividadId = b.ActividadId,
                    ActividadNombre = b.Actividad != null ? b.Actividad.Nombre : null,
                })
                .ToListAsync();
        }

        public async Task<RestriccionDto> Create(int projectId, RestriccionCreateDto dto, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var existeProyecto = await ctx.Project.AnyAsync(p => p.ProjectId == projectId);
            if (!existeProyecto)
                throw new AbrilException("El proyecto no existe.", 404);

            var restriccion = new BimRestriccion
            {
                ProjectId = projectId,
                Descripcion = dto.Descripcion.Trim(),
                Estado = dto.Estado,
                FechaCreacion = DateTimeOffset.UtcNow,
                CreatedUserId = userId,
                FechaLevantamientoPrevista = dto.FechaLevantamientoPrevista,
                ZonaId = dto.ZonaId,
                ZonaNivelId = dto.ZonaNivelId,
                ZonaSectorId = dto.ZonaSectorId,
                ActividadId = dto.ActividadId,
            };

            ctx.BimRestriccion.Add(restriccion);
            await ctx.SaveChangesAsync();

            return await GetById(ctx, restriccion.Id);
        }

        public async Task<RestriccionDto> Update(int restriccionId, RestriccionUpdateDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var restriccion = await ctx.BimRestriccion.FirstOrDefaultAsync(b => b.Id == restriccionId);
            if (restriccion == null)
                throw new AbrilException("La restricción no existe.", 404);
            if (restriccion.FechaCierre != null)
                throw new AbrilException("No se puede editar una restricción cerrada.", 409);

            restriccion.Descripcion = dto.Descripcion.Trim();
            restriccion.Estado = dto.Estado;
            restriccion.FechaActualizacion = DateTimeOffset.UtcNow;
            restriccion.FechaLevantamientoPrevista = dto.FechaLevantamientoPrevista;
            restriccion.ZonaId = dto.ZonaId;
            restriccion.ZonaNivelId = dto.ZonaNivelId;
            restriccion.ZonaSectorId = dto.ZonaSectorId;
            restriccion.ActividadId = dto.ActividadId;

            await ctx.SaveChangesAsync();

            return await GetById(ctx, restriccion.Id);
        }

        public async Task<RestriccionDto> Cerrar(int restriccionId)
        {
            using var ctx = _factory.CreateDbContext();

            var restriccion = await ctx.BimRestriccion.FirstOrDefaultAsync(b => b.Id == restriccionId);
            if (restriccion == null)
                throw new AbrilException("La restricción no existe.", 404);
            if (restriccion.FechaCierre != null)
                throw new AbrilException("La restricción ya está cerrada.", 409);

            var ahora = DateTimeOffset.UtcNow;
            restriccion.Estado = "CERRADO";
            restriccion.FechaCierre = ahora;
            restriccion.FechaActualizacion = ahora;

            await ctx.SaveChangesAsync();

            return await GetById(ctx, restriccion.Id);
        }

        private static async Task<RestriccionDto> GetById(AppDbContext ctx, int id)
            => await ctx.BimRestriccion
                .Where(b => b.Id == id)
                .Select(b => new RestriccionDto
                {
                    Id = b.Id,
                    ProjectId = b.ProjectId,
                    Descripcion = b.Descripcion,
                    Estado = b.Estado,
                    FechaCreacion = b.FechaCreacion,
                    FechaActualizacion = b.FechaActualizacion,
                    FechaCierre = b.FechaCierre,
                    FechaLevantamientoPrevista = b.FechaLevantamientoPrevista,
                    ZonaId = b.ZonaId,
                    ZonaNombre = b.Zona != null ? b.Zona.Nombre : null,
                    ZonaNivelId = b.ZonaNivelId,
                    ZonaNivelNombre = b.ZonaNivel != null ? b.ZonaNivel.Nombre : null,
                    ZonaSectorId = b.ZonaSectorId,
                    ZonaSectorNombre = b.ZonaSector != null ? b.ZonaSector.Nombre : null,
                    ActividadId = b.ActividadId,
                    ActividadNombre = b.Actividad != null ? b.Actividad.Nombre : null,
                })
                .FirstAsync();
    }
}

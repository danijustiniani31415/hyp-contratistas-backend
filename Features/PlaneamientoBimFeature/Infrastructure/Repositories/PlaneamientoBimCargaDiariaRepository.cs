using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.PlaneamientoBimFeature.Application.Dtos;
using Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Repositories
{
    public class PlaneamientoBimCargaDiariaRepository : IPlaneamientoBimCargaDiariaRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public PlaneamientoBimCargaDiariaRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<CargaDiariaDto?> GetCargaDiaria(int projectId, DateOnly fecha, string categoria)
        {
            using var ctx = _factory.CreateDbContext();

            var existeProyecto = await ctx.Project.AnyAsync(p => p.ProjectId == projectId);
            if (!existeProyecto)
                return null;

            // Se materializa primero (Include + ToListAsync) y el merge "propio del nivel +
            // compartido de la zona" se arma en memoria — un .Concat() de dos navegaciones
            // distintas dentro de una proyección LINQ-to-SQL no es traducible por Npgsql
            // (InvalidOperationException en runtime, ver incidente CEDRO 33 28/08/2026).
            var zonasEntidades = await ctx.BimProyectoZona
                .Where(z => z.ProjectId == projectId)
                .Include(z => z.Niveles)
                    .ThenInclude(n => n.Sectores)
                .Include(z => z.Sectores)
                .OrderBy(z => z.Orden)
                .ToListAsync();

            var zonas = zonasEntidades.Select(z => new ZonaDto
            {
                Id = z.Id,
                Nombre = z.Nombre,
                Orden = z.Orden,
                Niveles = z.Niveles
                    .OrderBy(n => n.Orden)
                    .Select(n => new NivelDto
                    {
                        Id = n.Id,
                        Nombre = n.Nombre,
                        Orden = n.Orden,
                        TipoEstructura = n.TipoEstructura,
                        Sectores = n.Sectores
                            .Concat(z.Sectores.Where(s => s.ZonaNivelId == null))
                            .OrderBy(s => s.Orden)
                            .Select(s => new SectorDto { Id = s.Id, Nombre = s.Nombre, Orden = s.Orden })
                            .ToList(),
                    })
                    .ToList(),
            }).ToList();

            var actividades = await ctx.BimActividad
                .OrderBy(a => a.MacroActividad.Orden).ThenBy(a => a.Orden)
                .Select(a => new ActividadCatalogoDto
                {
                    Id = a.Id,
                    MacroActividadId = a.MacroActividadId,
                    MacroActividadNombre = a.MacroActividad.Nombre,
                    Nombre = a.Nombre,
                    Tipo = a.Tipo,
                    Orden = a.Orden,
                })
                .ToListAsync();

            var causas = await ctx.BimCausaNoCumplimiento
                .OrderBy(c => c.Orden)
                .Select(c => new CausaCatalogoDto { Id = c.Id, Nombre = c.Nombre, Orden = c.Orden })
                .ToListAsync();

            var celdas = await ctx.BimRegistroDiario
                .Where(r => r.ProjectId == projectId && r.Fecha == fecha)
                .Select(r => new CeldaDto
                {
                    ZonaId = r.ZonaId,
                    NivelId = r.NivelId,
                    SectorId = r.SectorId,
                    ActividadId = r.ActividadId,
                    PorcentajeAvance = r.PorcentajeAvance,
                    CausaId = r.CausaId,
                    CausaNombre = r.Causa != null ? r.Causa.Nombre : null,
                    CausaDetalle = r.CausaDetalle,
                })
                .ToListAsync();

            var evidencias = await ctx.BimEvidenciaFoto
                .Where(e => e.ProjectId == projectId && e.Fecha == fecha && e.Categoria == categoria)
                .OrderBy(e => e.CreatedDateTime)
                .Select(e => new EvidenciaFotoDto { Id = e.Id, Url = e.Url, CreatedDateTime = e.CreatedDateTime })
                .ToListAsync();

            var restriccionesActivas = await ctx.BimRestriccion
                .Where(b => b.ProjectId == projectId && b.FechaCierre == null)
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

            return new CargaDiariaDto
            {
                Fecha = fecha,
                Categoria = categoria,
                Zonas = zonas,
                Actividades = actividades,
                Causas = causas,
                Celdas = celdas,
                Evidencias = evidencias,
                RestriccionesActivas = restriccionesActivas,
            };
        }

        public async Task GuardarCargaDiaria(int projectId, DateOnly fecha, CargaDiariaUpdateDto dto, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var existeProyecto = await ctx.Project.AnyAsync(p => p.ProjectId == projectId);
            if (!existeProyecto)
                throw new AbrilException("El proyecto no existe.", 404);

            var existentes = await ctx.BimRegistroDiario
                .Where(r => r.ProjectId == projectId && r.Fecha == fecha)
                .ToListAsync();

            var ahora = DateTimeOffset.UtcNow;

            foreach (var celda in dto.Celdas)
            {
                var registro = existentes.FirstOrDefault(r =>
                    r.ZonaId == celda.ZonaId && r.NivelId == celda.NivelId &&
                    r.SectorId == celda.SectorId && r.ActividadId == celda.ActividadId);

                if (registro == null)
                {
                    registro = new BimRegistroDiario
                    {
                        ProjectId = projectId,
                        ZonaId = celda.ZonaId,
                        NivelId = celda.NivelId,
                        SectorId = celda.SectorId,
                        ActividadId = celda.ActividadId,
                        Fecha = fecha,
                        CreatedUserId = userId,
                        CreatedDateTime = ahora,
                    };
                    ctx.BimRegistroDiario.Add(registro);
                }
                else
                {
                    registro.UpdatedUserId = userId;
                    registro.UpdatedDateTime = ahora;
                }

                registro.PorcentajeAvance = celda.PorcentajeAvance;
                registro.CausaId = celda.PorcentajeAvance == 100 ? null : celda.CausaId;
                registro.CausaDetalle = celda.PorcentajeAvance == 100 ? null : celda.CausaDetalle;
            }

            await ctx.SaveChangesAsync();
        }

        public async Task<List<EvidenciaFotoDto>> AgregarEvidencias(int projectId, DateOnly fecha, List<string> urls, int userId, string categoria)
        {
            using var ctx = _factory.CreateDbContext();

            var ahora = DateTimeOffset.UtcNow;
            var nuevas = urls.Select(url => new BimEvidenciaFoto
            {
                ProjectId = projectId,
                Fecha = fecha,
                Categoria = categoria,
                Url = url,
                CreatedUserId = userId,
                CreatedDateTime = ahora,
            }).ToList();

            ctx.BimEvidenciaFoto.AddRange(nuevas);
            await ctx.SaveChangesAsync();

            return nuevas.Select(e => new EvidenciaFotoDto { Id = e.Id, Url = e.Url, CreatedDateTime = e.CreatedDateTime }).ToList();
        }
    }
}

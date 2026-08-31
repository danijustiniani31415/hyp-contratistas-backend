using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.PlaneamientoBimFeature.Application.Dtos;
using Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Repositories
{
    public class PlaneamientoBimConfiguracionRepository : IPlaneamientoBimConfiguracionRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public PlaneamientoBimConfiguracionRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<ConfiguracionInicialDto?> GetConfiguracion(int projectId)
        {
            using var ctx = _factory.CreateDbContext();

            var existeProyecto = await ctx.Project.AnyAsync(p => p.ProjectId == projectId);
            if (!existeProyecto)
                return null;

            var tieneFases = await ctx.BimProyectoFase.AnyAsync(f => f.ProjectId == projectId);
            if (!tieneFases)
            {
                var fasesCatalogo = await ctx.BimFase.ToListAsync();
                ctx.BimProyectoFase.AddRange(fasesCatalogo.Select(fase => new BimProyectoFase
                {
                    ProjectId = projectId,
                    FaseId = fase.Id,
                }));
                await ctx.SaveChangesAsync();
            }

            // 3 queries secuenciales (proyecto/zonas/fases) en vez de 1 sola proyección:
            // la de zonas necesita Include+ToListAsync (ver comentario abajo, es la que
            // tenía el bug de traducción), lo que rompe el shape de "un solo Select desde
            // Project" que se usaba antes. No se combinan en 1: ni zonas se puede fusionar
            // dentro de la proyección de Project sin reintroducir el bug de Concat, ni se
            // paralelizan con Task.WhenAll: R2 (regla de codificación existente) prohíbe
            // Task.WhenAll contra la BD salvo Microsoft Graph. Si en el futuro se confirma
            // que R7 (IDbContextFactory por query paralela) reemplaza/reconcilia eso, esto
            // es candidato a paralelizarse con 3 DbContext separados — no se hizo acá para
            // no meter una técnica nueva sin probar en un hotfix de incidente.
            var proyecto = await ctx.Project
                .Where(p => p.ProjectId == projectId)
                .Select(p => new
                {
                    p.ResponsablePlaneamientoBimId,
                    p.ResponsablePlaneamientoBim,
                    p.MetaPpc,
                })
                .FirstAsync();

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

            var fases = await ctx.BimProyectoFase
                .Where(f => f.ProjectId == projectId)
                .OrderBy(f => f.Fase.Orden)
                .Select(f => new FaseDto
                {
                    Id = f.Id,
                    Nombre = f.Fase.Nombre,
                    FechaInicio = f.FechaInicio,
                    FechaFinMeta = f.FechaFinMeta,
                })
                .ToListAsync();

            return new ConfiguracionInicialDto
            {
                ResponsableId = proyecto.ResponsablePlaneamientoBimId,
                ResponsableNombre = proyecto.ResponsablePlaneamientoBim,
                MetaPpc = proyecto.MetaPpc,
                Zonas = zonas,
                Fases = fases,
            };
        }

        public async Task<List<ResponsableBimLookupDto>> GetResponsables()
        {
            using var ctx = _factory.CreateDbContext();

            return await ctx.Worker
                .Where(w => w.WorkersEstadoId == WorkersEstadoIds.Activo && w.Subarea == "Planeamiento BIM")
                .OrderBy(w => w.Person != null ? w.Person.FullName : null)
                .Select(w => new ResponsableBimLookupDto
                {
                    Id = w.Id,
                    ApellidoNombre = (w.Person != null ? w.Person.FullName : null) ?? string.Empty,
                })
                .ToListAsync();
        }

        public async Task GuardarConfiguracion(int projectId, ConfiguracionInicialUpdateDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var project = await ctx.Project.FirstOrDefaultAsync(p => p.ProjectId == projectId);
            if (project == null)
                throw new AbrilException("El proyecto no existe.", 404);

            var zonasExistentes = await ctx.BimProyectoZona
                .Where(z => z.ProjectId == projectId)
                .Include(z => z.Niveles)
                    .ThenInclude(n => n.Sectores)
                .Include(z => z.Sectores)
                .ToListAsync();

            var idsZonasEnviadas = dto.Zonas.Where(z => z.Id.HasValue).Select(z => z.Id!.Value).ToHashSet();
            foreach (var zonaEliminada in zonasExistentes.Where(z => !idsZonasEnviadas.Contains(z.Id)))
                ctx.BimProyectoZona.Remove(zonaEliminada);

            foreach (var zonaDto in dto.Zonas)
            {
                var zona = zonaDto.Id.HasValue
                    ? zonasExistentes.FirstOrDefault(z => z.Id == zonaDto.Id.Value)
                    : null;

                if (zona == null)
                {
                    zona = new BimProyectoZona { ProjectId = projectId };
                    ctx.BimProyectoZona.Add(zona);
                }

                zona.Nombre = (zonaDto.Nombre ?? string.Empty).Trim();
                zona.Orden = zonaDto.Orden;

                SincronizarNiveles(ctx, zona, zonaDto.Niveles);
                SincronizarSectoresCompartidos(ctx, zona, zonaDto.SectoresCompartidos);
            }

            if (dto.Fases.Count > 0)
            {
                var fasesExistentes = await ctx.BimProyectoFase
                    .Where(f => f.ProjectId == projectId)
                    .ToDictionaryAsync(f => f.Id);

                foreach (var faseDto in dto.Fases)
                {
                    if (!fasesExistentes.TryGetValue(faseDto.Id, out var fase))
                        throw new AbrilException($"La fase {faseDto.Id} no pertenece a este proyecto.", 400);

                    fase.FechaInicio = faseDto.FechaInicio;
                    fase.FechaFinMeta = faseDto.FechaFinMeta;
                }
            }

            project.ResponsablePlaneamientoBimId = dto.ResponsableId;
            project.ResponsablePlaneamientoBim = string.IsNullOrWhiteSpace(dto.ResponsableNombre) ? null : dto.ResponsableNombre.Trim();
            project.MetaPpc = dto.MetaPpc;
            project.UpdatedDateTime = DateTime.UtcNow;

            try
            {
                await ctx.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23503")
            {
                throw new AbrilException("No se puede eliminar una zona, nivel o sector que ya tiene registros de avance asociados.", 409);
            }
        }

        private static void SincronizarNiveles(AppDbContext ctx, BimProyectoZona zona, List<NivelUpdateDto> niveles)
        {
            var idsEnviados = niveles.Where(n => n.Id.HasValue).Select(n => n.Id!.Value).ToHashSet();
            foreach (var nivelEliminado in zona.Niveles.Where(n => !idsEnviados.Contains(n.Id)).ToList())
                ctx.BimZonaNivel.Remove(nivelEliminado);

            foreach (var nivelDto in niveles)
            {
                var nivel = nivelDto.Id.HasValue
                    ? zona.Niveles.FirstOrDefault(n => n.Id == nivelDto.Id.Value)
                    : null;

                if (nivel == null)
                {
                    nivel = new BimZonaNivel();
                    zona.Niveles.Add(nivel);
                }

                nivel.Nombre = (nivelDto.Nombre ?? string.Empty).Trim();
                nivel.Orden = nivelDto.Orden;
                nivel.TipoEstructura = nivelDto.TipoEstructura;

                SincronizarSectoresDeNivel(ctx, zona, nivel, nivelDto.Sectores);
            }
        }

        /// <summary>Solo toca sectores EXCLUSIVOS de este nivel (ZonaNivelId == nivel.Id).
        /// Nunca borra ni edita los "compartidos" — esos los maneja
        /// SincronizarSectoresCompartidos, aparte.</summary>
        private static void SincronizarSectoresDeNivel(AppDbContext ctx, BimProyectoZona zona, BimZonaNivel nivel, List<SectorUpdateDto> sectores)
        {
            var idsEnviados = sectores.Where(s => s.Id.HasValue).Select(s => s.Id!.Value).ToHashSet();
            foreach (var sectorEliminado in nivel.Sectores.Where(s => !idsEnviados.Contains(s.Id)).ToList())
                ctx.BimZonaSector.Remove(sectorEliminado);

            foreach (var sectorDto in sectores)
            {
                var sector = sectorDto.Id.HasValue
                    ? nivel.Sectores.FirstOrDefault(s => s.Id == sectorDto.Id.Value)
                    : null;

                if (sector == null)
                {
                    // BimZonaSector tiene 2 relaciones requeridas (Zona vía ZonaId NOT NULL,
                    // y ZonaNivel vía ZonaNivelId). nivel.Sectores.Add(sector) mas abajo solo
                    // hace fixup de la relacion con ZonaNivel — la relacion con Zona necesita
                    // su propio fixup explicito, igual que SincronizarSectoresCompartidos lo
                    // logra con zona.Sectores.Add(sector). Se asigna la navegacion (no el Id
                    // escalar) para que funcione aunque zona.Id todavia sea 0 (zona nueva).
                    sector = new BimZonaSector { Zona = zona };
                    nivel.Sectores.Add(sector);
                }

                sector.Nombre = (sectorDto.Nombre ?? string.Empty).Trim();
                sector.Orden = sectorDto.Orden;
            }
        }

        /// <summary>Solo toca sectores COMPARTIDOS de la zona (ZonaNivelId == null).
        /// Nunca borra ni edita los exclusivos de un nivel.</summary>
        private static void SincronizarSectoresCompartidos(AppDbContext ctx, BimProyectoZona zona, List<SectorUpdateDto> sectores)
        {
            var compartidosExistentes = zona.Sectores.Where(s => s.ZonaNivelId == null).ToList();
            var idsEnviados = sectores.Where(s => s.Id.HasValue).Select(s => s.Id!.Value).ToHashSet();
            foreach (var sectorEliminado in compartidosExistentes.Where(s => !idsEnviados.Contains(s.Id)))
                ctx.BimZonaSector.Remove(sectorEliminado);

            foreach (var sectorDto in sectores)
            {
                var sector = sectorDto.Id.HasValue
                    ? compartidosExistentes.FirstOrDefault(s => s.Id == sectorDto.Id.Value)
                    : null;

                if (sector == null)
                {
                    sector = new BimZonaSector { ZonaNivelId = null };
                    zona.Sectores.Add(sector);
                }

                sector.Nombre = (sectorDto.Nombre ?? string.Empty).Trim();
                sector.Orden = sectorDto.Orden;
            }
        }
    }
}

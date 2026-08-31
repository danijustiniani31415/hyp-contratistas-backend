using Abril_Backend.Shared.Constants;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.VecinosModule.Features.CroquisFeature.Application.Dtos;
using Abril_Backend.Features.VecinosModule.Features.CroquisFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.VecinosModule.Features.CroquisFeature.Infrastructure.Models;
using Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Application.Dtos;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Abril_Backend.Features.VecinosModule.Features.CroquisFeature.Infrastructure.Repositories
{
    public class CroquisRepository : ICroquisRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public CroquisRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<ProjectCroquisItemDto>> GetProjectsWithCroquis(string? search)
        {
            using var ctx = _factory.CreateDbContext();

            var query =
                from p in ctx.Project
                where p.State && p.Active && !ctx.ProyectoFiltro.Any(f => f.ProjectId == p.ProjectId && f.FuncionalidadId == ProyectoFiltroFuncionalidades.VecinosCroquis && !f.Active)
                join cr in ctx.ProjectCroquis.Where(c => c.State)
                    on p.ProjectId equals cr.ProjectId into croquisGroup
                from cr in croquisGroup.DefaultIfEmpty()
                select new ProjectCroquisItemDto
                {
                    ProjectId = p.ProjectId,
                    ProjectDescription = p.ProjectDescription,
                    ProjectCroquisId = cr != null ? cr.ProjectCroquisId : (int?)null,
                    ImageUrl = cr != null ? cr.ImageUrl : null,
                    OriginalFileName = cr != null ? cr.OriginalFileName : null,
                    UpdatedDateTime = cr != null ? (cr.UpdatedDateTime ?? cr.CreatedDateTime) : (DateTime?)null,
                };

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(x => x.ProjectDescription.ToLower().Contains(s));
            }

            return await query.OrderBy(x => x.ProjectDescription).ToListAsync();
        }

        public async Task UpsertCroquis(int projectId, string imageUrl, string? originalFileName, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var projectExists = await ctx.Project.AnyAsync(p => p.ProjectId == projectId && p.State);
            if (!projectExists)
                throw new AbrilException("El proyecto no existe.", 404);

            var now = DateTime.UtcNow;

            // Reemplazo de imagen EN SITIO: se conserva el mismo croquis (y por tanto sus lotes y los
            // vecinos ligados a ellos). Antes se soft-deleteaba el croquis y se creaba uno nuevo, lo que
            // dejaba huérfanos todos los polígonos/lotes ya dibujados.
            var existing = await ctx.ProjectCroquis
                .Where(c => c.ProjectId == projectId && c.State)
                .OrderByDescending(c => c.ProjectCroquisId)
                .FirstOrDefaultAsync();

            if (existing is not null)
            {
                existing.ImageUrl = imageUrl;
                existing.OriginalFileName = originalFileName;
                existing.UpdatedDateTime = now;
                existing.UpdatedUserId = userId;
            }
            else
            {
                ctx.ProjectCroquis.Add(new ProjectCroquis
                {
                    ProjectId = projectId,
                    ImageUrl = imageUrl,
                    OriginalFileName = originalFileName,
                    CreatedDateTime = now,
                    CreatedUserId = userId,
                    Active = true,
                    State = true,
                });
            }

            await ctx.SaveChangesAsync();
        }

        public async Task<List<CroquisLoteDto>> GetLotes(int projectCroquisId)
        {
            using var ctx = _factory.CreateDbContext();

            var rows = await ctx.ProjectCroquisLote
                .Where(l => l.ProjectCroquisId == projectCroquisId && l.State)
                .OrderBy(l => l.ProjectCroquisLoteId)
                .Select(l => new { l.ProjectCroquisLoteId, l.NumeroLote, l.Poligono })
                .ToListAsync();

            return rows.Select(r => new CroquisLoteDto
            {
                ProjectCroquisLoteId = r.ProjectCroquisLoteId,
                NumeroLote = r.NumeroLote,
                Puntos = DeserializePuntos(r.Poligono),
            }).ToList();
        }

        public async Task ReplaceLotes(int projectCroquisId, List<CroquisLoteDto> lotes, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var croquisExists = await ctx.ProjectCroquis.AnyAsync(c => c.ProjectCroquisId == projectCroquisId && c.State);
            if (!croquisExists)
                throw new AbrilException("El croquis no existe.", 404);

            var now = DateTime.UtcNow;

            // Validación previa: ningún lote puede tener menos de 3 puntos.
            foreach (var lote in lotes)
            {
                if (lote.Puntos == null || lote.Puntos.Count < 3)
                    throw new AbrilException($"El lote '{lote.NumeroLote}' debe tener al menos 3 puntos.", 422);
            }

            var existing = await ctx.ProjectCroquisLote
                .Where(l => l.ProjectCroquisId == projectCroquisId && l.State)
                .ToListAsync();
            var existingById = existing.ToDictionary(l => l.ProjectCroquisLoteId);

            // Diff por id: se actualizan los lotes conservados (preservando su lote de negocio
            // VecinoLoteId), se insertan los nuevos y se da de baja a los quitados.
            var keptIds = new HashSet<int>();

            foreach (var lote in lotes)
            {
                if (lote.ProjectCroquisLoteId.HasValue
                    && existingById.TryGetValue(lote.ProjectCroquisLoteId.Value, out var current))
                {
                    // Lote conservado: actualizar geometría/etiqueta sin tocar el VecinoLoteId.
                    current.NumeroLote = lote.NumeroLote;
                    current.Poligono = JsonSerializer.Serialize(lote.Puntos);
                    current.UpdatedDateTime = now;
                    current.UpdatedUserId = userId;
                    keptIds.Add(current.ProjectCroquisLoteId);
                }
                else
                {
                    // Lote nuevo: aún sin lote de negocio (se crea al registrar sus vecinos en Gestión).
                    ctx.ProjectCroquisLote.Add(new ProjectCroquisLote
                    {
                        ProjectCroquisId = projectCroquisId,
                        NumeroLote = lote.NumeroLote,
                        Poligono = JsonSerializer.Serialize(lote.Puntos),
                        CreatedDateTime = now,
                        CreatedUserId = userId,
                        Active = true,
                        State = true,
                    });
                }
            }

            // Lotes que el usuario quitó del croquis.
            var removed = existing.Where(l => !keptIds.Contains(l.ProjectCroquisLoteId)).ToList();
            if (removed.Count > 0)
            {
                // No se puede quitar un lote (polígono) que ya tiene vecinos registrados.
                var loteIdsRemovidos = removed.Where(l => l.VecinoLoteId.HasValue)
                    .Select(l => l.VecinoLoteId!.Value).ToList();
                if (loteIdsRemovidos.Count > 0)
                {
                    var conVecinos = await ctx.Vecino
                        .Where(v => v.State && v.Active && loteIdsRemovidos.Contains(v.VecinoLoteId))
                        .Select(v => v.VecinoLoteId)
                        .Distinct()
                        .ToListAsync();
                    if (conVecinos.Count > 0)
                    {
                        var etiquetas = string.Join(", ", removed
                            .Where(l => l.VecinoLoteId.HasValue && conVecinos.Contains(l.VecinoLoteId.Value))
                            .Select(l => l.NumeroLote));
                        throw new AbrilException(
                            $"No se puede quitar el/los lote(s) '{etiquetas}' porque tienen vecinos registrados.", 422);
                    }
                }

                foreach (var old in removed)
                {
                    old.State = false;
                    old.UpdatedDateTime = now;
                    old.UpdatedUserId = userId;
                }
            }

            await ctx.SaveChangesAsync();
        }

        public async Task<CroquisGestionResponseDto> GetGestion()
        {
            using var ctx = _factory.CreateDbContext();

            // Catálogos para el formulario de alta de vecino (siempre se devuelven).
            var colindancias = await ctx.VecinoColindancia
                .Where(c => c.State && c.Active)
                .OrderBy(c => c.VecinoColindanciaId)
                .Select(c => new CatalogOptionDto { Id = c.VecinoColindanciaId, Descripcion = c.Descripcion })
                .ToListAsync();

            var tipos = await ctx.VecinoTipoConstruccion
                .Where(t => t.State && t.Active)
                .OrderBy(t => t.VecinoTipoConstruccionId)
                .Select(t => new CatalogOptionDto { Id = t.VecinoTipoConstruccionId, Descripcion = t.Descripcion })
                .ToListAsync();

            var usos = await ctx.VecinoUso
                .Where(u => u.State && u.Active)
                .OrderBy(u => u.VecinoUsoId)
                .Select(u => new CatalogOptionDto { Id = u.VecinoUsoId, Descripcion = u.Descripcion })
                .ToListAsync();

            var relacionTipos = await ctx.VecinoRelacionTipo
                .Where(r => r.State && r.Active)
                .OrderBy(r => r.VecinoRelacionTipoId)
                .Select(r => new CatalogOptionDto { Id = r.VecinoRelacionTipoId, Descripcion = r.Descripcion })
                .ToListAsync();

            var projects = await ctx.Project
                .Where(p => p.State && p.Active && !ctx.ProyectoFiltro.Any(f => f.ProjectId == p.ProjectId && f.FuncionalidadId == ProyectoFiltroFuncionalidades.VecinosCroquis && !f.Active))
                .OrderBy(p => p.ProjectDescription)
                .Select(p => new ProjectOptionDto { ProjectId = p.ProjectId, ProjectDescription = p.ProjectDescription })
                .ToListAsync();

            var response = new CroquisGestionResponseDto
            {
                Projects = projects,
                Colindancias = colindancias,
                TiposConstruccion = tipos,
                Usos = usos,
                RelacionTipos = relacionTipos,
            };

            // Croquis registrados (uno activo por proyecto).
            var croquis = await (
                from c in ctx.ProjectCroquis.Where(c => c.State)
                join p in ctx.Project on c.ProjectId equals p.ProjectId
                orderby p.ProjectDescription
                select new { c.ProjectCroquisId, c.ImageUrl, c.ProjectId, p.ProjectDescription }
            ).ToListAsync();

            if (croquis.Count == 0) return response;

            var croquisIds = croquis.Select(c => c.ProjectCroquisId).ToList();
            var projectIds = croquis.Select(c => c.ProjectId).ToList();

            // Lotes (polígonos) de esos croquis. Cada polígono representa un lote/edificio.
            var lotes = await (
                from l in ctx.ProjectCroquisLote.Where(l => l.State && croquisIds.Contains(l.ProjectCroquisId))
                orderby l.ProjectCroquisLoteId
                select new
                {
                    l.ProjectCroquisLoteId,
                    l.ProjectCroquisId,
                    l.NumeroLote,
                    l.Poligono,
                    l.VecinoLoteId,
                }
            ).ToListAsync();

            // Direcciones/observaciones de los lotes de esos proyectos.
            var loteInfo = await ctx.VecinoLote
                .Where(lo => lo.State && projectIds.Contains(lo.ProjectId))
                .Select(lo => new { lo.VecinoLoteId, lo.Direccion, lo.Observaciones })
                .ToDictionaryAsync(x => x.VecinoLoteId, x => x);

            // Vecinos/departamentos de esos proyectos.
            var vecinos = await (
                from v in ctx.Vecino.Where(v => v.State && v.Active && projectIds.Contains(v.ProjectId))
                join p in ctx.Project on v.ProjectId equals p.ProjectId
                join col in ctx.VecinoColindancia on v.VecinoColindanciaId equals col.VecinoColindanciaId
                join tc in ctx.VecinoTipoConstruccion on v.VecinoTipoConstruccionId equals tc.VecinoTipoConstruccionId
                join u in ctx.VecinoUso on v.VecinoUsoId equals u.VecinoUsoId into uj
                from u in uj.DefaultIfEmpty()
                orderby v.VecinoId
                select new { v, p, col, tc, u }
            ).ToListAsync();

            // Conteo de solicitudes por vecino.
            var vecinoIds = vecinos.Select(x => x.v.VecinoId).ToList();

            // Personas de cada casa (una sola consulta), con propietario primero. Sirve para el
            // nombre principal mostrado en la tabla, las tarjetas y el nombre del vecino en cada lote.
            var personasRows = await (
                from per in ctx.VecinoPersona
                join rt in ctx.VecinoRelacionTipo on per.VecinoRelacionTipoId equals rt.VecinoRelacionTipoId
                where vecinoIds.Contains(per.VecinoId) && per.Active && per.State
                orderby per.VecinoRelacionTipoId, per.VecinoPersonaId
                select new { per.VecinoId, Dto = new VecinoPersonaDto
                {
                    VecinoPersonaId = per.VecinoPersonaId,
                    Nombre = per.Nombre,
                    Dni = per.Dni,
                    Celular = per.Celular,
                    VecinoRelacionTipoId = per.VecinoRelacionTipoId,
                    RelacionDescripcion = rt.Descripcion
                }}
            ).ToListAsync();

            var personasByVecino = personasRows
                .GroupBy(x => x.VecinoId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Dto).ToList());

            // Imágenes (estado de la propiedad) de cada casa, en una sola consulta.
            var imagenesByVecino = (await ctx.VecinoImagen
                .Where(im => vecinoIds.Contains(im.VecinoId) && im.Active && im.State)
                .OrderBy(im => im.VecinoImagenId)
                .Select(im => new { im.VecinoId, Dto = new VecinoImagenDto
                {
                    VecinoImagenId = im.VecinoImagenId,
                    ArchivoUrl = im.ArchivoUrl,
                    OriginalFileName = im.OriginalFileName
                }})
                .ToListAsync())
                .GroupBy(x => x.VecinoId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Dto).ToList());

            // Ids de los estados de solicitud relevantes para el % de aprobadas.
            var solicitudEstados = await ctx.VecinoSolicitudEstado
                .Where(e => e.State)
                .Select(e => new { e.VecinoSolicitudEstadoId, e.Descripcion })
                .ToListAsync();
            int solAceptadaId = solicitudEstados.FirstOrDefault(e => e.Descripcion == "Aceptada")?.VecinoSolicitudEstadoId ?? -1;
            int solDenegadaId = solicitudEstados.FirstOrDefault(e => e.Descripcion == "Denegada")?.VecinoSolicitudEstadoId ?? -1;

            // Solicitudes por vecino: total, aprobadas (Aceptada) y evaluables (Aceptada + Por responder, sin Denegada).
            var solicitudesPorVecino = await (
                from s in ctx.VecinoSolicitud.Where(s => s.State && s.Active && vecinoIds.Contains(s.VecinoId))
                group s by s.VecinoId into g
                select new
                {
                    VecinoId = g.Key,
                    Count = g.Count(),
                    Aprobadas = g.Count(x => x.VecinoSolicitudEstadoId == solAceptadaId),
                    Evaluables = g.Count(x => x.VecinoSolicitudEstadoId != solDenegadaId),
                }
            ).ToDictionaryAsync(x => x.VecinoId, x => new { x.Count, x.Aprobadas, x.Evaluables });

            // Conteo de compromisos por vecino (compromiso → solicitud → vecino).
            var compromisosPorVecino = await (
                from c in ctx.VecinoCompromiso.Where(c => c.State && c.Active)
                join s in ctx.VecinoSolicitud.Where(s => s.State && s.Active) on c.VecinoSolicitudId equals s.VecinoSolicitudId
                where vecinoIds.Contains(s.VecinoId)
                group c by s.VecinoId into g
                select new { VecinoId = g.Key, Count = g.Count() }
            ).ToDictionaryAsync(x => x.VecinoId, x => x.Count);

            // Estados de compromiso (para el desglose de las barras del resumen del proyecto).
            var compromisoEstados = await ctx.VecinoCompromisoEstado
                .Where(e => e.State)
                .Select(e => new { e.VecinoCompromisoEstadoId, e.Descripcion })
                .ToListAsync();
            int compPendienteId = compromisoEstados.FirstOrDefault(e => e.Descripcion == "Pendiente")?.VecinoCompromisoEstadoId ?? -1;
            int compEnProcesoId = compromisoEstados.FirstOrDefault(e => e.Descripcion == "En proceso")?.VecinoCompromisoEstadoId ?? -1;
            int compCulminadoId = compromisoEstados.FirstOrDefault(e => e.Descripcion == "Culminado")?.VecinoCompromisoEstadoId ?? -1;

            // Desglose de compromisos por vecino, estado y si tienen fecha límite por municipalidad/fiscalización.
            var compromisosDesglose = await (
                from c in ctx.VecinoCompromiso.Where(c => c.State && c.Active)
                join s in ctx.VecinoSolicitud.Where(s => s.State && s.Active) on c.VecinoSolicitudId equals s.VecinoSolicitudId
                where vecinoIds.Contains(s.VecinoId)
                group c by new { s.VecinoId, c.VecinoCompromisoEstadoId, ConLimite = c.FechaFinMunicipalidad != null } into g
                select new { g.Key.VecinoId, g.Key.VecinoCompromisoEstadoId, g.Key.ConLimite, Count = g.Count() }
            ).ToListAsync();

            // Mapa vecino → proyecto, para agregar el desglose por proyecto.
            var vecinoProyecto = vecinos.ToDictionary(x => x.v.VecinoId, x => x.v.ProjectId);
            int CompCount(int projectId, int estadoId, bool? conLimite) => compromisosDesglose
                .Where(x => vecinoProyecto.GetValueOrDefault(x.VecinoId) == projectId
                    && x.VecinoCompromisoEstadoId == estadoId
                    && (conLimite == null || x.ConLimite == conLimite.Value))
                .Sum(x => x.Count);

            // Ids de los estados de entregable relevantes para el % de aprobados.
            var entregableEstados = await ctx.VecinoEntregableEstado
                .Where(e => e.State)
                .Select(e => new { e.VecinoEntregableEstadoId, e.Descripcion })
                .ToListAsync();
            int noAplicaId = entregableEstados.FirstOrDefault(e => e.Descripcion == "No aplica")?.VecinoEntregableEstadoId ?? -1;
            int aprobadoId = entregableEstados.FirstOrDefault(e => e.Descripcion == "Aprobado")?.VecinoEntregableEstadoId ?? -1;

            // Entregables por vecino (entregable → compromiso → solicitud → vecino),
            // excluyendo los "No aplica" del cálculo del porcentaje de aprobados.
            var entregablesPorVecino = await (
                from e in ctx.VecinoCompromisoEntregable.Where(e => e.State && e.Active && e.VecinoEntregableEstadoId != noAplicaId)
                join cm in ctx.VecinoCompromiso.Where(c => c.State && c.Active) on e.VecinoCompromisoId equals cm.VecinoCompromisoId
                join s in ctx.VecinoSolicitud.Where(s => s.State && s.Active) on cm.VecinoSolicitudId equals s.VecinoSolicitudId
                where vecinoIds.Contains(s.VecinoId)
                group e by s.VecinoId into g
                select new
                {
                    VecinoId = g.Key,
                    Aprobados = g.Count(x => x.VecinoEntregableEstadoId == aprobadoId),
                    Evaluables = g.Count(),
                }
            ).ToDictionaryAsync(x => x.VecinoId, x => new { x.Aprobados, x.Evaluables });

            // Requisitos por vecino: subidos y "no aplica" (el resto se cuenta como "no subido").
            int totalTiposRequisito = await ctx.VecinoRequisitoTipo.CountAsync(t => t.State && t.Active);
            var reqEstados = await ctx.VecinoRequisitoEstado
                .Where(e => e.State)
                .Select(e => new { e.VecinoRequisitoEstadoId, e.Descripcion })
                .ToListAsync();
            int reqSubidoId = reqEstados.FirstOrDefault(e => e.Descripcion == "Subido")?.VecinoRequisitoEstadoId ?? -1;
            int reqNoAplicaId = reqEstados.FirstOrDefault(e => e.Descripcion == "No aplica")?.VecinoRequisitoEstadoId ?? -1;

            var requisitosPorVecino = await (
                from r in ctx.VecinoRequisito.Where(r => r.State && vecinoIds.Contains(r.VecinoId))
                group r by r.VecinoId into g
                select new
                {
                    VecinoId = g.Key,
                    Subidos = g.Count(x => x.VecinoRequisitoEstadoId == reqSubidoId),
                    NoAplica = g.Count(x => x.VecinoRequisitoEstadoId == reqNoAplicaId),
                }
            ).ToDictionaryAsync(x => x.VecinoId, x => new { x.Subidos, x.NoAplica });

            // Evaluables de un vecino = total de tipos - los marcados "No aplica".
            int reqEvaluablesDe(int vid) =>
                totalTiposRequisito - (requisitosPorVecino.GetValueOrDefault(vid)?.NoAplica ?? 0);
            int reqSubidosDe(int vid) =>
                requisitosPorVecino.GetValueOrDefault(vid)?.Subidos ?? 0;

            // DTO por vecino/departamento (con sus KPIs), construido una sola vez.
            var vecinoDtos = vecinos.Select(x => new VecinoListItemDto
            {
                VecinoId = x.v.VecinoId,
                VecinoLoteId = x.v.VecinoLoteId,
                ProjectId = x.v.ProjectId,
                ProjectDescription = x.p.ProjectDescription,
                Predio = x.v.Predio,
                VecinoUsoId = x.v.VecinoUsoId,
                UsoDescripcion = x.u != null ? x.u.Descripcion : null,
                Direccion = loteInfo.GetValueOrDefault(x.v.VecinoLoteId)?.Direccion,
                InteriorDepartamento = x.v.InteriorDepartamento,
                NombrePropietario = personasByVecino.GetValueOrDefault(x.v.VecinoId)?.FirstOrDefault()?.Nombre,
                Dni = personasByVecino.GetValueOrDefault(x.v.VecinoId)?.FirstOrDefault()?.Dni,
                Celular = personasByVecino.GetValueOrDefault(x.v.VecinoId)?.FirstOrDefault()?.Celular,
                Personas = personasByVecino.GetValueOrDefault(x.v.VecinoId, new()),
                Imagenes = imagenesByVecino.GetValueOrDefault(x.v.VecinoId, new()),
                VecinoColindanciaId = x.v.VecinoColindanciaId,
                ColindanciaDescripcion = x.col.Descripcion,
                VecinoTipoConstruccionId = x.v.VecinoTipoConstruccionId,
                TipoConstruccionDescripcion = x.tc.Descripcion,
                Observaciones = loteInfo.GetValueOrDefault(x.v.VecinoLoteId)?.Observaciones,
                CreatedDateTime = x.v.CreatedDateTime,
                SolicitudesCount = solicitudesPorVecino.GetValueOrDefault(x.v.VecinoId)?.Count ?? 0,
                CompromisosCount = compromisosPorVecino.GetValueOrDefault(x.v.VecinoId),
                SolicitudesAprobadas = solicitudesPorVecino.GetValueOrDefault(x.v.VecinoId)?.Aprobadas ?? 0,
                SolicitudesEvaluables = solicitudesPorVecino.GetValueOrDefault(x.v.VecinoId)?.Evaluables ?? 0,
                EntregablesAprobados = entregablesPorVecino.GetValueOrDefault(x.v.VecinoId)?.Aprobados ?? 0,
                EntregablesEvaluables = entregablesPorVecino.GetValueOrDefault(x.v.VecinoId)?.Evaluables ?? 0,
                RequisitosSubidos = reqSubidosDe(x.v.VecinoId),
                RequisitosEvaluables = reqEvaluablesDe(x.v.VecinoId),
            }).ToList();

            // Agrupar los vecinos por su lote (para anidarlos bajo cada polígono).
            var vecinosByLote = vecinoDtos
                .GroupBy(v => v.VecinoLoteId)
                .ToDictionary(g => g.Key, g => g.ToList());

            response.Croquis = croquis.Select(c =>
            {
                var projectVecinos = vecinoDtos.Where(v => v.ProjectId == c.ProjectId).ToList();
                var projectLotes = lotes.Where(l => l.ProjectCroquisId == c.ProjectCroquisId).ToList();

                return new CroquisGestionDto
                {
                    ProjectId = c.ProjectId,
                    ProjectDescription = c.ProjectDescription,
                    ProjectCroquisId = c.ProjectCroquisId,
                    ImageUrl = c.ImageUrl,
                    SolicitudesCount = projectVecinos.Sum(v => v.SolicitudesCount),
                    SolicitudesAprobadas = projectVecinos.Sum(v => v.SolicitudesAprobadas),
                    SolicitudesEvaluables = projectVecinos.Sum(v => v.SolicitudesEvaluables),
                    CompromisosCount = projectVecinos.Sum(v => v.CompromisosCount),
                    CompromisosPendientes = CompCount(c.ProjectId, compPendienteId, null),
                    CompromisosEnProceso = CompCount(c.ProjectId, compEnProcesoId, null),
                    CompromisosCulminados = CompCount(c.ProjectId, compCulminadoId, null),
                    CompromisosLimitePendientes = CompCount(c.ProjectId, compPendienteId, true),
                    CompromisosLimiteEnProceso = CompCount(c.ProjectId, compEnProcesoId, true),
                    CompromisosLimiteCulminados = CompCount(c.ProjectId, compCulminadoId, true),
                    EntregablesAprobados = projectVecinos.Sum(v => v.EntregablesAprobados),
                    EntregablesEvaluables = projectVecinos.Sum(v => v.EntregablesEvaluables),
                    RequisitosSubidos = projectVecinos.Sum(v => v.RequisitosSubidos),
                    RequisitosEvaluables = projectVecinos.Sum(v => v.RequisitosEvaluables),
                    LotesCount = projectLotes.Count,
                    VecinosCount = projectVecinos.Count,
                    Lotes = projectLotes.Select(l =>
                    {
                        var loteVecinos = l.VecinoLoteId.HasValue
                            ? vecinosByLote.GetValueOrDefault(l.VecinoLoteId.Value, new())
                            : new List<VecinoListItemDto>();
                        return new CroquisGestionLoteDto
                        {
                            ProjectCroquisLoteId = l.ProjectCroquisLoteId,
                            NumeroLote = l.NumeroLote,
                            Puntos = DeserializePuntos(l.Poligono),
                            VecinoLoteId = l.VecinoLoteId,
                            Direccion = l.VecinoLoteId.HasValue ? loteInfo.GetValueOrDefault(l.VecinoLoteId.Value)?.Direccion : null,
                            Observaciones = l.VecinoLoteId.HasValue ? loteInfo.GetValueOrDefault(l.VecinoLoteId.Value)?.Observaciones : null,
                            VecinosCount = loteVecinos.Count,
                            SolicitudesCount = loteVecinos.Sum(v => v.SolicitudesCount),
                            CompromisosCount = loteVecinos.Sum(v => v.CompromisosCount),
                            SolicitudesAprobadas = loteVecinos.Sum(v => v.SolicitudesAprobadas),
                            SolicitudesEvaluables = loteVecinos.Sum(v => v.SolicitudesEvaluables),
                            EntregablesAprobados = loteVecinos.Sum(v => v.EntregablesAprobados),
                            EntregablesEvaluables = loteVecinos.Sum(v => v.EntregablesEvaluables),
                            RequisitosSubidos = loteVecinos.Sum(v => v.RequisitosSubidos),
                            RequisitosEvaluables = loteVecinos.Sum(v => v.RequisitosEvaluables),
                            Vecinos = loteVecinos,
                        };
                    }).ToList(),
                };
            }).ToList();

            return response;
        }

        private static List<List<double>> DeserializePuntos(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<List<List<double>>>(json) ?? new();
            }
            catch
            {
                return new();
            }
        }
    }
}

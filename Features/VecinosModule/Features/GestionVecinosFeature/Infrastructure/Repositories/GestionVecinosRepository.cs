using Abril_Backend.Shared.Constants;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Application.Dtos;
using Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Infrastructure.Repositories
{
    public class GestionVecinosRepository : IGestionVecinosRepository
    {
        private const int PageSize = 12;

        private readonly IDbContextFactory<AppDbContext> _factory;

        public GestionVecinosRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<VecinoFormOptionsDto> GetOptions()
        {
            using var ctx = _factory.CreateDbContext();

            var projects = await ctx.Project
                .Where(p => p.State && p.Active && !ctx.ProyectoFiltro.Any(f => f.ProjectId == p.ProjectId && f.FuncionalidadId == ProyectoFiltroFuncionalidades.VecinosGestion && !f.Active))
                .OrderBy(p => p.ProjectDescription)
                .Select(p => new ProjectOptionDto
                {
                    ProjectId = p.ProjectId,
                    ProjectDescription = p.ProjectDescription
                })
                .ToListAsync();

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

            return new VecinoFormOptionsDto
            {
                Projects = projects,
                Colindancias = colindancias,
                TiposConstruccion = tipos,
                Usos = usos,
                RelacionTipos = relacionTipos
            };
        }

        /// <summary>
        /// Carga las personas de un conjunto de casas (vecinos) en una sola consulta para evitar N+1.
        /// Devuelve un diccionario vecinoId → lista de personas (propietario primero).
        /// </summary>
        private static async Task<Dictionary<int, List<VecinoPersonaDto>>> LoadPersonas(AppDbContext ctx, List<int> vecinoIds)
        {
            if (vecinoIds.Count == 0) return new();

            var personas = await (
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

            return personas
                .GroupBy(x => x.VecinoId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Dto).ToList());
        }

        /// <summary>Carga las imágenes (estado de la propiedad) de un conjunto de casas en una sola consulta.</summary>
        private static async Task<Dictionary<int, List<VecinoImagenDto>>> LoadImagenes(AppDbContext ctx, List<int> vecinoIds)
        {
            if (vecinoIds.Count == 0) return new();

            var imagenes = await ctx.VecinoImagen
                .Where(im => vecinoIds.Contains(im.VecinoId) && im.Active && im.State)
                .OrderBy(im => im.VecinoImagenId)
                .Select(im => new { im.VecinoId, Dto = new VecinoImagenDto
                {
                    VecinoImagenId = im.VecinoImagenId,
                    ArchivoUrl = im.ArchivoUrl,
                    OriginalFileName = im.OriginalFileName
                }})
                .ToListAsync();

            return imagenes
                .GroupBy(x => x.VecinoId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Dto).ToList());
        }

        public async Task<List<VecinoImagenDto>> AddImagenes(int vecinoId, List<(string ArchivoUrl, string? OriginalFileName)> imagenes, int userId)
        {
            if (imagenes.Count == 0) return new();
            using var ctx = _factory.CreateDbContext();

            var now = DateTime.UtcNow;
            var entities = imagenes.Select(img => new VecinoImagen
            {
                VecinoId = vecinoId,
                ArchivoUrl = img.ArchivoUrl,
                OriginalFileName = img.OriginalFileName,
                CreatedDateTime = now,
                CreatedUserId = userId,
                Active = true,
                State = true
            }).ToList();

            ctx.VecinoImagen.AddRange(entities);
            await ctx.SaveChangesAsync();

            return entities.Select(e => new VecinoImagenDto
            {
                VecinoImagenId = e.VecinoImagenId,
                ArchivoUrl = e.ArchivoUrl,
                OriginalFileName = e.OriginalFileName
            }).ToList();
        }

        public async Task<bool> DeleteImagen(int imagenId, int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var img = await ctx.VecinoImagen.FirstOrDefaultAsync(i => i.VecinoImagenId == imagenId && i.State);
            if (img is null) return false;

            img.State = false;
            img.UpdatedDateTime = DateTime.UtcNow;
            img.UpdatedUserId = userId;
            await ctx.SaveChangesAsync();
            return true;
        }

        public async Task<bool> Update(int vecinoId, VecinoUpdateDto dto, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var vecino = await ctx.Vecino.FirstOrDefaultAsync(v => v.VecinoId == vecinoId && v.Active && v.State);
            if (vecino is null) return false;

            var now = DateTime.UtcNow;

            vecino.VecinoUsoId = dto.VecinoUsoId;
            vecino.InteriorDepartamento = dto.InteriorDepartamento?.Trim();
            vecino.VecinoColindanciaId = dto.VecinoColindanciaId;
            vecino.VecinoTipoConstruccionId = dto.VecinoTipoConstruccionId;
            vecino.UpdatedDateTime = now;
            vecino.UpdatedUserId = userId;

            // Reconciliar personas: actualizar existentes, insertar nuevas, soft-delete las removidas.
            var existentes = await ctx.VecinoPersona
                .Where(p => p.VecinoId == vecinoId && p.Active && p.State)
                .ToListAsync();

            var idsEnviados = dto.Personas.Where(p => p.VecinoPersonaId.HasValue).Select(p => p.VecinoPersonaId!.Value).ToHashSet();

            foreach (var ex in existentes.Where(e => !idsEnviados.Contains(e.VecinoPersonaId)))
            {
                ex.State = false;
                ex.UpdatedDateTime = now;
                ex.UpdatedUserId = userId;
            }

            foreach (var per in dto.Personas)
            {
                var nombre = per.Nombre.Trim();
                var pdni = string.IsNullOrWhiteSpace(per.Dni) ? null : per.Dni.Trim();
                var celular = string.IsNullOrWhiteSpace(per.Celular) ? null : per.Celular.Trim();

                if (per.VecinoPersonaId.HasValue)
                {
                    var row = existentes.FirstOrDefault(e => e.VecinoPersonaId == per.VecinoPersonaId.Value);
                    if (row is null) continue;
                    row.Nombre = nombre;
                    row.Dni = pdni;
                    row.Celular = celular;
                    row.VecinoRelacionTipoId = per.VecinoRelacionTipoId;
                    row.UpdatedDateTime = now;
                    row.UpdatedUserId = userId;
                }
                else
                {
                    ctx.VecinoPersona.Add(new VecinoPersona
                    {
                        VecinoId = vecinoId,
                        Nombre = nombre,
                        Dni = pdni,
                        Celular = celular,
                        VecinoRelacionTipoId = per.VecinoRelacionTipoId,
                        CreatedDateTime = now,
                        CreatedUserId = userId,
                        Active = true,
                        State = true
                    });
                }
            }

            await ctx.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateLote(int vecinoLoteId, VecinoLoteUpdateDto dto, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var lote = await ctx.VecinoLote.FirstOrDefaultAsync(l => l.VecinoLoteId == vecinoLoteId && l.Active && l.State);
            if (lote is null) return false;

            lote.Direccion = dto.Direccion.Trim();
            lote.Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones.Trim();
            lote.UpdatedDateTime = DateTime.UtcNow;
            lote.UpdatedUserId = userId;
            await ctx.SaveChangesAsync();
            return true;
        }

        public async Task<PagedResult<VecinoListItemDto>> GetPaged(VecinoFilterDto filter)
        {
            using var ctx = _factory.CreateDbContext();

            var query =
                from v in ctx.Vecino
                join p in ctx.Project on v.ProjectId equals p.ProjectId
                join lo in ctx.VecinoLote on v.VecinoLoteId equals lo.VecinoLoteId into loj
                from lo in loj.DefaultIfEmpty()
                join col in ctx.VecinoColindancia on v.VecinoColindanciaId equals col.VecinoColindanciaId
                join tc in ctx.VecinoTipoConstruccion on v.VecinoTipoConstruccionId equals tc.VecinoTipoConstruccionId
                join u in ctx.VecinoUso on v.VecinoUsoId equals u.VecinoUsoId into uj
                from u in uj.DefaultIfEmpty()
                where v.Active && v.State
                select new { v, p, lo, col, tc, u };

            if (filter.ProjectId.HasValue)
                query = query.Where(x => x.v.ProjectId == filter.ProjectId.Value);

            if (filter.VecinoColindanciaId.HasValue)
                query = query.Where(x => x.v.VecinoColindanciaId == filter.VecinoColindanciaId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var s = filter.Search.ToLower();
                query = query.Where(x =>
                    (x.lo != null && x.lo.Direccion != null && x.lo.Direccion.ToLower().Contains(s)) ||
                    ctx.VecinoPersona.Any(per => per.VecinoId == x.v.VecinoId && per.Active && per.State &&
                        (per.Nombre.ToLower().Contains(s) || (per.Dni != null && per.Dni.Contains(s)))));
            }

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.v.VecinoId)
                .Skip((filter.Page - 1) * PageSize)
                .Take(PageSize)
                .Select(x => new VecinoListItemDto
                {
                    VecinoId = x.v.VecinoId,
                    VecinoLoteId = x.v.VecinoLoteId,
                    ProjectId = x.v.ProjectId,
                    ProjectDescription = x.p.ProjectDescription,
                    Predio = x.v.Predio,
                    VecinoUsoId = x.v.VecinoUsoId,
                    UsoDescripcion = x.u != null ? x.u.Descripcion : null,
                    Direccion = x.lo != null ? x.lo.Direccion : null,
                    InteriorDepartamento = x.v.InteriorDepartamento,
                    VecinoColindanciaId = x.v.VecinoColindanciaId,
                    ColindanciaDescripcion = x.col.Descripcion,
                    VecinoTipoConstruccionId = x.v.VecinoTipoConstruccionId,
                    TipoConstruccionDescripcion = x.tc.Descripcion,
                    Observaciones = x.lo != null ? x.lo.Observaciones : null,
                    CreatedDateTime = x.v.CreatedDateTime
                })
                .ToListAsync();

            // Personas e imágenes de las casas de esta página (una sola consulta cada una).
            var pageIds = items.Select(i => i.VecinoId).ToList();
            var personasByVecino = await LoadPersonas(ctx, pageIds);
            var imagenesByVecino = await LoadImagenes(ctx, pageIds);
            foreach (var item in items)
            {
                item.Personas = personasByVecino.GetValueOrDefault(item.VecinoId, new());
                item.Imagenes = imagenesByVecino.GetValueOrDefault(item.VecinoId, new());
                var principal = item.Personas.FirstOrDefault();
                item.NombrePropietario = principal?.Nombre;
                item.Dni = principal?.Dni;
                item.Celular = principal?.Celular;
            }

            return new PagedResult<VecinoListItemDto>
            {
                Page = filter.Page,
                PageSize = PageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)PageSize),
                Data = items
            };
        }

        public async Task<VecinoListItemDto?> GetById(int vecinoId)
        {
            using var ctx = _factory.CreateDbContext();

            var item = await (
                from v in ctx.Vecino
                join p in ctx.Project on v.ProjectId equals p.ProjectId
                join lo in ctx.VecinoLote on v.VecinoLoteId equals lo.VecinoLoteId into loj
                from lo in loj.DefaultIfEmpty()
                join col in ctx.VecinoColindancia on v.VecinoColindanciaId equals col.VecinoColindanciaId
                join tc in ctx.VecinoTipoConstruccion on v.VecinoTipoConstruccionId equals tc.VecinoTipoConstruccionId
                join u in ctx.VecinoUso on v.VecinoUsoId equals u.VecinoUsoId into uj
                from u in uj.DefaultIfEmpty()
                where v.VecinoId == vecinoId && v.Active && v.State
                select new VecinoListItemDto
                {
                    VecinoId = v.VecinoId,
                    VecinoLoteId = v.VecinoLoteId,
                    ProjectId = v.ProjectId,
                    ProjectDescription = p.ProjectDescription,
                    Predio = v.Predio,
                    VecinoUsoId = v.VecinoUsoId,
                    UsoDescripcion = u != null ? u.Descripcion : null,
                    Direccion = lo != null ? lo.Direccion : null,
                    InteriorDepartamento = v.InteriorDepartamento,
                    VecinoColindanciaId = v.VecinoColindanciaId,
                    ColindanciaDescripcion = col.Descripcion,
                    VecinoTipoConstruccionId = v.VecinoTipoConstruccionId,
                    TipoConstruccionDescripcion = tc.Descripcion,
                    Observaciones = lo != null ? lo.Observaciones : null,
                    CreatedDateTime = v.CreatedDateTime
                }
            ).FirstOrDefaultAsync();

            if (item is null) return null;

            var personas = await LoadPersonas(ctx, new List<int> { vecinoId });
            var imagenes = await LoadImagenes(ctx, new List<int> { vecinoId });
            item.Personas = personas.GetValueOrDefault(vecinoId, new());
            item.Imagenes = imagenes.GetValueOrDefault(vecinoId, new());
            var principal = item.Personas.FirstOrDefault();
            item.NombrePropietario = principal?.Nombre;
            item.Dni = principal?.Dni;
            item.Celular = principal?.Celular;
            return item;
        }

        public async Task<VecinoLoteRegisterResultDto> RegisterVecinos(VecinoLoteRegisterDto dto, int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var now = DateTime.UtcNow;

            // El lote se define como un polígono del croquis. Resolvemos el polígono seleccionado.
            var poligono = await ctx.ProjectCroquisLote
                .FirstOrDefaultAsync(l => l.ProjectCroquisLoteId == dto.ProjectCroquisLoteId && l.State);
            if (poligono is null)
                throw new AbrilException("El lote seleccionado del croquis no existe.", 404);

            var projectId = await ctx.ProjectCroquis
                .Where(c => c.ProjectCroquisId == poligono.ProjectCroquisId)
                .Select(c => c.ProjectId)
                .FirstOrDefaultAsync();

            // Lote de negocio: reutilizar el existente o crearlo (perezoso) sobre el polígono.
            VecinoLote lote;
            if (poligono.VecinoLoteId.HasValue)
            {
                lote = await ctx.VecinoLote.FirstAsync(l => l.VecinoLoteId == poligono.VecinoLoteId.Value);
                lote.UpdatedDateTime = now;
                lote.UpdatedUserId = userId;
            }
            else
            {
                lote = new VecinoLote
                {
                    ProjectId = projectId,
                    CreatedDateTime = now,
                    CreatedUserId = userId,
                    Active = true,
                    State = true,
                };
                ctx.VecinoLote.Add(lote);
                poligono.Lote = lote; // EF fija la FK vecino_lote_id del polígono al guardar
                poligono.UpdatedDateTime = now;
                poligono.UpdatedUserId = userId;
            }

            // Dirección / observaciones a nivel de lote (upsert).
            lote.Direccion = dto.Direccion.Trim();
            lote.Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones.Trim();

            // Crear los vecinos/departamentos del lote (con sus personas).
            var vecinos = dto.Vecinos.Select(dep => new Vecino
            {
                ProjectId = projectId,
                Lote = lote, // nav; EF fija VecinoLoteId al guardar
                VecinoUsoId = dep.VecinoUsoId,
                InteriorDepartamento = dep.InteriorDepartamento?.Trim(),
                VecinoColindanciaId = dep.VecinoColindanciaId,
                VecinoTipoConstruccionId = dep.VecinoTipoConstruccionId,
                CreatedDateTime = now,
                CreatedUserId = userId,
                Active = true,
                State = true,
                Personas = dep.Personas.Select(per => new VecinoPersona
                {
                    Nombre = per.Nombre.Trim(),
                    Dni = string.IsNullOrWhiteSpace(per.Dni) ? null : per.Dni.Trim(),
                    Celular = string.IsNullOrWhiteSpace(per.Celular) ? null : per.Celular.Trim(),
                    VecinoRelacionTipoId = per.VecinoRelacionTipoId,
                    CreatedDateTime = now,
                    CreatedUserId = userId,
                    Active = true,
                    State = true
                }).ToList()
            }).ToList();

            ctx.Vecino.AddRange(vecinos);
            await ctx.SaveChangesAsync();

            return new VecinoLoteRegisterResultDto
            {
                VecinoLoteId = lote.VecinoLoteId,
                VecinoIds = vecinos.Select(v => v.VecinoId).ToList(),
            };
        }

        // ── Solicitudes ─────────────────────────────────────────────────────
        public async Task<bool> VecinoExists(int vecinoId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Vecino.AnyAsync(v => v.VecinoId == vecinoId && v.Active && v.State);
        }

        public async Task<VecinoSolicitudesResponseDto> GetSolicitudes(int vecinoId)
        {
            using var ctx = _factory.CreateDbContext();

            var solicitudes = await (
                from s in ctx.VecinoSolicitud
                join e in ctx.VecinoSolicitudEstado on s.VecinoSolicitudEstadoId equals e.VecinoSolicitudEstadoId
                where s.VecinoId == vecinoId && s.Active && s.State
                orderby s.VecinoSolicitudId descending
                select new VecinoSolicitudItemDto
                {
                    VecinoSolicitudId = s.VecinoSolicitudId,
                    VecinoId = s.VecinoId,
                    Descripcion = s.Descripcion,
                    EsCritica = s.EsCritica,
                    VecinoSolicitudEstadoId = s.VecinoSolicitudEstadoId,
                    EstadoDescripcion = e.Descripcion,
                    CreatedDateTime = s.CreatedDateTime
                }
            ).ToListAsync();

            var estados = await ctx.VecinoSolicitudEstado
                .Where(e => e.State && e.Active)
                .OrderBy(e => e.VecinoSolicitudEstadoId)
                .Select(e => new CatalogOptionDto { Id = e.VecinoSolicitudEstadoId, Descripcion = e.Descripcion })
                .ToListAsync();

            var compromisoEstados = await ctx.VecinoCompromisoEstado
                .Where(e => e.State && e.Active)
                .OrderBy(e => e.VecinoCompromisoEstadoId)
                .Select(e => new CatalogOptionDto { Id = e.VecinoCompromisoEstadoId, Descripcion = e.Descripcion })
                .ToListAsync();

            var entregableEstados = await ctx.VecinoEntregableEstado
                .Where(e => e.State && e.Active)
                .OrderBy(e => e.VecinoEntregableEstadoId)
                .Select(e => new CatalogOptionDto { Id = e.VecinoEntregableEstadoId, Descripcion = e.Descripcion })
                .ToListAsync();

            return new VecinoSolicitudesResponseDto
            {
                Solicitudes = solicitudes,
                Estados = estados,
                CompromisoEstados = compromisoEstados,
                EntregableEstados = entregableEstados
            };
        }

        public async Task<int> CreateSolicitud(int vecinoId, VecinoSolicitudCreateDto dto, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            // Toda solicitud nueva nace en estado "Por responder".
            var estadoPorResponder = await ctx.VecinoSolicitudEstado
                .Where(e => e.Descripcion == "Por responder" && e.State)
                .Select(e => e.VecinoSolicitudEstadoId)
                .FirstOrDefaultAsync();

            var solicitud = new VecinoSolicitud
            {
                VecinoId = vecinoId,
                Descripcion = dto.Descripcion.Trim(),
                EsCritica = dto.EsCritica,
                VecinoSolicitudEstadoId = estadoPorResponder,
                CreatedDateTime = DateTime.UtcNow,
                CreatedUserId = userId,
                Active = true,
                State = true
            };

            ctx.VecinoSolicitud.Add(solicitud);
            await ctx.SaveChangesAsync();
            return solicitud.VecinoSolicitudId;
        }

        public async Task<bool> UpdateSolicitudEstado(int solicitudId, int estadoId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var solicitud = await ctx.VecinoSolicitud
                .FirstOrDefaultAsync(s => s.VecinoSolicitudId == solicitudId && s.Active && s.State);
            if (solicitud is null) return false;

            var estadoValido = await ctx.VecinoSolicitudEstado
                .AnyAsync(e => e.VecinoSolicitudEstadoId == estadoId && e.State);
            if (!estadoValido) return false;

            solicitud.VecinoSolicitudEstadoId = estadoId;
            solicitud.UpdatedDateTime = DateTime.UtcNow;
            solicitud.UpdatedUserId = userId;
            await ctx.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateSolicitudDescripcion(int solicitudId, string descripcion, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var solicitud = await ctx.VecinoSolicitud
                .FirstOrDefaultAsync(s => s.VecinoSolicitudId == solicitudId && s.Active && s.State);
            if (solicitud is null) return false;

            solicitud.Descripcion = descripcion.Trim();
            solicitud.UpdatedDateTime = DateTime.UtcNow;
            solicitud.UpdatedUserId = userId;
            await ctx.SaveChangesAsync();
            return true;
        }

        // ── Compromisos ─────────────────────────────────────────────────────
        public async Task<bool> SolicitudExists(int solicitudId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.VecinoSolicitud.AnyAsync(s => s.VecinoSolicitudId == solicitudId && s.Active && s.State);
        }

        public async Task<List<VecinoCompromisoItemDto>> GetCompromisos(int solicitudId)
        {
            using var ctx = _factory.CreateDbContext();

            var compromisos = await (
                from c in ctx.VecinoCompromiso
                join e in ctx.VecinoCompromisoEstado on c.VecinoCompromisoEstadoId equals e.VecinoCompromisoEstadoId
                where c.VecinoSolicitudId == solicitudId && c.Active && c.State
                orderby c.VecinoCompromisoId descending
                select new VecinoCompromisoItemDto
                {
                    VecinoCompromisoId = c.VecinoCompromisoId,
                    VecinoSolicitudId = c.VecinoSolicitudId,
                    Descripcion = c.Descripcion,
                    EsCritico = c.EsCritico,
                    VecinoCompromisoEstadoId = c.VecinoCompromisoEstadoId,
                    EstadoDescripcion = e.Descripcion,
                    FechaInicio = c.FechaInicio,
                    FechaFin = c.FechaFin,
                    FechaFinMunicipalidad = c.FechaFinMunicipalidad,
                    Observaciones = c.Observaciones,
                    CreatedDateTime = c.CreatedDateTime
                }
            ).ToListAsync();

            if (compromisos.Count == 0) return compromisos;

            var ids = compromisos.Select(c => c.VecinoCompromisoId).ToList();

            var entregables = await (
                from en in ctx.VecinoCompromisoEntregable
                join t in ctx.VecinoEntregableTipo on en.VecinoEntregableTipoId equals t.VecinoEntregableTipoId
                join es in ctx.VecinoEntregableEstado on en.VecinoEntregableEstadoId equals es.VecinoEntregableEstadoId
                where ids.Contains(en.VecinoCompromisoId) && en.Active && en.State
                orderby t.Orden
                select new
                {
                    en.VecinoCompromisoId,
                    Item = new VecinoEntregableItemDto
                    {
                        VecinoCompromisoEntregableId = en.VecinoCompromisoEntregableId,
                        VecinoEntregableTipoId = en.VecinoEntregableTipoId,
                        TipoDescripcion = t.Descripcion,
                        Orden = t.Orden,
                        VecinoEntregableEstadoId = en.VecinoEntregableEstadoId,
                        EstadoDescripcion = es.Descripcion,
                        ArchivoUrl = en.ArchivoUrl,
                        OriginalFileName = en.OriginalFileName
                    }
                }
            ).ToListAsync();

            var byCompromiso = entregables
                .GroupBy(x => x.VecinoCompromisoId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Item).ToList());

            foreach (var c in compromisos)
                c.Entregables = byCompromiso.GetValueOrDefault(c.VecinoCompromisoId, new());

            // Normativas (varios archivos por compromiso).
            var normativas = await (
                from n in ctx.VecinoCompromisoNormativa
                where ids.Contains(n.VecinoCompromisoId) && n.Active && n.State
                orderby n.VecinoCompromisoNormativaId
                select new
                {
                    n.VecinoCompromisoId,
                    Dto = new VecinoNormativaDto
                    {
                        VecinoCompromisoNormativaId = n.VecinoCompromisoNormativaId,
                        ArchivoUrl = n.ArchivoUrl,
                        OriginalFileName = n.OriginalFileName
                    }
                }
            ).ToListAsync();

            var normativasByCompromiso = normativas
                .GroupBy(x => x.VecinoCompromisoId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Dto).ToList());

            foreach (var c in compromisos)
                c.Normativas = normativasByCompromiso.GetValueOrDefault(c.VecinoCompromisoId, new());

            return compromisos;
        }

        public async Task<int> CreateCompromiso(int solicitudId, VecinoCompromisoCreateDto dto, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var estadoId = dto.VecinoCompromisoEstadoId ?? await ctx.VecinoCompromisoEstado
                .Where(e => e.Descripcion == "Pendiente" && e.State)
                .Select(e => e.VecinoCompromisoEstadoId)
                .FirstOrDefaultAsync();

            var compromiso = new VecinoCompromiso
            {
                VecinoSolicitudId = solicitudId,
                Descripcion = dto.Descripcion.Trim(),
                EsCritico = dto.EsCritico,
                VecinoCompromisoEstadoId = estadoId,
                FechaInicio = dto.FechaInicio,
                FechaFin = dto.FechaFin,
                FechaFinMunicipalidad = dto.FechaFinMunicipalidad,
                Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones.Trim(),
                CreatedDateTime = DateTime.UtcNow,
                CreatedUserId = userId,
                Active = true,
                State = true
            };

            ctx.VecinoCompromiso.Add(compromiso);
            await ctx.SaveChangesAsync();

            // Crear los entregables (un registro por tipo) con estado por defecto "Falta".
            var estadoFalta = await ctx.VecinoEntregableEstado
                .Where(e => e.Descripcion == "Falta" && e.State)
                .Select(e => e.VecinoEntregableEstadoId)
                .FirstOrDefaultAsync();

            var tipos = await ctx.VecinoEntregableTipo
                .Where(t => t.State && t.Active)
                .OrderBy(t => t.Orden)
                .Select(t => t.VecinoEntregableTipoId)
                .ToListAsync();

            foreach (var tipoId in tipos)
            {
                ctx.VecinoCompromisoEntregable.Add(new VecinoCompromisoEntregable
                {
                    VecinoCompromisoId = compromiso.VecinoCompromisoId,
                    VecinoEntregableTipoId = tipoId,
                    VecinoEntregableEstadoId = estadoFalta,
                    CreatedDateTime = DateTime.UtcNow,
                    CreatedUserId = userId,
                    Active = true,
                    State = true
                });
            }

            await ctx.SaveChangesAsync();
            return compromiso.VecinoCompromisoId;
        }

        public async Task<bool> UpdateCompromisoEstado(int compromisoId, int estadoId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var compromiso = await ctx.VecinoCompromiso
                .FirstOrDefaultAsync(c => c.VecinoCompromisoId == compromisoId && c.Active && c.State);
            if (compromiso is null) return false;

            var valido = await ctx.VecinoCompromisoEstado.AnyAsync(e => e.VecinoCompromisoEstadoId == estadoId && e.State);
            if (!valido) return false;

            compromiso.VecinoCompromisoEstadoId = estadoId;
            compromiso.UpdatedDateTime = DateTime.UtcNow;
            compromiso.UpdatedUserId = userId;
            await ctx.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateEntregableEstado(int entregableId, int estadoId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var entregable = await ctx.VecinoCompromisoEntregable
                .FirstOrDefaultAsync(e => e.VecinoCompromisoEntregableId == entregableId && e.Active && e.State);
            if (entregable is null) return false;

            var nuevoEstado = await ctx.VecinoEntregableEstado
                .FirstOrDefaultAsync(e => e.VecinoEntregableEstadoId == estadoId && e.State);
            if (nuevoEstado is null) return false;

            // "Acta de Compromiso" es obligatoria: no se le puede asignar el estado "No aplica".
            if (nuevoEstado.Descripcion == "No aplica")
            {
                var tipoDescripcion = await ctx.VecinoEntregableTipo
                    .Where(t => t.VecinoEntregableTipoId == entregable.VecinoEntregableTipoId)
                    .Select(t => t.Descripcion)
                    .FirstOrDefaultAsync();
                if (tipoDescripcion == "Acta de Compromiso")
                    throw new AbrilException("El entregable 'Acta de Compromiso' es obligatorio y no puede marcarse como 'No aplica'.", 422);
            }

            entregable.VecinoEntregableEstadoId = estadoId;
            entregable.UpdatedDateTime = DateTime.UtcNow;
            entregable.UpdatedUserId = userId;
            await ctx.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCompromisoObservaciones(int compromisoId, string? observaciones, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var compromiso = await ctx.VecinoCompromiso
                .FirstOrDefaultAsync(c => c.VecinoCompromisoId == compromisoId && c.Active && c.State);
            if (compromiso is null) return false;

            compromiso.Observaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim();
            compromiso.UpdatedDateTime = DateTime.UtcNow;
            compromiso.UpdatedUserId = userId;
            await ctx.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCompromisoFechaMunicipalidad(int compromisoId, DateOnly? fechaFinMunicipalidad, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var compromiso = await ctx.VecinoCompromiso
                .FirstOrDefaultAsync(c => c.VecinoCompromisoId == compromisoId && c.Active && c.State);
            if (compromiso is null) return false;

            // La fecha límite por municipalidad/fiscalización debe ser anterior (o igual) a la fecha fin del compromiso.
            if (fechaFinMunicipalidad.HasValue && compromiso.FechaFin.HasValue && fechaFinMunicipalidad.Value > compromiso.FechaFin.Value)
                throw new AbrilException("La fecha límite por municipalidad/fiscalización no puede ser posterior a la fecha fin del compromiso.", 422);

            compromiso.FechaFinMunicipalidad = fechaFinMunicipalidad;
            compromiso.UpdatedDateTime = DateTime.UtcNow;
            compromiso.UpdatedUserId = userId;
            await ctx.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UploadEntregable(int entregableId, string archivoUrl, string? originalFileName, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var entregable = await ctx.VecinoCompromisoEntregable
                .FirstOrDefaultAsync(e => e.VecinoCompromisoEntregableId == entregableId && e.Active && e.State);
            if (entregable is null) return false;

            entregable.ArchivoUrl = archivoUrl;
            entregable.OriginalFileName = originalFileName;
            entregable.UpdatedDateTime = DateTime.UtcNow;
            entregable.UpdatedUserId = userId;
            await ctx.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CompromisoExists(int compromisoId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.VecinoCompromiso.AnyAsync(c => c.VecinoCompromisoId == compromisoId && c.Active && c.State);
        }

        public async Task<List<VecinoNormativaDto>> AddNormativas(int compromisoId, List<(string ArchivoUrl, string? OriginalFileName)> archivos, int userId)
        {
            if (archivos.Count == 0) return new();
            using var ctx = _factory.CreateDbContext();

            var now = DateTime.UtcNow;
            var entities = archivos.Select(a => new VecinoCompromisoNormativa
            {
                VecinoCompromisoId = compromisoId,
                ArchivoUrl = a.ArchivoUrl,
                OriginalFileName = a.OriginalFileName,
                CreatedDateTime = now,
                CreatedUserId = userId,
                Active = true,
                State = true
            }).ToList();

            ctx.VecinoCompromisoNormativa.AddRange(entities);
            await ctx.SaveChangesAsync();

            return entities.Select(e => new VecinoNormativaDto
            {
                VecinoCompromisoNormativaId = e.VecinoCompromisoNormativaId,
                ArchivoUrl = e.ArchivoUrl,
                OriginalFileName = e.OriginalFileName
            }).ToList();
        }

        public async Task<bool> DeleteNormativa(int normativaId, int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var row = await ctx.VecinoCompromisoNormativa.FirstOrDefaultAsync(n => n.VecinoCompromisoNormativaId == normativaId && n.State);
            if (row is null) return false;

            row.State = false;
            row.UpdatedDateTime = DateTime.UtcNow;
            row.UpdatedUserId = userId;
            await ctx.SaveChangesAsync();
            return true;
        }

        // ── Calendario de limpiezas ────────────────────────────────────────────
        public async Task<VecinoLimpiezasResponseDto> GetLimpiezas(int projectId, int year, int month)
        {
            using var ctx = _factory.CreateDbContext();

            var desde = new DateOnly(year, month, 1);
            var hasta = desde.AddMonths(1);

            var rows = await (
                from l in ctx.VecinoLimpieza.Where(l => l.ProjectId == projectId && l.State && l.Fecha >= desde && l.Fecha < hasta)
                join t in ctx.VecinoLimpiezaTipo on l.VecinoLimpiezaTipoId equals t.VecinoLimpiezaTipoId
                join v in ctx.Vecino on l.VecinoId equals v.VecinoId into vj
                from v in vj.DefaultIfEmpty()
                join lo in ctx.VecinoLote on v.VecinoLoteId equals lo.VecinoLoteId into loj
                from lo in loj.DefaultIfEmpty()
                orderby l.Fecha, l.VecinoLimpiezaId
                select new VecinoLimpiezaDto
                {
                    VecinoLimpiezaId = l.VecinoLimpiezaId,
                    Fecha = l.Fecha,
                    VecinoLimpiezaTipoId = l.VecinoLimpiezaTipoId,
                    TipoDescripcion = t.Descripcion,
                    VecinoId = l.VecinoId,
                    VecinoDireccion = lo != null ? lo.Direccion : null,
                    VecinoInterior = v != null ? v.InteriorDepartamento : null,
                    Descripcion = l.Descripcion,
                    AtencionArchivoUrl = l.AtencionArchivoUrl,
                    AtencionOriginalFileName = l.AtencionOriginalFileName,
                    AtencionVecinoCompromisoId = l.AtencionVecinoCompromisoId,
                }
            ).ToListAsync();

            // Etiqueta del compromiso asociado a la atención (si tiene).
            var compromisoIds = rows.Where(r => r.AtencionVecinoCompromisoId.HasValue)
                .Select(r => r.AtencionVecinoCompromisoId!.Value).Distinct().ToList();
            if (compromisoIds.Count > 0)
            {
                var labels = await (
                    from c in ctx.VecinoCompromiso.Where(c => compromisoIds.Contains(c.VecinoCompromisoId))
                    join s in ctx.VecinoSolicitud on c.VecinoSolicitudId equals s.VecinoSolicitudId
                    select new { c.VecinoCompromisoId, Solicitud = s.Descripcion, Compromiso = c.Descripcion }
                ).ToListAsync();
                var labelById = labels.ToDictionary(x => x.VecinoCompromisoId, x => $"{x.Solicitud} — {x.Compromiso}");
                foreach (var r in rows)
                    if (r.AtencionVecinoCompromisoId.HasValue)
                        r.AtencionCompromisoLabel = labelById.GetValueOrDefault(r.AtencionVecinoCompromisoId.Value);
            }

            // Nombre del propietario (primera persona) de los vecinos involucrados, en una consulta.
            var vecinoIds = rows.Where(r => r.VecinoId.HasValue).Select(r => r.VecinoId!.Value).Distinct().ToList();
            if (vecinoIds.Count > 0)
            {
                var nombres = await (
                    from per in ctx.VecinoPersona.Where(p => vecinoIds.Contains(p.VecinoId) && p.Active && p.State)
                    orderby per.VecinoRelacionTipoId, per.VecinoPersonaId
                    select new { per.VecinoId, per.Nombre }
                ).ToListAsync();
                var nombreByVecino = nombres.GroupBy(x => x.VecinoId).ToDictionary(g => g.Key, g => g.First().Nombre);
                foreach (var r in rows)
                    if (r.VecinoId.HasValue) r.VecinoNombre = nombreByVecino.GetValueOrDefault(r.VecinoId.Value);
            }

            var tipos = await ctx.VecinoLimpiezaTipo
                .Where(t => t.State && t.Active)
                .OrderBy(t => t.VecinoLimpiezaTipoId)
                .Select(t => new CatalogOptionDto { Id = t.VecinoLimpiezaTipoId, Descripcion = t.Descripcion })
                .ToListAsync();

            return new VecinoLimpiezasResponseDto { Limpiezas = rows, Tipos = tipos };
        }

        public async Task<VecinoLimpiezaDto> CreateLimpieza(int projectId, VecinoLimpiezaCreateDto dto, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            if (!await ctx.Project.AnyAsync(p => p.ProjectId == projectId && p.State))
                throw new AbrilException("El proyecto no existe.", 404);

            var tipo = await ctx.VecinoLimpiezaTipo.FirstOrDefaultAsync(t => t.VecinoLimpiezaTipoId == dto.VecinoLimpiezaTipoId && t.State && t.Active);
            if (tipo is null)
                throw new AbrilException("El tipo de limpieza no existe.", 404);

            int? vecinoId = null;
            string? vecinoNombre = null;
            string? vecinoDireccion = null;
            string? vecinoInterior = null;

            if (tipo.Descripcion == "Departamento")
            {
                if (!dto.VecinoId.HasValue)
                    throw new AbrilException("Debe seleccionar el vecino del departamento a limpiar.", 400);

                var vecino = await ctx.Vecino.FirstOrDefaultAsync(v => v.VecinoId == dto.VecinoId.Value && v.State && v.ProjectId == projectId);
                if (vecino is null)
                    throw new AbrilException("El vecino no pertenece a este proyecto.", 422);

                vecinoId = vecino.VecinoId;
                vecinoInterior = vecino.InteriorDepartamento;
                vecinoDireccion = await ctx.VecinoLote
                    .Where(lo => lo.VecinoLoteId == vecino.VecinoLoteId)
                    .Select(lo => lo.Direccion)
                    .FirstOrDefaultAsync();
                vecinoNombre = await (
                    from per in ctx.VecinoPersona.Where(p => p.VecinoId == vecino.VecinoId && p.Active && p.State)
                    orderby per.VecinoRelacionTipoId, per.VecinoPersonaId
                    select per.Nombre
                ).FirstOrDefaultAsync();
            }

            var entity = new VecinoLimpieza
            {
                ProjectId = projectId,
                VecinoLimpiezaTipoId = tipo.VecinoLimpiezaTipoId,
                VecinoId = vecinoId,
                Fecha = dto.Fecha,
                Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
                CreatedDateTime = DateTime.UtcNow,
                CreatedUserId = userId,
                Active = true,
                State = true,
            };

            ctx.VecinoLimpieza.Add(entity);
            await ctx.SaveChangesAsync();

            return new VecinoLimpiezaDto
            {
                VecinoLimpiezaId = entity.VecinoLimpiezaId,
                Fecha = entity.Fecha,
                VecinoLimpiezaTipoId = tipo.VecinoLimpiezaTipoId,
                TipoDescripcion = tipo.Descripcion,
                VecinoId = vecinoId,
                VecinoNombre = vecinoNombre,
                VecinoDireccion = vecinoDireccion,
                VecinoInterior = vecinoInterior,
                Descripcion = entity.Descripcion,
            };
        }

        public async Task<bool> DeleteLimpieza(int limpiezaId, int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var row = await ctx.VecinoLimpieza.FirstOrDefaultAsync(l => l.VecinoLimpiezaId == limpiezaId && l.State);
            if (row is null) return false;

            row.State = false;
            row.UpdatedDateTime = DateTime.UtcNow;
            row.UpdatedUserId = userId;
            await ctx.SaveChangesAsync();
            return true;
        }

        public async Task<VecinoLimpiezaCumplimientoDto> GetCumplimiento(int projectId)
        {
            using var ctx = _factory.CreateDbContext();
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

            var rows = await (
                from l in ctx.VecinoLimpieza.Where(l => l.ProjectId == projectId && l.State && l.Fecha <= hoy)
                join t in ctx.VecinoLimpiezaTipo on l.VecinoLimpiezaTipoId equals t.VecinoLimpiezaTipoId
                select new { t.Descripcion, Hecha = l.AtencionArchivoUrl != null }
            ).ToListAsync();

            var depto = rows.Where(r => r.Descripcion == "Departamento").ToList();
            var comun = rows.Where(r => r.Descripcion != "Departamento").ToList();

            return new VecinoLimpiezaCumplimientoDto
            {
                DepartamentoProgramadas = depto.Count,
                DepartamentoHechas = depto.Count(r => r.Hecha),
                ComunProgramadas = comun.Count,
                ComunHechas = comun.Count(r => r.Hecha),
                TotalProgramadas = rows.Count,
                TotalHechas = rows.Count(r => r.Hecha),
            };
        }

        public async Task<List<VecinoCompromisoSelectDto>> GetCompromisosSelect(int vecinoId)
        {
            using var ctx = _factory.CreateDbContext();
            return await (
                from c in ctx.VecinoCompromiso.Where(c => c.Active && c.State)
                join s in ctx.VecinoSolicitud.Where(s => s.VecinoId == vecinoId && s.State) on c.VecinoSolicitudId equals s.VecinoSolicitudId
                orderby c.VecinoCompromisoId descending
                select new VecinoCompromisoSelectDto
                {
                    VecinoCompromisoId = c.VecinoCompromisoId,
                    Label = s.Descripcion + " — " + c.Descripcion,
                }
            ).ToListAsync();
        }

        /// <summary>Guarda la atención de limpieza (archivo + compromiso opcional). Valida que la fecha no sea futura.</summary>
        public async Task<bool> UploadAtencion(int limpiezaId, string archivoUrl, string? originalFileName, int? vecinoCompromisoId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var limpieza = await ctx.VecinoLimpieza.FirstOrDefaultAsync(l => l.VecinoLimpiezaId == limpiezaId && l.State);
            if (limpieza is null) return false;

            if (limpieza.Fecha > DateOnly.FromDateTime(DateTime.UtcNow))
                throw new AbrilException("No se puede registrar la atención de una limpieza en una fecha futura.", 422);

            var tipoDesc = await ctx.VecinoLimpiezaTipo
                .Where(t => t.VecinoLimpiezaTipoId == limpieza.VecinoLimpiezaTipoId)
                .Select(t => t.Descripcion)
                .FirstOrDefaultAsync();

            if (tipoDesc == "Departamento")
            {
                if (!vecinoCompromisoId.HasValue)
                    throw new AbrilException("Debe relacionar la atención con un compromiso de una solicitud.", 400);

                // El compromiso debe pertenecer a una solicitud del vecino de esta limpieza.
                var ok = await (
                    from c in ctx.VecinoCompromiso.Where(c => c.VecinoCompromisoId == vecinoCompromisoId.Value && c.State)
                    join s in ctx.VecinoSolicitud on c.VecinoSolicitudId equals s.VecinoSolicitudId
                    where s.VecinoId == limpieza.VecinoId
                    select c.VecinoCompromisoId
                ).AnyAsync();
                if (!ok)
                    throw new AbrilException("El compromiso seleccionado no pertenece a este vecino.", 422);

                limpieza.AtencionVecinoCompromisoId = vecinoCompromisoId.Value;
            }
            else
            {
                limpieza.AtencionVecinoCompromisoId = null;
            }

            limpieza.AtencionArchivoUrl = archivoUrl;
            limpieza.AtencionOriginalFileName = originalFileName;
            limpieza.UpdatedDateTime = DateTime.UtcNow;
            limpieza.UpdatedUserId = userId;
            await ctx.SaveChangesAsync();
            return true;
        }

        // ── Dashboard ────────────────────────────────────────────────────────
        public async Task<VecinosDashboardDto> GetDashboard()
        {
            using var ctx = _factory.CreateDbContext();

            // Catálogos de estado (definen las "porciones" de cada donut, incluso si quedan en 0).
            var solicitudEstados = await ctx.VecinoSolicitudEstado
                .Where(e => e.State && e.Active)
                .OrderBy(e => e.VecinoSolicitudEstadoId)
                .Select(e => new CatalogOptionDto { Id = e.VecinoSolicitudEstadoId, Descripcion = e.Descripcion })
                .ToListAsync();

            var compromisoEstados = await ctx.VecinoCompromisoEstado
                .Where(e => e.State && e.Active)
                .OrderBy(e => e.VecinoCompromisoEstadoId)
                .Select(e => new CatalogOptionDto { Id = e.VecinoCompromisoEstadoId, Descripcion = e.Descripcion })
                .ToListAsync();

            // Todos los proyectos activos (aparecen aunque no tengan vecinos aún).
            var projects = await ctx.Project
                .Where(p => p.State && p.Active && !ctx.ProyectoFiltro.Any(f => f.ProjectId == p.ProjectId && f.FuncionalidadId == ProyectoFiltroFuncionalidades.VecinosGestion && !f.Active))
                .OrderBy(p => p.ProjectDescription)
                .Select(p => new { p.ProjectId, p.ProjectDescription })
                .ToListAsync();

            // Conteos agregados (una consulta por métrica para evitar N+1).
            var vecinoCounts = await ctx.Vecino
                .Where(v => v.Active && v.State)
                .GroupBy(v => v.ProjectId)
                .Select(g => new { ProjectId = g.Key, Count = g.Count() })
                .ToListAsync();

            var loteCounts = await ctx.VecinoLote
                .Where(l => l.Active && l.State)
                .GroupBy(l => l.ProjectId)
                .Select(g => new { ProjectId = g.Key, Count = g.Count() })
                .ToListAsync();

            var solicitudCounts = await (
                from s in ctx.VecinoSolicitud
                join v in ctx.Vecino on s.VecinoId equals v.VecinoId
                where s.Active && s.State && v.Active && v.State
                group s by new { v.ProjectId, s.VecinoSolicitudEstadoId } into g
                select new { g.Key.ProjectId, EstadoId = g.Key.VecinoSolicitudEstadoId, Count = g.Count() }
            ).ToListAsync();

            var compromisoCounts = await (
                from c in ctx.VecinoCompromiso
                join s in ctx.VecinoSolicitud on c.VecinoSolicitudId equals s.VecinoSolicitudId
                join v in ctx.Vecino on s.VecinoId equals v.VecinoId
                where c.Active && c.State && s.Active && s.State && v.Active && v.State
                group c by new { v.ProjectId, c.VecinoCompromisoEstadoId } into g
                select new { g.Key.ProjectId, EstadoId = g.Key.VecinoCompromisoEstadoId, Count = g.Count() }
            ).ToListAsync();

            // Cumplimiento de limpiezas por proyecto (programadas hasta hoy vs. hechas).
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
            var limpiezaCounts = await (
                from l in ctx.VecinoLimpieza.Where(l => l.State && l.Fecha <= hoy)
                join t in ctx.VecinoLimpiezaTipo on l.VecinoLimpiezaTipoId equals t.VecinoLimpiezaTipoId
                group new { t.Descripcion, Hecha = l.AtencionArchivoUrl != null } by l.ProjectId into g
                select new
                {
                    ProjectId = g.Key,
                    DeptoProg = g.Count(x => x.Descripcion == "Departamento"),
                    DeptoHechas = g.Count(x => x.Descripcion == "Departamento" && x.Hecha),
                    ComunProg = g.Count(x => x.Descripcion != "Departamento"),
                    ComunHechas = g.Count(x => x.Descripcion != "Departamento" && x.Hecha),
                }
            ).ToListAsync();

            VecinoLimpiezaCumplimientoDto LimpiezasFor(int? projectId)
            {
                var src = limpiezaCounts.Where(x => projectId == null || x.ProjectId == projectId).ToList();
                var deptoProg = src.Sum(x => x.DeptoProg);
                var deptoHechas = src.Sum(x => x.DeptoHechas);
                var comunProg = src.Sum(x => x.ComunProg);
                var comunHechas = src.Sum(x => x.ComunHechas);
                return new VecinoLimpiezaCumplimientoDto
                {
                    DepartamentoProgramadas = deptoProg,
                    DepartamentoHechas = deptoHechas,
                    ComunProgramadas = comunProg,
                    ComunHechas = comunHechas,
                    TotalProgramadas = deptoProg + comunProg,
                    TotalHechas = deptoHechas + comunHechas,
                };
            }

            List<VecinosDashboardEstadoDto> SolicitudesFor(int? projectId) => solicitudEstados
                .Select(e => new VecinosDashboardEstadoDto
                {
                    EstadoId = e.Id,
                    Descripcion = e.Descripcion,
                    Count = solicitudCounts
                        .Where(x => x.EstadoId == e.Id && (projectId == null || x.ProjectId == projectId))
                        .Sum(x => x.Count)
                })
                .ToList();

            List<VecinosDashboardEstadoDto> CompromisosFor(int? projectId) => compromisoEstados
                .Select(e => new VecinosDashboardEstadoDto
                {
                    EstadoId = e.Id,
                    Descripcion = e.Descripcion,
                    Count = compromisoCounts
                        .Where(x => x.EstadoId == e.Id && (projectId == null || x.ProjectId == projectId))
                        .Sum(x => x.Count)
                })
                .ToList();

            var proyectos = projects.Select(p => new VecinosDashboardProjectDto
            {
                ProjectId = p.ProjectId,
                ProjectDescription = p.ProjectDescription,
                LotesCount = loteCounts.FirstOrDefault(x => x.ProjectId == p.ProjectId)?.Count ?? 0,
                VecinosCount = vecinoCounts.FirstOrDefault(x => x.ProjectId == p.ProjectId)?.Count ?? 0,
                Solicitudes = SolicitudesFor(p.ProjectId),
                Compromisos = CompromisosFor(p.ProjectId),
                Limpiezas = LimpiezasFor(p.ProjectId)
            }).ToList();

            var resumen = new VecinosDashboardProjectDto
            {
                ProjectId = 0,
                ProjectDescription = "Resumen general",
                LotesCount = loteCounts.Sum(x => x.Count),
                VecinosCount = vecinoCounts.Sum(x => x.Count),
                Solicitudes = SolicitudesFor(null),
                Compromisos = CompromisosFor(null),
                Limpiezas = LimpiezasFor(null)
            };

            return new VecinosDashboardDto { Resumen = resumen, Proyectos = proyectos };
        }

        // ── Requisitos ───────────────────────────────────────────────────────
        public async Task<VecinoRequisitosResponseDto> GetRequisitos(int vecinoId)
        {
            using var ctx = _factory.CreateDbContext();

            var tipos = await ctx.VecinoRequisitoTipo
                .Where(t => t.State && t.Active)
                .OrderBy(t => t.Orden)
                .ToListAsync();

            var estados = await ctx.VecinoRequisitoEstado
                .Where(e => e.State && e.Active)
                .OrderBy(e => e.VecinoRequisitoEstadoId)
                .ToListAsync();

            var rows = await ctx.VecinoRequisito
                .Where(r => r.VecinoId == vecinoId && r.State)
                .ToListAsync();

            var estadoDesc = estados.ToDictionary(e => e.VecinoRequisitoEstadoId, e => e.Descripcion);
            int noSubidoId = estados.FirstOrDefault(e => e.Descripcion == "No subido")?.VecinoRequisitoEstadoId ?? 0;

            var items = tipos.Select(t =>
            {
                var row = rows.FirstOrDefault(r => r.VecinoRequisitoTipoId == t.VecinoRequisitoTipoId);
                return new VecinoRequisitoItemDto
                {
                    VecinoRequisitoId = row?.VecinoRequisitoId,
                    VecinoRequisitoTipoId = t.VecinoRequisitoTipoId,
                    TipoDescripcion = t.Descripcion,
                    Orden = t.Orden,
                    VecinoRequisitoEstadoId = row?.VecinoRequisitoEstadoId ?? noSubidoId,
                    EstadoDescripcion = row != null ? estadoDesc[row.VecinoRequisitoEstadoId] : "No subido",
                    ArchivoUrl = row?.ArchivoUrl,
                    OriginalFileName = row?.OriginalFileName,
                };
            }).ToList();

            return new VecinoRequisitosResponseDto
            {
                Requisitos = items,
                Estados = estados.Select(e => new CatalogOptionDto { Id = e.VecinoRequisitoEstadoId, Descripcion = e.Descripcion }).ToList(),
            };
        }

        public async Task<bool> TipoRequisitoExists(int tipoId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.VecinoRequisitoTipo.AnyAsync(t => t.VecinoRequisitoTipoId == tipoId && t.State && t.Active);
        }

        /// <summary>Obtiene la fila activa del requisito (vecino + tipo) o crea una nueva en el contexto.</summary>
        private static async Task<VecinoRequisito> GetOrCreateRow(AppDbContext ctx, int vecinoId, int tipoId, int userId)
        {
            var row = await ctx.VecinoRequisito
                .FirstOrDefaultAsync(r => r.VecinoId == vecinoId && r.VecinoRequisitoTipoId == tipoId && r.State);

            if (row == null)
            {
                row = new VecinoRequisito
                {
                    VecinoId = vecinoId,
                    VecinoRequisitoTipoId = tipoId,
                    CreatedDateTime = DateTime.UtcNow,
                    CreatedUserId = userId,
                    Active = true,
                    State = true,
                };
                ctx.VecinoRequisito.Add(row);
            }
            else
            {
                row.UpdatedDateTime = DateTime.UtcNow;
                row.UpdatedUserId = userId;
            }
            return row;
        }

        public async Task UploadRequisito(int vecinoId, int tipoId, string archivoUrl, string? originalFileName, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var subidoId = await ctx.VecinoRequisitoEstado
                .Where(e => e.Descripcion == "Subido" && e.State)
                .Select(e => e.VecinoRequisitoEstadoId)
                .FirstOrDefaultAsync();

            var row = await GetOrCreateRow(ctx, vecinoId, tipoId, userId);
            row.ArchivoUrl = archivoUrl;
            row.OriginalFileName = originalFileName;
            row.VecinoRequisitoEstadoId = subidoId; // subir archivo ⇒ Subido (sobrescribe "No aplica").

            await ctx.SaveChangesAsync();
        }

        /// <summary>
        /// Marca/desmarca "No aplica". Al desmarcar, el estado vuelve a derivarse automáticamente
        /// según haya o no archivo (Subido / No subido). El usuario nunca fija Subido/No subido a mano.
        /// </summary>
        public async Task SetRequisitoNoAplica(int vecinoId, int tipoId, bool noAplica, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var estados = await ctx.VecinoRequisitoEstado
                .Where(e => e.State)
                .Select(e => new { e.VecinoRequisitoEstadoId, e.Descripcion })
                .ToListAsync();
            int noAplicaId = estados.First(e => e.Descripcion == "No aplica").VecinoRequisitoEstadoId;
            int subidoId = estados.First(e => e.Descripcion == "Subido").VecinoRequisitoEstadoId;
            int noSubidoId = estados.First(e => e.Descripcion == "No subido").VecinoRequisitoEstadoId;

            var row = await GetOrCreateRow(ctx, vecinoId, tipoId, userId);
            row.VecinoRequisitoEstadoId = noAplica
                ? noAplicaId
                : (string.IsNullOrEmpty(row.ArchivoUrl) ? noSubidoId : subidoId);

            await ctx.SaveChangesAsync();
        }
    }
}

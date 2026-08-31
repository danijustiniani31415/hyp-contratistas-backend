using Abril_Backend.Shared.Constants;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Dtos.HabEmpresa;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Trabajadores;
using Abril_Backend.Features.Habilitacion.Infrastructure.Helpers;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Repositories
{
    public class HabEmpresaRepository : IHabEmpresaRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public HabEmpresaRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<EmpresaEntregableDto>> GetEntregablesEmpresaAsync(
            int empresaId, int proyectoId, int? mes, int? anio)
        {
            using var ctx = _factory.CreateDbContext();

            var registros = await ctx.SsHabEmpresa
                .Include(h => h.Item)
                .Where(h => h.EmpresaId == empresaId && h.ProyectoId == proyectoId)
                .ToListAsync();

            if (registros.Count == 0) return [];

            var items = await ctx.SsItemEmpresa
                .Where(i => i.Activo)
                .OrderBy(i => i.Orden)
                .ToListAsync();

            // Batch: versión más reciente (Enviado=true) + sus archivos para todos los entregables
            var habEmpresaIds = registros.Select(r => r.Id).ToList();
            var versionesConArchivos = await ctx.SsHabDocumentoVersion
                .Include(v => v.Archivos)
                .Where(v => v.HabEmpresaId.HasValue
                         && habEmpresaIds.Contains(v.HabEmpresaId.Value)
                         && v.Enviado)
                .ToListAsync();

            var archivosPorEntregable = versionesConArchivos
                .Where(v => v.HabEmpresaId.HasValue)
                .GroupBy(v => v.HabEmpresaId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(v => v.Version)
                          .SelectMany(v => v.Archivos)
                          .GroupBy(a => a.Id)
                          .Select(grp => grp.First())
                          .OrderBy(a => a.Orden)
                          .Select(a => new EntregableMesArchivoDto
                          {
                              Id = a.Id,
                              NombreArchivo = a.NombreArchivo ?? "",
                              ArchivoUrl = a.ArchivoUrl,
                              EsZip = a.EsZip,
                              Orden = a.Orden
                          })
                          .ToList()
                );

            var result = new List<EmpresaEntregableDto>();

            foreach (var item in items)
            {
                var regsItem = registros.Where(r => r.ItemId == item.Id).ToList();

                if (!item.EsMensual)
                {
                    var reg = regsItem.FirstOrDefault();
                    if (reg == null) continue;
                    result.Add(MapToDto(reg, item, [],
                        archivosPorEntregable.TryGetValue(reg.Id, out var arch) ? arch : []));
                }
                else
                {
                    if (regsItem.Count == 0) continue;

                    var meses = regsItem
                        .Where(r => r.Mes.HasValue && r.Anio.HasValue)
                        .OrderByDescending(r => r.Anio)
                        .ThenByDescending(r => r.Mes)
                        .Select(r =>
                        {
                            var baseId = registros
                                .Where(s => s.ItemId == r.ItemId && s.Mes == null && s.Anio == null)
                                .Select(s => (int?)s.Id)
                                .FirstOrDefault();

                            return new EntregableMesDto
                            {
                                Id = r.Id,
                                Mes = r.Mes ?? 0,
                                Anio = r.Anio ?? 0,
                                Estado = r.Estado,
                                Vigencia = r.Vigencia,
                                ArchivoUrl = r.ArchivoUrl,
                                ObsAbril = r.ObsAbril,
                                ObsContratista = r.ObsContratista,
                                MotivoRechazo = r.MotivoRechazo,
                                Archivos = archivosPorEntregable.TryGetValue(r.Id, out var arch) && arch.Count > 0
                                    ? arch
                                    : (baseId.HasValue && archivosPorEntregable.TryGetValue(baseId.Value, out var baseArch)
                                        ? baseArch
                                        : [])
                            };
                        })
                        .ToList();

                    var regReciente = regsItem.OrderByDescending(r => r.Anio).ThenByDescending(r => r.Mes).First();
                    var estadoGlobal = regReciente.Estado;

                    result.Add(new EmpresaEntregableDto
                    {
                        Id = regReciente.Id,
                        ItemId = item.Id,
                        NombreItem = item.Nombre,
                        Estado = estadoGlobal,
                        Vigencia = regReciente.Vigencia,
                        ArchivoUrl = regReciente.ArchivoUrl,
                        ObsAbril = regReciente.ObsAbril,
                        ObsContratista = regReciente.ObsContratista,
                        RequiereVigencia = item.RequiereVigencia,
                        EsMensual = true,
                        Responsable = item.Responsable,
                        Mes = regReciente.Mes,
                        Anio = regReciente.Anio,
                        Meses = meses
                    });
                }
            }

            return result;
        }

        private static EmpresaEntregableDto MapToDto(SsHabEmpresa r, SsItemEmpresa item, List<EntregableMesDto> meses, List<EntregableMesArchivoDto> archivos)
            => new()
            {
                Id = r.Id,
                ItemId = item.Id,
                NombreItem = item.Nombre,
                Estado = r.Estado,
                Vigencia = r.Vigencia,
                ArchivoUrl = r.ArchivoUrl,
                ObsAbril = r.ObsAbril,
                ObsContratista = r.ObsContratista,
                MotivoRechazo = r.MotivoRechazo,
                RequiereVigencia = item.RequiereVigencia,
                EsMensual = item.EsMensual,
                Responsable = item.Responsable,
                Mes = r.Mes,
                Anio = r.Anio,
                Meses = meses,
                Archivos = archivos
            };

        public async Task<SsHabEmpresa> UpdateEntregableEmpresaAsync(
            int id, EmpresaEntregableUpdateDto dto, int? userId, int? empresaId = null)
        {
            using var ctx = _factory.CreateDbContext();

            var entregable = await ctx.SsHabEmpresa
                .Include(h => h.Item)
                .FirstOrDefaultAsync(h => h.Id == id)
                ?? throw new AbrilException("Entregable no encontrado.", 404);

            if (!string.IsNullOrWhiteSpace(dto.ArchivoUrl) && dto.ArchivoUrl != entregable.ArchivoUrl)
            {
                int? ssEmpresaId = empresaId;

                var versionActual = await ctx.SsHabDocumentoVersion
                    .CountAsync(v => v.HabEmpresaId == id);

                ctx.SsHabDocumentoVersion.Add(new SsHabDocumentoVersion
                {
                    HabEmpresaId = id,
                    Version = versionActual + 1,
                    ArchivoUrl = dto.ArchivoUrl,
                    SubidoPorUserId = userId,
                    SubidoPorEmpresaId = ssEmpresaId,
                    EstadoAlSubir = dto.Estado,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (!string.IsNullOrEmpty(dto.Estado))
                entregable.Estado = dto.Estado;

            if (string.Equals(dto.Estado, "Enviado", StringComparison.OrdinalIgnoreCase))
            {
                entregable.Vigencia = HabilitacionDateHelper.ResolverVigenciaAlEnviar(
                    entregable.ItemId,
                    entregable.Item?.EsMensual ?? false,
                    entregable.Mes,
                    entregable.Anio,
                    dto.Vigencia);
            }
            else if (string.Equals(dto.Estado, "Aprobado", StringComparison.OrdinalIgnoreCase)
                  || string.Equals(dto.Estado, "Rechazado", StringComparison.OrdinalIgnoreCase))
            {
                entregable.Vigencia = HabilitacionDateHelper.ResolverVigenciaAlAprobar(
                    entregable.ItemId, entregable.Estado, dto.Vigencia, entregable.Vigencia);
                entregable.AprobadoPor = userId;
                entregable.FechaAprobacion = DateTime.UtcNow;
                if (string.Equals(dto.Estado, "Rechazado", StringComparison.OrdinalIgnoreCase))
                    entregable.MotivoRechazo = dto.MotivoRechazo;
            }
            else if (dto.Vigencia.HasValue)
            {
                entregable.Vigencia = HabilitacionDateHelper.AsUtc(dto.Vigencia);
            }

            if (dto.ArchivoUrl is not null) entregable.ArchivoUrl = dto.ArchivoUrl;
            if (dto.ObsAbril is not null) entregable.ObsAbril = dto.ObsAbril;
            if (dto.ObsContratista is not null) entregable.ObsContratista = dto.ObsContratista;
            if (dto.Mes is not null) entregable.Mes = dto.Mes;
            if (dto.Anio is not null) entregable.Anio = dto.Anio;
            entregable.UpdatedAt = DateTime.UtcNow;

            await ctx.SaveChangesAsync();
            return entregable;
        }

        public async Task<List<SsHabDocumentoVersionDto>> GetVersionesDocumentoEmpresaAsync(int empresaId, int itemId)
        {
            using var ctx = _factory.CreateDbContext();

            var habEmpresaIds = await ctx.SsHabEmpresa
                .Where(h => h.EmpresaId == empresaId && h.ItemId == itemId)
                .Select(h => h.Id)
                .ToListAsync();

            if (habEmpresaIds.Count == 0) return [];

            var versiones = await ctx.SsHabDocumentoVersion
                .Where(v => v.HabEmpresaId.HasValue && habEmpresaIds.Contains(v.HabEmpresaId.Value))
                .OrderByDescending(v => v.Version)
                .ToListAsync();

            var userIds = versiones
                .Where(v => v.SubidoPorUserId.HasValue)
                .Select(v => v.SubidoPorUserId!.Value)
                .Distinct()
                .ToList();

            var nombresPorUserId = new Dictionary<int, string?>();
            if (userIds.Count > 0)
            {
                var users = await (
                    from u in ctx.User
                    join p in ctx.Person on u.UserId equals p.UserId
                    where userIds.Contains(u.UserId)
                    select new { u.UserId, p.FullName }
                  ).ToListAsync();

                foreach (var x in users)
                    nombresPorUserId[x.UserId] = x.FullName;
            }

            return versiones.Select(v => new SsHabDocumentoVersionDto
            {
                Id = v.Id,
                HabTrabajadorId = v.HabTrabajadorId,
                Version = v.Version,
                ArchivoUrl = v.ArchivoUrl,
                SubidoPorUserId = v.SubidoPorUserId,
                SubidoPorNombre = v.SubidoPorUserId.HasValue && nombresPorUserId.TryGetValue(v.SubidoPorUserId.Value, out var nombre)
                    ? nombre
                    : null,
                SubidoPorEmpresaId = v.SubidoPorEmpresaId,
                EstadoAlSubir = v.EstadoAlSubir,
                EstadoAnterior = v.EstadoAnterior,
                ProyectoId = v.ProyectoId,
                EmpresaId = v.EmpresaId,
                AprobadoPorUserId = v.AprobadoPorUserId,
                MotivoRechazo = v.MotivoRechazo,
                CreatedAt = v.CreatedAt
            }).ToList();
        }

        public async Task InicializarEntregablesEmpresaAsync(int empresaId, int proyectoId)
        {
            using var ctx = _factory.CreateDbContext();

            var items = await ctx.SsItemEmpresa
                .Where(i => i.Activo)
                .ToListAsync();

            var existentesIds = await ctx.SsHabEmpresa
                .Where(h => h.EmpresaId == empresaId && h.ProyectoId == proyectoId)
                .Select(h => h.ItemId)
                .ToListAsync();

            var faltantes = items
                .Where(i => !existentesIds.Contains(i.Id))
                .Select(i => new SsHabEmpresa
                {
                    EmpresaId = empresaId,
                    ProyectoId = proyectoId,
                    ItemId = i.Id,
                    Estado = "Falta",
                    Vigencia = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                })
                .ToList();

            if (faltantes.Count > 0)
            {
                ctx.SsHabEmpresa.AddRange(faltantes);
                await ctx.SaveChangesAsync();
            }
        }

        public async Task ActivarProyectoAsync(int empresaId, int proyectoId)
        {
            using var ctx = _factory.CreateDbContext();

            var proyectoExiste = await ctx.Project.AnyAsync(p => p.ProjectId == proyectoId);
            if (!proyectoExiste)
                throw new AbrilException("Proyecto no encontrado.", 404);

            // Si ya existe una fila para (empresa, proyecto) — reactivar en lugar de insertar
            // para evitar violación de UNIQUE constraint (empresa_id, proyecto_id) cuando se
            // re-activa un proyecto previamente desactivado.
            var existente = await ctx.SsEmpresaProyecto
                .FirstOrDefaultAsync(ep => ep.EmpresaId == empresaId && ep.ProyectoId == proyectoId);

            if (existente != null)
            {
                if (existente.Activo)
                    throw new AbrilException("La empresa ya está activa en este proyecto.", 409);

                existente.Activo      = true;
                existente.FechaInicio = DateTime.UtcNow;
                existente.FechaFin    = null;
            }
            else
            {
                ctx.SsEmpresaProyecto.Add(new SsEmpresaProyecto
                {
                    EmpresaId   = empresaId,
                    ProyectoId  = proyectoId,
                    Activo      = true,
                    FechaInicio = DateTime.UtcNow,
                    CreatedAt   = DateTime.UtcNow
                });
            }

            await ctx.SaveChangesAsync();

            await InicializarEntregablesEmpresaAsync(empresaId, proyectoId);
        }

        public async Task<List<ProyectoDisponibleDto>> GetProyectosDisponiblesAsync(int empresaId)
        {
            using var ctx = _factory.CreateDbContext();

            var proyectos = await ctx.Project
                .Where(p => p.State && p.Active && !ctx.ProyectoFiltro.Any(f => f.ProjectId == p.ProjectId && f.FuncionalidadId == ProyectoFiltroFuncionalidades.Habilitacion && !f.Active))
                .OrderBy(p => p.ProjectDescription)
                .Select(p => new { p.ProjectId, p.ProjectDescription })
                .ToListAsync();

            var activas = await ctx.SsEmpresaProyecto
                .Where(ep => ep.EmpresaId == empresaId && ep.Activo)
                .Select(ep => new { ep.ProyectoId, ep.FechaInicio })
                .ToListAsync();

            var activasMap = activas.ToDictionary(ep => ep.ProyectoId, ep => ep.FechaInicio);

            return proyectos.Select(p => new ProyectoDisponibleDto
            {
                Id = p.ProjectId,
                Nombre = p.ProjectDescription,
                EstaActiva = activasMap.ContainsKey(p.ProjectId),
                FechaInicio = activasMap.TryGetValue(p.ProjectId, out var fi) && fi.HasValue
                    ? DateOnly.FromDateTime(fi.Value)
                    : null
            }).ToList();
        }

        public async Task DesactivarProyectoAsync(int empresaId, int proyectoId)
        {
            using var ctx = _factory.CreateDbContext();

            var registro = await ctx.SsEmpresaProyecto
                .FirstOrDefaultAsync(ep => ep.EmpresaId == empresaId && ep.ProyectoId == proyectoId && ep.Activo)
                ?? throw new AbrilException("No existe una activación activa para esa empresa y proyecto.", 404);

            registro.Activo = false;
            registro.FechaFin = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
        }

        public async Task<SsHabEmpresa> CrearOActualizarEntregableMesAsync(
            int empresaId, int proyectoId, int itemId, int mes, int anio,
            EmpresaEntregableUpdateDto dto, int? userId, int? empresaContId)
        {
            using var ctx = _factory.CreateDbContext();

            var entregable = await ctx.SsHabEmpresa
                .Include(h => h.Item)
                .FirstOrDefaultAsync(h =>
                    h.EmpresaId == empresaId &&
                    h.ProyectoId == proyectoId &&
                    h.ItemId == itemId &&
                    h.Mes == mes &&
                    h.Anio == anio);

            if (entregable == null)
            {
                _ = await ctx.SsItemEmpresa.FindAsync(itemId)
                    ?? throw new AbrilException("Item no encontrado.", 404);

                entregable = new SsHabEmpresa
                {
                    EmpresaId = empresaId,
                    ProyectoId = proyectoId,
                    ItemId = itemId,
                    Mes = mes,
                    Anio = anio,
                    Estado = "Falta",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                ctx.SsHabEmpresa.Add(entregable);
                await ctx.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(dto.Estado)) entregable.Estado = dto.Estado;
            if (dto.ArchivoUrl is not null) entregable.ArchivoUrl = dto.ArchivoUrl;
            if (dto.ObsAbril is not null) entregable.ObsAbril = dto.ObsAbril;
            if (dto.ObsContratista is not null) entregable.ObsContratista = dto.ObsContratista;
            if (dto.MotivoRechazo is not null) entregable.MotivoRechazo = dto.MotivoRechazo;

            entregable.Vigencia = HabilitacionDateHelper.ResolverVigenciaEmpresa(
                entregable.ItemId, entregable.Estado, dto.Vigencia ?? entregable.Vigencia);

            if (string.Equals(dto.Estado, "Aprobado", StringComparison.OrdinalIgnoreCase))
            {
                entregable.AprobadoPor = userId;
                entregable.FechaAprobacion = DateTime.UtcNow;
            }

            entregable.UpdatedAt = DateTime.UtcNow;

            if (dto.ArchivoUrl is not null && dto.ArchivoUrl != entregable.ArchivoUrl)
            {
                var versionActual = await ctx.SsHabDocumentoVersion
                    .CountAsync(v => v.HabEmpresaId == entregable.Id);
                ctx.SsHabDocumentoVersion.Add(new SsHabDocumentoVersion
                {
                    HabEmpresaId = entregable.Id,
                    Version = versionActual + 1,
                    ArchivoUrl = dto.ArchivoUrl,
                    SubidoPorUserId = userId,
                    SubidoPorEmpresaId = empresaContId,
                    EstadoAlSubir = dto.Estado,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await ctx.SaveChangesAsync();
            return entregable;
        }

        public async Task EliminarArchivoVersionAsync(int versionArchivoId, int empresaId)
        {
            using var ctx = _factory.CreateDbContext();

            var archivo = await ctx.SsHabDocumentoArchivo
                .Include(a => a.Version)
                .FirstOrDefaultAsync(a => a.Id == versionArchivoId)
                ?? throw new AbrilException("Archivo no encontrado.", 404);

            if (archivo.Version?.HabEmpresaId.HasValue == true)
            {
                var entregable = await ctx.SsHabEmpresa
                    .FindAsync(archivo.Version.HabEmpresaId.Value)
                    ?? throw new AbrilException("Entregable no encontrado.", 404);

                if (entregable.EmpresaId != empresaId)
                    throw new AbrilException("No tienes permiso para eliminar este archivo.", 403);

                if (entregable.Estado == "Aprobado" || entregable.Estado == "Rechazado")
                    throw new AbrilException("No puedes eliminar archivos de un entregable ya revisado.", 403);
            }

            ctx.SsHabDocumentoArchivo.Remove(archivo);
            await ctx.SaveChangesAsync();
        }

        public async Task<string?> GetResponsableItemEmpresaAsync(int entregableId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsHabEmpresa
                .Where(h => h.Id == entregableId)
                .Select(h => h.Item != null ? h.Item.Responsable : null)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Empresas activas en un proyecto con su estado de habilitación (mismo criterio SSOMA
        /// que <see cref="EmpresaHabilitacionHelper"/> usa en Trabajadores/Dashboard) y sus
        /// entregables subidos/faltantes agrupados por responsable (SSOMA vs Administración).
        /// Fuente para el filtro "por proyecto" de la pantalla Empresa.
        /// </summary>
        public async Task<List<EmpresaPorProyectoDto>> GetEmpresasPorProyectoAsync(int proyectoId)
        {
            using var ctx = _factory.CreateDbContext();

            var empresasActivas = await ctx.SsEmpresaProyecto
                .Include(ep => ep.Empresa)
                .Where(ep => ep.ProyectoId == proyectoId && ep.Activo)
                .ToListAsync();

            if (empresasActivas.Count == 0) return [];

            var itemsActivos = await ctx.SsItemEmpresa
                .Where(i => i.Activo && !EmpresaHabilitacionHelper.ItemsSctrVidaLey.Contains(i.Id))
                .ToListAsync();
            var itemsPorId = itemsActivos.ToDictionary(i => i.Id, i => i.Responsable);
            var itemsSsomaIds = itemsActivos
                .Where(i => string.Equals(i.Responsable, "SSOMA", StringComparison.OrdinalIgnoreCase))
                .Select(i => i.Id)
                .ToHashSet();

            var registros = await ctx.SsHabEmpresa
                .Where(h => h.ProyectoId == proyectoId && itemsPorId.Keys.Contains(h.ItemId))
                .ToListAsync();

            var habilitadasMap = EmpresaHabilitacionHelper.CalcularHabilitadas(registros, itemsSsomaIds);

            // "Vigente" por ítem = el registro más reciente (mismo criterio que el helper) —
            // así un ítem mensual con historial no se cuenta varias veces.
            var vigentesPorEmpresa = registros
                .GroupBy(r => (r.EmpresaId, r.ItemId))
                .Select(g => g.OrderByDescending(r => r.Anio).ThenByDescending(r => r.Mes).First())
                .GroupBy(r => r.EmpresaId)
                .ToDictionary(g => g.Key, g => g.ToList());

            return empresasActivas.Select(ep =>
            {
                var vigentes = vigentesPorEmpresa.TryGetValue(ep.EmpresaId, out var v) ? v : new List<SsHabEmpresa>();
                bool EsSsoma(int itemId) => itemsSsomaIds.Contains(itemId);

                return new EmpresaPorProyectoDto
                {
                    EmpresaId = ep.EmpresaId,
                    Nombre = ep.Empresa?.ContributorName ?? "",
                    Habilitada = habilitadasMap.TryGetValue((ep.EmpresaId, proyectoId), out var hab) && hab,
                    EntregablesSsomaSubidos = vigentes.Count(r => EsSsoma(r.ItemId) && r.Estado != "Falta"),
                    EntregablesSsomaFaltantes = vigentes.Count(r => EsSsoma(r.ItemId) && r.Estado == "Falta"),
                    EntregablesAdminSubidos = vigentes.Count(r => !EsSsoma(r.ItemId) && r.Estado != "Falta"),
                    EntregablesAdminFaltantes = vigentes.Count(r => !EsSsoma(r.ItemId) && r.Estado == "Falta"),
                };
            })
            .OrderBy(e => e.Nombre)
            .ToList();
        }

    }
}

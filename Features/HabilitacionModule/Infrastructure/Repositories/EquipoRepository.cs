using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Equipos;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Abril_Backend.Features.Habilitacion.Infrastructure.Helpers;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Repositories
{
    public class EquipoRepository : IEquipoRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public EquipoRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<(List<EquipoListDto> Items, int Total)> GetPagedAsync(
            int? proyectoId, int? empresaId, string? search,
            bool? activo, int page, int pageSize)
        {
            using var ctx = _factory.CreateDbContext();

            var query = ctx.SsEquipo.Include(e => e.TipoEquipo).AsQueryable();

            if (proyectoId.HasValue) query = query.Where(e => e.ProyectoId == proyectoId.Value);
            if (empresaId.HasValue) query = query.Where(e => e.PropietarioEmpresaId == empresaId.Value);
            if (activo.HasValue) query = query.Where(e => e.Activo == activo.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(e =>
                    (e.TipoEquipo != null && e.TipoEquipo.Nombre.ToLower().Contains(s)) ||
                    (e.Marca != null && e.Marca.ToLower().Contains(s)) ||
                    (e.Modelo != null && e.Modelo.ToLower().Contains(s)) ||
                    (e.NSerie != null && e.NSerie.ToLower().Contains(s)));
            }

            var withState = query.Select(e => new
            {
                Equipo = e,
                HasPendientes = !ctx.SsHabEquipo.Any(h => h.EquipoId == e.Id
                                    && h.Item != null && h.Item.Activo && (h.Item.TipoEquipoId == null || h.Item.TipoEquipoId == e.TipoEquipoId))
                             || ctx.SsHabEquipo.Any(h => h.EquipoId == e.Id
                                    && h.Item != null && h.Item.Activo && (h.Item.TipoEquipoId == null || h.Item.TipoEquipoId == e.TipoEquipoId)
                                    && h.Estado != "No Aplica" && h.Estado != "Aprobado")
            });

            var total = await withState.CountAsync();

            var pageRows = await withState
                .OrderBy(x => x.Equipo.TipoEquipo!.Nombre)
                .ThenBy(x => x.Equipo.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var empresaIds = pageRows
                .Where(r => r.Equipo.PropietarioEmpresaId.HasValue)
                .Select(r => r.Equipo.PropietarioEmpresaId!.Value)
                .Distinct()
                .ToList();
            var proyectoIds = pageRows.Select(r => r.Equipo.ProyectoId).Distinct().ToList();

            var empresaMap = await ctx.Contributor
                .Where(c => empresaIds.Contains(c.ContributorId))
                .ToDictionaryAsync(c => c.ContributorId, c => c.ContributorName);

            var proyectoMap = await ctx.Project
                .Where(p => proyectoIds.Contains(p.ProjectId))
                .ToDictionaryAsync(p => p.ProjectId, p => p.ProjectDescription);

            var items = pageRows.Select(r => new EquipoListDto
            {
                Id = r.Equipo.Id,
                TipoEquipoId = r.Equipo.TipoEquipoId,
                TipoNombre = r.Equipo.TipoEquipo?.Nombre ?? string.Empty,
                Marca = r.Equipo.Marca,
                Modelo = r.Equipo.Modelo,
                NSerie = r.Equipo.NSerie,
                Capacidad = r.Equipo.Capacidad,
                PropietarioEmpresaNombre = r.Equipo.PropietarioEmpresaId is int eid && empresaMap.TryGetValue(eid, out var en) ? en : null,
                ProyectoId = r.Equipo.ProyectoId,
                ProyectoNombre = proyectoMap.TryGetValue(r.Equipo.ProyectoId, out var pn) ? pn ?? string.Empty : string.Empty,
                EstadoHabilitacion = r.HasPendientes ? "No Autorizado" : "Habilitado",
                Activo = r.Equipo.Activo
            }).ToList();

            return (items, total);
        }

        public async Task<SsEquipo?> GetByIdAsync(int id)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsEquipo.FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<SsEquipo> CreateAsync(EquipoCreateDto dto)
        {
            using var ctx = _factory.CreateDbContext();
            var entity = new SsEquipo
            {
                TipoEquipoId = dto.TipoEquipoId,
                Marca = dto.Marca,
                Modelo = dto.Modelo,
                NSerie = dto.NSerie,
                NVin = dto.NVin,
                Capacidad = dto.Capacidad,
                PropietarioEmpresaId = dto.PropietarioEmpresaId,
                ProyectoId = dto.ProyectoId,
                Activo = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            ctx.SsEquipo.Add(entity);
            await ctx.SaveChangesAsync();
            return entity;
        }

        public async Task<SsEquipo> UpdateAsync(int id, EquipoCreateDto dto)
        {
            using var ctx = _factory.CreateDbContext();
            var entity = await ctx.SsEquipo.FirstOrDefaultAsync(e => e.Id == id)
                ?? throw new AbrilException("Equipo no encontrado.", 404);

            entity.TipoEquipoId = dto.TipoEquipoId;
            entity.Marca = dto.Marca;
            entity.Modelo = dto.Modelo;
            entity.NSerie = dto.NSerie;
            entity.NVin = dto.NVin;
            entity.Capacidad = dto.Capacidad;
            entity.PropietarioEmpresaId = dto.PropietarioEmpresaId;
            entity.ProyectoId = dto.ProyectoId;
            entity.UpdatedAt = DateTime.UtcNow;

            await ctx.SaveChangesAsync();
            return entity;
        }

        public async Task<List<EquipoEntregableDto>> GetEntregablesAsync(int equipoId)
        {
            using var ctx = _factory.CreateDbContext();

            var equipo = await ctx.SsEquipo.FirstOrDefaultAsync(e => e.Id == equipoId)
                ?? throw new AbrilException("Equipo no encontrado.", 404);

            var items = await ctx.SsItemEquipo
                .Where(i => i.Activo && (i.TipoEquipoId == null || i.TipoEquipoId == equipo.TipoEquipoId))
                .OrderBy(i => i.Orden)
                .ToListAsync();

            var itemIds = items.Select(i => i.Id).ToList();

            var existentes = await ctx.SsHabEquipo
                .Where(h => h.EquipoId == equipoId && itemIds.Contains(h.ItemId))
                .ToListAsync();

            var faltantes = items
                .Where(i => !existentes.Any(h => h.ItemId == i.Id))
                .Select(i => new SsHabEquipo
                {
                    EquipoId = equipoId,
                    ItemId = i.Id,
                    Estado = "Falta",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                })
                .ToList();

            if (faltantes.Count > 0)
            {
                ctx.SsHabEquipo.AddRange(faltantes);
                await ctx.SaveChangesAsync();
                existentes.AddRange(faltantes);
            }

            var itemMap = items.ToDictionary(i => i.Id);

            return existentes
                .Where(h => itemMap.ContainsKey(h.ItemId))
                .Select(h =>
                {
                    var item = itemMap[h.ItemId];
                    return new EquipoEntregableDto
                    {
                        Id = h.Id,
                        ItemId = h.ItemId,
                        NombreItem = item.Nombre,
                        Estado = h.Estado,
                        Vigencia = h.Vigencia,
                        ArchivoUrl = h.ArchivoUrl,
                        ObsAbril = h.ObsAbril,
                        ObsContratista = h.ObsContratista,
                        RequiereVigencia = item.RequiereVigencia
                    };
                })
                .OrderBy(d => itemMap[d.ItemId].Orden)
                .ToList();
        }

        public async Task<List<SsHabDocumentoVersionEquipoDto>> GetVersionesAsync(int habEquipoId)
        {
            using var ctx = _factory.CreateDbContext();

            var versiones = await ctx.SsHabDocumentoVersion
                .Where(v => v.HabEquipoId == habEquipoId)
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

            return versiones.Select(v => new SsHabDocumentoVersionEquipoDto
            {
                Id = v.Id,
                HabEquipoId = v.HabEquipoId,
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

        public async Task<SsHabEquipo> UpdateEntregableAsync(int id, EquipoEntregableUpdateDto dto, int? userId, int? empresaId = null)
        {
            using var ctx = _factory.CreateDbContext();

            var entregable = await ctx.SsHabEquipo
                .Include(h => h.Item)
                .FirstOrDefaultAsync(h => h.Id == id)
                ?? throw new AbrilException("Entregable no encontrado.", 404);

            if (!string.IsNullOrWhiteSpace(dto.ArchivoUrl) && dto.ArchivoUrl != entregable.ArchivoUrl)
            {
                int? ssEmpresaId = empresaId;

                var versionActual = await ctx.SsHabDocumentoVersion
                    .CountAsync(v => v.HabEquipoId == id);

                ctx.SsHabDocumentoVersion.Add(new SsHabDocumentoVersion
                {
                    HabEquipoId = id,
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
            if (!string.IsNullOrEmpty(dto.Estado) || dto.Vigencia.HasValue)
                entregable.Vigencia = HabilitacionDateHelper.ResolverVigencia(entregable.Item?.RequiereVigencia ?? true, entregable.Estado, dto.Vigencia);
            if (dto.ArchivoUrl is not null) entregable.ArchivoUrl = dto.ArchivoUrl;
            if (dto.ObsAbril is not null) entregable.ObsAbril = dto.ObsAbril;
            if (dto.ObsContratista is not null) entregable.ObsContratista = dto.ObsContratista;
            entregable.UpdatedAt = DateTime.UtcNow;

            if (string.Equals(dto.Estado, "Aprobado", StringComparison.OrdinalIgnoreCase))
            {
                entregable.AprobadoPor = userId;
            }

            await ctx.SaveChangesAsync();
            return entregable;
        }
    }
}

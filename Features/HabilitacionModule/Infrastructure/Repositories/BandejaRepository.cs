using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Bandeja;
using Abril_Backend.Features.Habilitacion.Application.Dtos.HabEmpresa;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Proyectos;
using Abril_Backend.Features.Habilitacion.Infrastructure.Helpers;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.DTOs;
using Abril_Backend.Shared.Helpers;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;
using System.Text;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Repositories
{
    public class BandejaRepository : IBandejaRepository
    {
        static BandejaRepository()
        {
            DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IConfiguration _configuration;

        public BandejaRepository(IDbContextFactory<AppDbContext> factory, IConfiguration configuration)
        {
            _factory = factory;
            _configuration = configuration;
        }

        private IDbConnection CreateConnection()
            => new NpgsqlConnection(_configuration["Database:PostgreSQL"]);

        /// <summary>
        /// El filtro de "Área" elige un nodo del árbol area_scope (p.ej. "Ventas", colgado hoy
        /// de "Gerencia de Administración"): debe incluir a ese nodo y a todos sus hijos, no
        /// solo coincidencia exacta. Null si no hay filtro — así el placeholder SQL
        /// "@AreaScopeIds::int[] IS NULL" deja pasar todo.
        ///
        /// El cast ::int[] en el SQL no es cosmético: al ser null, Dapper manda DBNull sin tipo
        /// y Npgsql deja que el servidor lo infiera, pero ni "$n IS NULL" ni "ANY($n)" dan pista
        /// de tipo → Postgres revienta con 42P08 ("no se pudo determinar el tipo del parámetro").
        /// Con el cast el tipo queda resuelto. No quitarlo.
        /// </summary>
        private async Task<int[]?> ResolverAreaScopeIdsAsync(int? areaScopeId)
        {
            if (!areaScopeId.HasValue) return null;
            using var ctx = _factory.CreateDbContext();
            return (await ctx.ResolveDescendantsAsync(areaScopeId.Value)).ToArray();
        }

        private const string SelectBase = @"
SELECT
    ht.id, 'TRABAJADOR' as tipo,
    i.nombre as nombre_entregable,
    COALESCE(per.full_name, '') as entidad_nombre,
    ec.contributor_name as empresa_nombre,
    p.project_description as proyecto_nombre,
    p.project_id as proyecto_id,
    ht.estado,
    CAST(ht.vigencia AS timestamp) as vigencia,
    ht.archivo_url,
    ht.obs_contratista,
    i.responsable,
    ht.updated_at as fecha_envio,
    NULL::int as item_id,
    false as es_mensual,
    NULL::int as empresa_id_raw,
    NULL::int as mes,
    NULL::int as anio,
    0 as meses_pendientes,
    i.requiere_vigencia as requiere_vigencia
FROM ss_hab_trabajador ht
JOIN ss_item_trabajador i ON i.id = ht.item_id
JOIN workers w ON w.id = ht.worker_id AND w.state
LEFT JOIN person per ON per.person_id = w.person_id
LEFT JOIN LATERAL (
    SELECT empresa_id, proyecto_id
    FROM worker_vinculaciones
    WHERE worker_id = w.id AND fecha_fin IS NULL
    ORDER BY created_at DESC, id DESC
    LIMIT 1
) wv ON TRUE
LEFT JOIN contributor ec ON ec.contributor_id = wv.empresa_id
LEFT JOIN project p ON p.project_id = wv.proyecto_id
WHERE ht.estado = 'Enviado'
  AND ht.item_id NOT IN (11, 12, 13)
  AND NOT (ht.item_id IN (4, 25) AND w.contrata_casa = 'Casa')
  AND (@ProyectoId IS NULL OR wv.proyecto_id = @ProyectoId)
  AND (@EmpresaId IS NULL OR wv.empresa_id = @EmpresaId)
  AND (@Responsable IS NULL OR i.responsable = @Responsable)
  AND (@Tipo IS NULL OR @Tipo = 'TRABAJADOR')
  AND (@Search IS NULL OR per.full_name ILIKE '%' || @Search || '%')
  AND (@AreaScopeIds::int[] IS NULL OR w.area_scope_id = ANY(@AreaScopeIds::int[]))

UNION ALL

SELECT
    he.id, 'EMPRESA' as tipo,
    i.nombre as nombre_entregable,
    ec.contributor_name as entidad_nombre,
    ec.contributor_name as empresa_nombre,
    p.project_description as proyecto_nombre,
    he.proyecto_id,
    he.estado,
    CAST(he.vigencia AS timestamp) as vigencia,
    he.archivo_url,
    he.obs_contratista,
    i.responsable,
    he.updated_at as fecha_envio,
    i.id as item_id,
    i.es_mensual as es_mensual,
    he.empresa_id as empresa_id_raw,
    he.mes as mes,
    he.anio as anio,
    (SELECT COUNT(*) FROM ss_hab_empresa sub
     WHERE sub.empresa_id = he.empresa_id
       AND sub.proyecto_id = he.proyecto_id
       AND sub.item_id = he.item_id
       AND sub.estado = 'Enviado') as meses_pendientes,
    i.requiere_vigencia as requiere_vigencia
FROM ss_hab_empresa he
JOIN ss_item_empresa i ON i.id = he.item_id
JOIN contributor ec ON ec.contributor_id = he.empresa_id
JOIN project p ON p.project_id = he.proyecto_id
WHERE he.estado = 'Enviado'
  AND he.item_id NOT IN (15, 16)
  AND (NOT i.es_mensual OR he.id = (
      SELECT id FROM ss_hab_empresa sub2
      WHERE sub2.empresa_id = he.empresa_id
        AND sub2.proyecto_id = he.proyecto_id
        AND sub2.item_id = he.item_id
        AND sub2.estado = 'Enviado'
        AND sub2.mes IS NOT NULL
      ORDER BY sub2.anio DESC, sub2.mes DESC
      LIMIT 1
  ))
  AND (@ProyectoId IS NULL OR he.proyecto_id = @ProyectoId)
  AND (@EmpresaId IS NULL OR he.empresa_id = @EmpresaId)
  AND (@Responsable IS NULL OR i.responsable = @Responsable)
  AND (@Tipo IS NULL OR @Tipo = 'EMPRESA')
  AND (@Search IS NULL OR ec.contributor_name ILIKE '%' || @Search || '%')
  -- Entregable a nivel empresa, no de un trabajador puntual: no tiene área propia,
  -- así que se excluye del listado en cuanto se filtra por área.
  AND @AreaScopeIds::int[] IS NULL

UNION ALL

SELECT
    heq.id, 'EQUIPO' as tipo,
    i.nombre as nombre_entregable,
    CONCAT(te.nombre, ' - ', eq.marca, ' ', eq.modelo) as entidad_nombre,
    ec.contributor_name as empresa_nombre,
    p.project_description as proyecto_nombre,
    eq.proyecto_id,
    heq.estado,
    CAST(heq.vigencia AS timestamp) as vigencia,
    heq.archivo_url,
    NULL as obs_contratista,
    'SSOMA' as responsable,
    heq.updated_at as fecha_envio,
    NULL::int as item_id,
    false as es_mensual,
    NULL::int as empresa_id_raw,
    NULL::int as mes,
    NULL::int as anio,
    0 as meses_pendientes,
    i.requiere_vigencia as requiere_vigencia
FROM ss_hab_equipo heq
JOIN ss_item_equipo i ON i.id = heq.item_id
JOIN ss_equipo eq ON eq.id = heq.equipo_id
JOIN ss_tipo_equipo te ON te.id = eq.tipo_equipo_id
LEFT JOIN contributor ec ON ec.contributor_id = eq.propietario_empresa_id
JOIN project p ON p.project_id = eq.proyecto_id
WHERE heq.estado = 'Enviado'
  AND (@ProyectoId IS NULL OR eq.proyecto_id = @ProyectoId)
  AND (@EmpresaId IS NULL OR eq.propietario_empresa_id = @EmpresaId)
  AND (@Tipo IS NULL OR @Tipo = 'EQUIPO')
  AND (@Responsable IS NULL OR @Responsable = 'SSOMA')
  AND (@Search IS NULL OR CONCAT(te.nombre, ' - ', eq.marca, ' ', eq.modelo) ILIKE '%' || @Search || '%')
  -- Entregable de un equipo, no de un trabajador: sin área propia.
  AND @AreaScopeIds::int[] IS NULL

UNION ALL

SELECT
    i.id, 'INDUCCION' as tipo,
    'Inducción de Obra' as nombre_entregable,
    COALESCE(per.full_name, '') as entidad_nombre,
    c.contributor_name as empresa_nombre,
    p.project_description as proyecto_nombre,
    p.project_id as proyecto_id,
    i.estado,
    NULL as vigencia,
    NULL as archivo_url,
    NULL as obs_contratista,
    'SSOMA' as responsable,
    i.created_at as fecha_envio,
    NULL::int as item_id,
    false as es_mensual,
    NULL::int as empresa_id_raw,
    NULL::int as mes,
    NULL::int as anio,
    0 as meses_pendientes,
    false as requiere_vigencia
FROM ss_induccion i
JOIN workers w ON w.id = i.worker_id AND w.state
LEFT JOIN person per ON per.person_id = w.person_id
JOIN contributor c ON c.contributor_id = i.empresa_id
JOIN project p ON p.project_id = i.proyecto_id
WHERE i.estado = 'PROGRAMADA'
  AND @Tipo = 'INDUCCION'
  AND (@ProyectoId IS NULL OR i.proyecto_id = @ProyectoId)
  AND (@EmpresaId IS NULL OR i.empresa_id = @EmpresaId)
  AND (@Responsable IS NULL OR @Responsable = 'SSOMA')
  AND (@Search IS NULL OR per.full_name ILIKE '%' || @Search || '%')
  AND (@AreaScopeIds::int[] IS NULL OR w.area_scope_id = ANY(@AreaScopeIds::int[]))
";

        public async Task<(List<BandejaItemDto> Items, int Total)> GetPendientesAsync(
            string? tipo, int? proyectoId, int? empresaId,
            string? responsable, string? search, int page, int pageSize,
            int? areaScopeId = null)
        {
            var areaScopeIds = await ResolverAreaScopeIdsAsync(areaScopeId);

            var parametros = new
            {
                Tipo = tipo,
                ProyectoId = proyectoId,
                EmpresaId = empresaId,
                Responsable = responsable,
                Search = search,
                AreaScopeIds = areaScopeIds,
                PageSize = pageSize,
                Offset = (page - 1) * pageSize
            };

            var dataSql = $@"SELECT * FROM ({SelectBase}) t
ORDER BY fecha_envio DESC NULLS LAST
LIMIT @PageSize OFFSET @Offset";

            var countSql = $"SELECT COUNT(*) FROM ({SelectBase}) t";

            using var conn = CreateConnection();
            var items = (await conn.QueryAsync<BandejaItemDto>(dataSql, parametros)).ToList();
            var total = await conn.ExecuteScalarAsync<int>(countSql, parametros);

            await EnrichWithArchivosAsync(items);

            return (items, total);
        }

        public async Task<CursorPagedResult<BandejaItemDto>> GetPendientesCursorAsync(
            string? tipo, int? proyectoId, int? empresaId,
            string? responsable, string? search, string? cursor, int pageSize,
            int? areaScopeId = null)
        {
            var areaScopeIds = await ResolverAreaScopeIdsAsync(areaScopeId);
            DateTime? cursorFecha = null;
            int? cursorId = null;
            if (!string.IsNullOrWhiteSpace(cursor))
            {
                try
                {
                    var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
                    var parts = raw.Split('_', 2);
                    if (parts.Length == 2
                        && long.TryParse(parts[0], out var ticks)
                        && int.TryParse(parts[1], out var idVal))
                    {
                        cursorFecha = new DateTime(ticks, DateTimeKind.Utc);
                        cursorId = idVal;
                    }
                }
                catch { /* cursor inválido se ignora */ }
            }

            var parametros = new
            {
                Tipo = tipo,
                ProyectoId = proyectoId,
                EmpresaId = empresaId,
                Responsable = responsable,
                Search = search,
                AreaScopeIds = areaScopeIds,
                CursorFecha = cursorFecha,
                CursorId = cursorId,
                PageSize = pageSize + 1
            };

            var sql = $@"SELECT * FROM ({SelectBase}) t
WHERE (@CursorFecha IS NULL
       OR t.fecha_envio < @CursorFecha
       OR (t.fecha_envio = @CursorFecha AND t.id < @CursorId))
ORDER BY t.fecha_envio DESC NULLS LAST, t.id DESC
LIMIT @PageSize";

            var countSql = $"SELECT COUNT(*) FROM ({SelectBase}) t";

            using var conn = CreateConnection();
            var rows = (await conn.QueryAsync<BandejaItemDto>(sql, parametros)).ToList();
            var total = await conn.ExecuteScalarAsync<int>(countSql, new
            {
                Tipo = tipo,
                ProyectoId = proyectoId,
                EmpresaId = empresaId,
                Responsable = responsable,
                Search = search,
                AreaScopeIds = areaScopeIds
            });

            var hasMore = rows.Count > pageSize;
            var page = hasMore ? rows.Take(pageSize).ToList() : rows;

            await EnrichWithArchivosAsync(page);

            string? nextCursor = null;
            if (hasMore && page.Count > 0)
            {
                var last = page[^1];
                var ticks = (last.FechaEnvio ?? DateTime.MinValue).Ticks;
                nextCursor = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{ticks}_{last.Id}"));
            }

            return new CursorPagedResult<BandejaItemDto>
            {
                Data = page,
                Total = total,
                NextCursor = nextCursor,
                HasMore = hasMore
            };
        }

        private async Task EnrichWithArchivosAsync(List<BandejaItemDto> items)
        {
            var empresaIds = items
                .Where(i => i.Tipo == "EMPRESA")
                .Select(i => i.Id)
                .Distinct()
                .ToList();

            if (empresaIds.Count == 0) return;

            Console.WriteLine($"empresaIds: {string.Join(",", empresaIds)}");

            using var ctx = _factory.CreateDbContext();

            var registrosBase = await ctx.SsHabEmpresa
                .Where(e => empresaIds.Contains(e.Id))
                .ToListAsync();

            var grupos = registrosBase
                .Select(r => new { r.EmpresaId, r.ProyectoId, r.ItemId })
                .Distinct()
                .ToList();

            var todosRegistros = new List<SsHabEmpresa>();
            foreach (var g in grupos)
            {
                var regs = await ctx.SsHabEmpresa
                    .Where(e => e.EmpresaId == g.EmpresaId
                             && e.ProyectoId == g.ProyectoId
                             && e.ItemId == g.ItemId)
                    .ToListAsync();
                todosRegistros.AddRange(regs);
            }

            var todosIds = todosRegistros.Select(r => r.Id).ToList();

            var versiones = await ctx.SsHabDocumentoVersion
                .Include(v => v.Archivos)
                .Where(v => v.HabEmpresaId.HasValue
                         && todosIds.Contains(v.HabEmpresaId.Value)
                         && v.Enviado)
                .ToListAsync();

            var archivosPorEntregable = versiones
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

            Console.WriteLine($"archivosPorEntregable keys: {string.Join(",", archivosPorEntregable.Keys)}");

            foreach (var item in items.Where(i => i.Tipo == "EMPRESA"))
            {
                var idsDelGrupo = todosRegistros
                    .Where(r => r.EmpresaId == registrosBase
                        .FirstOrDefault(b => b.Id == item.Id)?.EmpresaId
                        && r.ProyectoId == registrosBase
                        .FirstOrDefault(b => b.Id == item.Id)?.ProyectoId
                        && r.ItemId == registrosBase
                        .FirstOrDefault(b => b.Id == item.Id)?.ItemId)
                    .Select(r => r.Id)
                    .ToList();

                // Solo usar archivos del registro que aparece en bandeja (item.Id)
                // que es el mensual más reciente con estado Enviado
                var archivosDelItem = archivosPorEntregable.ContainsKey(item.Id)
                    ? archivosPorEntregable[item.Id]
                    : new List<EntregableMesArchivoDto>();

                if (archivosDelItem.Any())
                    item.Archivos = archivosDelItem;

                if (item.EsMensual)
                {
                    item.Meses = todosRegistros
                        .Where(r => r.EmpresaId == registrosBase.FirstOrDefault(b => b.Id == item.Id)?.EmpresaId
                                 && r.ProyectoId == registrosBase.FirstOrDefault(b => b.Id == item.Id)?.ProyectoId
                                 && r.ItemId == registrosBase.FirstOrDefault(b => b.Id == item.Id)?.ItemId
                                 && r.Mes.HasValue && r.Anio.HasValue
                                 && r.Estado == "Enviado")
                        .OrderByDescending(r => r.Anio)
                        .ThenByDescending(r => r.Mes)
                        .Select(r => new BandejaMesDto
                        {
                            Id = r.Id,
                            Mes = r.Mes!.Value,
                            Anio = r.Anio!.Value,
                            Estado = r.Estado,
                            Vigencia = r.Vigencia,
                            Archivos = archivosPorEntregable.TryGetValue(r.Id, out var arch) ? arch : []
                        })
                        .ToList();
                }
            }
        }

        public async Task<List<ProyectoSimpleDto>> GetProyectosUnicosAsync()
        {
            const string sql = @"
SELECT DISTINCT proyecto_id as Id, proyecto_nombre as Nombre
FROM (
    SELECT p.project_id as proyecto_id, p.project_description as proyecto_nombre
    FROM ss_hab_trabajador ht
    JOIN workers w ON w.id = ht.worker_id AND w.state
    LEFT JOIN LATERAL (
        SELECT proyecto_id FROM worker_vinculaciones
        WHERE worker_id = w.id AND fecha_fin IS NULL
        ORDER BY created_at DESC, id DESC LIMIT 1
    ) wv ON TRUE
    LEFT JOIN project p ON p.project_id = wv.proyecto_id
    WHERE ht.estado = 'Enviado'
    UNION
    SELECT p.project_id, p.project_description
    FROM ss_hab_empresa he
    JOIN project p ON p.project_id = he.proyecto_id
    WHERE he.estado = 'Enviado'
    UNION
    SELECT p.project_id, p.project_description
    FROM ss_hab_equipo heq
    JOIN ss_equipo eq ON eq.id = heq.equipo_id
    JOIN project p ON p.project_id = eq.proyecto_id
    WHERE heq.estado = 'Enviado'
    UNION
    SELECT p.project_id, p.project_description
    FROM ss_induccion i
    JOIN project p ON p.project_id = i.proyecto_id
    WHERE i.estado = 'PROGRAMADA'
) t
WHERE proyecto_nombre IS NOT NULL
ORDER BY Nombre";

            using var conn = CreateConnection();
            var result = await conn.QueryAsync<ProyectoSimpleDto>(sql);
            return result.ToList();
        }

        public async Task<List<string>> GetEmpresasUnicasAsync()
        {
            const string sql = @"
SELECT DISTINCT ec.contributor_name
FROM ss_hab_empresa he
JOIN contributor ec ON ec.contributor_id = he.empresa_id
WHERE he.estado = 'Enviado'
ORDER BY ec.contributor_name";

            using var conn = CreateConnection();
            var result = await conn.QueryAsync<string>(sql);
            return result.ToList();
        }

        public async Task<SsHabTrabajador?> AprobarTrabajadorAsync(int id, BandejaAprobarDto dto, int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var entity = await ctx.SsHabTrabajador
                .Include(h => h.Item)
                .FirstOrDefaultAsync(h => h.Id == id);
            if (entity is null) return null;

            entity.Estado = dto.Estado;
            entity.ObsAbril = dto.ObsAbril;

            var requiereVigencia = entity.Item?.RequiereVigencia ?? true;
            DateTime? nuevaVigencia;
            if (string.Equals(dto.Estado, "Aprobado", StringComparison.OrdinalIgnoreCase) && !dto.Vigencia.HasValue)
                nuevaVigencia = entity.Vigencia; // preservar vigencia existente al aprobar sin fecha
            else
                nuevaVigencia = HabilitacionDateHelper.ResolverVigencia(requiereVigencia, dto.Estado, dto.Vigencia);
            if (string.Equals(dto.Estado, "Aprobado", StringComparison.OrdinalIgnoreCase)
                && requiereVigencia && !nuevaVigencia.HasValue)
                throw new AbrilException("Este documento requiere fecha de vigencia para ser aprobado.", 400);

            entity.Vigencia = nuevaVigencia;
            entity.AprobadoPor = userId;
            entity.FechaAprobacion = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            await ctx.SaveChangesAsync();
            return entity;
        }

        public async Task<SsHabEmpresa?> AprobarEmpresaAsync(int id, BandejaAprobarDto dto, int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var entity = await ctx.SsHabEmpresa
                .Include(h => h.Item)
                .FirstOrDefaultAsync(h => h.Id == id);
            if (entity is null) return null;

            entity.Estado = dto.Estado;
            entity.ObsAbril = dto.ObsAbril;
            entity.Vigencia = HabilitacionDateHelper.ResolverVigenciaEmpresa(entity.ItemId, dto.Estado, dto.Vigencia);
            entity.AprobadoPor = userId;
            entity.FechaAprobacion = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            await ctx.SaveChangesAsync();
            return entity;
        }

        public async Task<SsHabEquipo?> AprobarEquipoAsync(int id, BandejaAprobarDto dto, int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var entity = await ctx.SsHabEquipo
                .Include(h => h.Item)
                .FirstOrDefaultAsync(h => h.Id == id);
            if (entity is null) return null;

            entity.Estado = dto.Estado;
            entity.ObsAbril = dto.ObsAbril;
            entity.Vigencia = HabilitacionDateHelper.ResolverVigencia(entity.Item?.RequiereVigencia ?? true, dto.Estado, dto.Vigencia);
            entity.AprobadoPor = userId;
            entity.UpdatedAt = DateTime.UtcNow;

            await ctx.SaveChangesAsync();
            return entity;
        }

    }
}

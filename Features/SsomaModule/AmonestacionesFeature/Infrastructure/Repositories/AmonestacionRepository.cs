using Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Infrastructure.Repositories;

public class AmonestacionRepository : IAmonestacionRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IConfiguration _config;

    public AmonestacionRepository(IDbContextFactory<AppDbContext> factory, IConfiguration config)
    {
        _factory = factory;
        _config = config;
    }

    private NpgsqlConnection Conn() => new(_config["Database:PostgreSQL"]);

    public async Task<AmonestacionInitDto> GetInitAsync()
    {
        const string sql = """
            SELECT id, nombre, nivel_gravedad AS nivelGravedad, genera_suspension AS generaSuspension
            FROM ssoma_amonestacion_tipo_sanciones WHERE state = true ORDER BY id;

            SELECT id, nombre FROM ssoma_amonestacion_infraccion_tipos WHERE state = true ORDER BY nombre;

            SELECT id, nombre, monto_fijo AS montoFijo, factor_uit AS factorUit
            FROM ssoma_rac_infraccion WHERE activo = true ORDER BY nombre;

            SELECT project_id AS id, project_description AS nombre
            FROM project WHERE active = true
              AND NOT EXISTS (SELECT 1 FROM proyecto_filtro f
                              JOIN proyecto_filtro_funcionalidad fn ON fn.id = f.funcionalidad_id
                              WHERE f.project_id = project.project_id
                                AND fn.codigo = 'SSOMA_AMONESTACIONES' AND f.active = false)
            ORDER BY project_description;

            SELECT partida_id AS id, partida_description AS nombre
            FROM partida WHERE active = true ORDER BY partida_description;

            SELECT COALESCE(valor, 0) FROM ssoma_uit_anio
            WHERE anio = EXTRACT(YEAR FROM CURRENT_DATE)::int AND activo = true
            LIMIT 1;
            """;

        await using var conn = Conn();
        await conn.OpenAsync();
        await using var multi = await conn.QueryMultipleAsync(sql);

        var tiposSancion = (await multi.ReadAsync<TipoSancionDto>()).ToList();
        var infraccionesTipo = (await multi.ReadAsync<InfraccionTipoDto>()).ToList();
        var racInfracciones = (await multi.ReadAsync<AmonCatalogoDto>()).ToList();
        var proyectos = (await multi.ReadAsync<AmonCatalogoDto>()).ToList();
        var partidas = (await multi.ReadAsync<AmonPartidaDto>()).ToList();
        var uitActual = await multi.ReadFirstOrDefaultAsync<decimal?>() ?? 0m;

        return new AmonestacionInitDto
        {
            TiposSancion = tiposSancion,
            InfraccionesTipo = infraccionesTipo,
            RacInfracciones = racInfracciones,
            Proyectos = proyectos,
            Partidas = partidas,
            UitActual = uitActual
        };
    }

    public async Task<string> GenerarCodigoAsync(int proyectoId)
    {
        const string sql = """
            SELECT COALESCE(abbreviation, 'XXX') FROM project WHERE project_id = @pid;
            SELECT COUNT(*) FROM ssoma_amonestaciones WHERE proyecto_id = @pid;
            """;

        await using var conn = Conn();
        await conn.OpenAsync();
        await using var multi = await conn.QueryMultipleAsync(sql, new { pid = proyectoId });
        var abbrev = ((await multi.ReadFirstAsync<string>()) ?? "XXX").ToUpperInvariant();
        var count = await multi.ReadFirstAsync<long>();
        var correlativo = (count + 1).ToString("D3");
        return $"{abbrev}-AMON-{correlativo}";
    }

    public async Task<int> CrearAsync(AmonestacionDetalleDto detalle, List<(string Base64, string NombreArchivo, string Url)> fotos)
    {
        using var ctx = _factory.CreateDbContext();

        var entity = new SsomaAmonestacion
        {
            Codigo               = detalle.Codigo,
            ProyectoId           = detalle.ProyectoId,
            Fecha                = DateTime.SpecifyKind(detalle.Fecha, DateTimeKind.Utc),
            WorkerId             = detalle.WorkerId,
            PartidaId            = detalle.PartidaId,
            TipoSancionId        = detalle.TipoSancionId,
            InfraccionTipoId     = detalle.InfraccionTipoId,
            Descripcion          = detalle.Descripcion,
            AplicaPenalizacion   = detalle.AplicaPenalizacion,
            SancionInfraccionId  = detalle.SancionInfraccionId,
            MontoCalculado       = detalle.MontoCalculado,
            UitReferencia        = 0m,
            PuntosInfraccion     = detalle.PuntosInfraccion,
            DiasSuspension       = detalle.DiasSuspension,
            FechaInicioSuspension = detalle.FechaInicioSuspension,
            FechaFinSuspension   = detalle.FechaFinSuspension,
            PersonaReportaId     = detalle.PersonaReportaId,
            Estado               = detalle.Estado,
            CreatedBy            = detalle.PersonaReportaId,
            State                = true
        };

        ctx.SsomaAmonestaciones.Add(entity);
        await ctx.SaveChangesAsync();

        for (int i = 0; i < fotos.Count; i++)
        {
            ctx.SsomaAmonestacionFotos.Add(new SsomaAmonestacionFoto
            {
                AmonestacionId = entity.Id,
                Url            = fotos[i].Url,
                NombreArchivo  = fotos[i].NombreArchivo,
                Base64Data     = fotos[i].Base64,   // solo se guarda en borradores; null en registradas
                Orden          = i + 1
            });
        }

        await ctx.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<(List<AmonestacionListItemDto> Items, int Total)> GetListAsync(AmonestacionListQuery q)
    {
        var where = new List<string> { "a.state = true" };
        var p = new DynamicParameters();
        p.Add("offset", (q.Page - 1) * q.PageSize);
        p.Add("limit", q.PageSize <= 0 ? 20 : Math.Min(q.PageSize, 100));

        if (q.ProyectoId.HasValue)   { where.Add("a.proyecto_id = @pid");   p.Add("pid", q.ProyectoId); }
        if (q.WorkerId.HasValue)     { where.Add("a.worker_id = @wid");     p.Add("wid", q.WorkerId); }
        if (q.TipoSancionId.HasValue){ where.Add("a.tipo_sancion_id = @tsid"); p.Add("tsid", q.TipoSancionId); }
        if (q.FechaDesde.HasValue)   { where.Add("a.fecha >= @fd"); p.Add("fd", q.FechaDesde.Value.Date); }
        if (q.FechaHasta.HasValue)   { where.Add("a.fecha <= @fh"); p.Add("fh", q.FechaHasta.Value.Date); }
        if (!string.IsNullOrWhiteSpace(q.WorkerSearch))
        {
            where.Add("(pe.document_identity_code ILIKE @ws OR pe.full_name ILIKE @ws)");
            p.Add("ws", $"%{q.WorkerSearch.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(q.EmpresaNombre))
        {
            where.Add("c.contributor_name ILIKE @en");
            p.Add("en", $"%{q.EmpresaNombre.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(q.Estado))
        {
            where.Add("a.estado = @estado");
            p.Add("estado", q.Estado.Trim());
        }
        if (q.EmpresaIdContratista.HasValue)
        {
            where.Add("wv.empresa_id = @empresaIdContratista");
            p.Add("empresaIdContratista", q.EmpresaIdContratista.Value);
        }

        var whereClause = "WHERE " + string.Join(" AND ", where);

        var sql = $"""
            SELECT DISTINCT ON (a.id)
                a.id, a.codigo,
                pr.project_description AS proyectoNombre,
                a.fecha,
                pe.full_name AS workerNombre,
                pe.document_identity_code AS workerDni,
                c.contributor_name AS empresaNombre,
                ts.nombre AS tipoSancionNombre,
                ts.nivel_gravedad AS nivelGravedad,
                it.nombre AS infraccionTipoNombre,
                a.descripcion,
                a.puntos_infraccion AS puntosInfraccion,
                a.aplica_penalizacion AS aplicaPenalizacion,
                a.monto_calculado AS montoCalculado,
                a.estado
            FROM ssoma_amonestaciones a
            JOIN project pr ON pr.project_id = a.proyecto_id
            JOIN workers w ON w.id = a.worker_id AND w.state
            JOIN person pe ON pe.person_id = w.person_id
            LEFT JOIN worker_vinculaciones wv ON wv.worker_id = w.id
                AND (wv.fecha_fin IS NULL OR wv.fecha_fin > CURRENT_DATE)
            LEFT JOIN contributor c ON c.contributor_id = wv.empresa_id
            JOIN ssoma_amonestacion_tipo_sanciones ts ON ts.id = a.tipo_sancion_id
            LEFT JOIN ssoma_amonestacion_infraccion_tipos it ON it.id = a.infraccion_tipo_id
            {whereClause}
            ORDER BY a.id DESC, a.fecha DESC
            LIMIT @limit OFFSET @offset;

            SELECT COUNT(*) FROM ssoma_amonestaciones a
            JOIN workers w ON w.id = a.worker_id AND w.state
            JOIN person pe ON pe.person_id = w.person_id
            LEFT JOIN worker_vinculaciones wv ON wv.worker_id = w.id
                AND (wv.fecha_fin IS NULL OR wv.fecha_fin > CURRENT_DATE)
            LEFT JOIN contributor c ON c.contributor_id = wv.empresa_id
            {whereClause};
            """;

        await using var conn = Conn();
        await conn.OpenAsync();
        await using var multi = await conn.QueryMultipleAsync(sql, p);
        var items = (await multi.ReadAsync<AmonestacionListItemDto>()).ToList();
        var total = await multi.ReadSingleAsync<int>();
        return (items, total);
    }

    public async Task<AmonestacionDetalleDto?> GetDetalleAsync(int id)
    {
        const string sql = """
            SELECT
                a.id, a.codigo,
                a.proyecto_id AS proyectoId,
                pr.project_description AS proyectoNombre,
                a.fecha,
                a.worker_id AS workerId,
                pe.full_name AS workerNombre,
                pe.document_identity_code AS workerDni,
                wcat.nombre AS workerCategoria,
                wpue.nombre AS workerCargo,
                CASE WHEN pe.fecha_nacimiento IS NOT NULL
                     THEN DATE_PART('year', AGE(pe.fecha_nacimiento))::int ELSE NULL END AS workerEdad,
                c.contributor_id AS empresaId,
                COALESCE(c.contributor_name, '') AS empresaNombre,
                COALESCE(c.es_abril, false) AS esEmpresaAbril,
                ct.logo_file_url AS empresaLogoUrl,
                a.partida_id AS partidaId,
                pt.partida_description AS partidaNombre,
                a.tipo_sancion_id AS tipoSancionId,
                ts.nombre AS tipoSancionNombre,
                ts.nivel_gravedad AS nivelGravedad,
                ts.genera_suspension AS generaSuspension,
                a.infraccion_tipo_id AS infraccionTipoId,
                it.nombre AS infraccionTipoNombre,
                a.descripcion,
                a.aplica_penalizacion AS aplicaPenalizacion,
                a.sancion_infraccion_id AS sancionInfraccionId,
                ri.nombre AS sancionInfraccionNombre,
                a.monto_calculado AS montoCalculado,
                a.puntos_infraccion AS puntosInfraccion,
                a.dias_suspension AS diasSuspension,
                a.fecha_inicio_suspension AS fechaInicioSuspension,
                a.fecha_fin_suspension AS fechaFinSuspension,
                up.full_name AS personaReportaNombre,
                a.pdf_url AS pdfUrl,
                a.estado,
                a.documento_firmado_url AS documentoFirmadoUrl,
                a.fecha_cierre AS fechaCierre,
                a.created_at AS createdAt,
                COALESCE((SELECT SUM(a2.puntos_infraccion) FROM ssoma_amonestaciones a2
                           WHERE a2.worker_id = a.worker_id AND a2.state = true), 0)::int AS puntosAcumulados,
                EXISTS(SELECT 1 FROM ssoma_inhabilitacion si
                        WHERE si.worker_id = a.worker_id AND si.fecha_fin IS NULL) AS inhabilitado
            FROM ssoma_amonestaciones a
            JOIN project pr ON pr.project_id = a.proyecto_id
            JOIN workers w ON w.id = a.worker_id AND w.state
            JOIN person pe ON pe.person_id = w.person_id
            LEFT JOIN puesto wpue ON wpue.puesto_id = w.puesto_id
            LEFT JOIN categoria wcat ON wcat.categoria_id = wpue.categoria_id
            LEFT JOIN worker_vinculaciones wv ON wv.worker_id = w.id
                AND (wv.fecha_fin IS NULL OR wv.fecha_fin > CURRENT_DATE)
            LEFT JOIN contributor c ON c.contributor_id = wv.empresa_id
            LEFT JOIN contractor ct ON ct.contractor_id = c.contributor_id
            LEFT JOIN partida pt ON pt.partida_id = a.partida_id
            JOIN ssoma_amonestacion_tipo_sanciones ts ON ts.id = a.tipo_sancion_id
            JOIN ssoma_amonestacion_infraccion_tipos it ON it.id = a.infraccion_tipo_id
            LEFT JOIN ssoma_rac_infraccion ri ON ri.id = a.sancion_infraccion_id
            LEFT JOIN app_user au ON au.user_id = a.persona_reporta_id
            LEFT JOIN person up ON LOWER(up.email) = LOWER(au.email)
            WHERE a.id = @id AND a.state = true;

            SELECT id, url, nombre_archivo AS nombreArchivo, orden, base64data AS base64Data
            FROM ssoma_amonestacion_fotos WHERE amonestacion_id = @id ORDER BY orden;
            """;

        await using var conn = Conn();
        await conn.OpenAsync();
        await using var multi = await conn.QueryMultipleAsync(sql, new { id });

        var detalle = await multi.ReadFirstOrDefaultAsync<AmonestacionDetalleDto>();
        if (detalle is null) return null;

        detalle.Fotos = (await multi.ReadAsync<AmonFotoDto>()).ToList();
        return detalle;
    }

    public async Task<AmonestacionDashboardDto> GetDashboardAsync(int? empresaIdContratista = null)
    {
        // Cuando el usuario es contratista, acota TODAS las subconsultas a los
        // trabajadores vinculados actualmente a su empresa.
        var filtroEmpresa = empresaIdContratista.HasValue
            ? """
              AND EXISTS (
                  SELECT 1 FROM worker_vinculaciones wve
                  WHERE wve.worker_id = a.worker_id
                    AND wve.empresa_id = @empresaIdContratista
                    AND (wve.fecha_fin IS NULL OR wve.fecha_fin > CURRENT_DATE)
              )
              """
            : "";

        var sql = $"""
            -- 1) KPIs
            SELECT
                COUNT(*)::int AS totalAmonestaciones,
                COUNT(CASE WHEN puntos_acc >= 5 AND puntos_acc < 10 THEN 1 END)::int AS trabajadoresConMas5Puntos,
                COUNT(CASE WHEN puntos_acc >= 10 THEN 1 END)::int AS trabajadoresInhabilitados,
                COUNT(CASE WHEN EXTRACT(MONTH FROM fecha_mes) = EXTRACT(MONTH FROM CURRENT_DATE)
                            AND EXTRACT(YEAR FROM fecha_mes) = EXTRACT(YEAR FROM CURRENT_DATE) THEN 1 END)::int AS amonestacionesMesActual,
                COUNT(CASE WHEN estado_a = 'Borrador' THEN 1 END)::int AS borradorPendientes,
                COUNT(CASE WHEN estado_a = 'Registrada' THEN 1 END)::int AS pendientesCierre,
                COUNT(CASE WHEN estado_a = 'Registrada' THEN 1 END)::int AS amonestacionesRegistradas,
                COUNT(CASE WHEN estado_a = 'Cerrada' THEN 1 END)::int AS amonestacionesCerradas
            FROM (
                SELECT a.id, a.fecha AS fecha_mes, a.estado AS estado_a,
                    SUM(a2.puntos_infraccion) OVER (PARTITION BY a.worker_id) AS puntos_acc
                FROM ssoma_amonestaciones a
                JOIN ssoma_amonestaciones a2 ON a2.worker_id = a.worker_id AND a2.state = true
                WHERE a.state = true {filtroEmpresa}
            ) sub;

            -- 2) Por tipo
            SELECT ts.nombre AS tipoNombre, COUNT(*)::int AS total
            FROM ssoma_amonestaciones a
            JOIN ssoma_amonestacion_tipo_sanciones ts ON ts.id = a.tipo_sancion_id
            WHERE a.state = true {filtroEmpresa}
            GROUP BY ts.nombre ORDER BY total DESC;

            -- 3) Matriz proyecto × tipo (filas planas, se agrupa en C#)
            SELECT pr.project_description AS proyectoNombre,
                   ts.nombre AS tipoNombre,
                   COUNT(*)::int AS total
            FROM ssoma_amonestaciones a
            JOIN project pr ON pr.project_id = a.proyecto_id
            JOIN ssoma_amonestacion_tipo_sanciones ts ON ts.id = a.tipo_sancion_id
            WHERE a.state = true {filtroEmpresa}
            GROUP BY pr.project_description, ts.nombre
            ORDER BY pr.project_description, total DESC;

            -- 4) Tendencia por proyecto (12 meses año actual)
            SELECT EXTRACT(MONTH FROM a.fecha)::int AS mes,
                   pr.project_description AS tipoNombre,
                   COUNT(*)::int AS total
            FROM ssoma_amonestaciones a
            JOIN project pr ON pr.project_id = a.proyecto_id
            WHERE a.state = true {filtroEmpresa}
              AND EXTRACT(YEAR FROM a.fecha) = EXTRACT(YEAR FROM CURRENT_DATE)::int
            GROUP BY mes, pr.project_description
            ORDER BY mes, total DESC;

            -- 5) Últimos 8 sancionados
            SELECT a.id, a.codigo,
                   pe.full_name AS workerNombre,
                   pe.document_identity_code AS workerDni,
                   COALESCE(c.contributor_name, '') AS empresaNombre,
                   pr.project_description AS proyectoNombre,
                   ts.nombre AS tipoSancionNombre,
                   ts.nivel_gravedad AS nivelGravedad,
                   a.puntos_infraccion AS puntosInfraccion,
                   a.fecha,
                   a.estado
            FROM ssoma_amonestaciones a
            JOIN project pr ON pr.project_id = a.proyecto_id
            JOIN workers w ON w.id = a.worker_id AND w.state
            JOIN person pe ON pe.person_id = w.person_id
            LEFT JOIN worker_vinculaciones wv ON wv.worker_id = w.id
                AND (wv.fecha_fin IS NULL OR wv.fecha_fin > CURRENT_DATE)
            LEFT JOIN contributor c ON c.contributor_id = wv.empresa_id
            JOIN ssoma_amonestacion_tipo_sanciones ts ON ts.id = a.tipo_sancion_id
            WHERE a.state = true {filtroEmpresa}
            ORDER BY a.fecha DESC, a.id DESC
            LIMIT 8;
            """;

        await using var conn = Conn();
        await conn.OpenAsync();
        await using var multi = await conn.QueryMultipleAsync(sql, new { empresaIdContratista });

        var resumen = await multi.ReadFirstAsync<AmonestacionDashboardDto>();
        resumen.PorTipoSancion = (await multi.ReadAsync<AmonPorTipoDto>()).ToList();

        // Agrupar filas planas en matriz proyecto × tipo
        var matrizRaw = (await multi.ReadAsync<(string ProyectoNombre, string TipoNombre, int Total)>()).ToList();
        resumen.MatrizProyecto = matrizRaw
            .GroupBy(r => r.ProyectoNombre)
            .Select(g => new AmonMatrizProyectoDto
            {
                ProyectoNombre = g.Key,
                Total          = g.Sum(r => r.Total),
                PorTipo        = g.Select(r => new AmonCeldaTipoDto { TipoNombre = r.TipoNombre, Total = r.Total }).ToList()
            })
            .OrderByDescending(p => p.Total)
            .ToList();

        // Tendencia por proyecto — 12 meses fijos
        var tendRaw = (await multi.ReadAsync<(int Mes, string TipoNombre, int Total)>()).ToList();
        resumen.TendenciaMeses = Enumerable.Range(1, 12).Select(mes =>
        {
            var filas = tendRaw.Where(r => r.Mes == mes).ToList();
            return new AmonTendenciaMesDto
            {
                Mes         = mes,
                Total       = filas.Sum(r => r.Total),
                PorProyecto = filas.Select(r => new AmonCeldaTipoDto { TipoNombre = r.TipoNombre, Total = r.Total }).ToList()
            };
        }).ToList();

        // Últimos sancionados
        resumen.UltimosSancionados = (await multi.ReadAsync<AmonUltimoSancionadoDto>()).ToList();

        return resumen;
    }

    public async Task<WorkerPuntajeDto?> GetPuntajeWorkerAsync(int workerId)
    {
        const string sqlWorker = """
            SELECT w.id AS workerId,
                   pe.full_name AS nombre,
                   pe.document_identity_code AS dni,
                   c.contributor_id AS empresaId,
                   COALESCE(c.contributor_name, '') AS empresaNombre,
                   COALESCE(SUM(a.puntos_infraccion), 0)::int AS puntosAcumulados,
                   EXISTS(SELECT 1 FROM ssoma_inhabilitacion si
                           WHERE si.worker_id = w.id AND si.fecha_fin IS NULL) AS inhabilitado
            FROM workers w
            JOIN person pe ON pe.person_id = w.person_id
            LEFT JOIN worker_vinculaciones wv ON wv.worker_id = w.id
                AND (wv.fecha_fin IS NULL OR wv.fecha_fin > CURRENT_DATE)
            LEFT JOIN contributor c ON c.contributor_id = wv.empresa_id
            LEFT JOIN ssoma_amonestaciones a ON a.worker_id = w.id AND a.state = true
            WHERE w.state AND w.id = @wid
            GROUP BY w.id, pe.full_name, pe.document_identity_code, c.contributor_id, c.contributor_name;
            """;

        await using var conn = Conn();
        await conn.OpenAsync();

        var workerDto = await conn.QueryFirstOrDefaultAsync<WorkerPuntajeDto>(sqlWorker, new { wid = workerId });
        if (workerDto is null) return null;

        // Historial de amonestaciones
        var (historial, _) = await GetListAsync(new AmonestacionListQuery { WorkerId = workerId, PageSize = 100 });
        workerDto.Historial = historial;

        return workerDto;
    }

    public async Task GuardarPdfUrlAsync(int id, string url)
    {
        using var ctx = _factory.CreateDbContext();
        var entity = await ctx.SsomaAmonestaciones.FindAsync(id);
        if (entity is null) return;
        entity.PdfUrl = url;
        entity.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    public async Task CerrarAsync(int id, string documentoFirmadoUrl)
    {
        using var ctx = _factory.CreateDbContext();
        var entity = await ctx.SsomaAmonestaciones.FindAsync(id)
            ?? throw new Exception("Amonestación no encontrada.");
        entity.Estado = "Cerrada";
        entity.DocumentoFirmadoUrl = documentoFirmadoUrl;
        entity.FechaCierre = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    public async Task<List<(byte[] Bytes, string Nombre)>> GetFotosBytesAsync(int id)
    {
        using var ctx = _factory.CreateDbContext();
        var fotos = await ctx.SsomaAmonestacionFotos
            .Where(f => f.AmonestacionId == id && f.Base64Data != null)
            .OrderBy(f => f.Orden)
            .ToListAsync();

        var result = new List<(byte[] Bytes, string Nombre)>();
        foreach (var f in fotos)
        {
            try
            {
                var b64 = f.Base64Data!.Contains(',') ? f.Base64Data.Split(',')[1] : f.Base64Data;
                result.Add((Convert.FromBase64String(b64), f.NombreArchivo ?? "foto.jpg"));
            }
            catch { /* foto corrupta, ignorar */ }
        }
        return result;
    }

    public async Task LimpiarBase64FotosAsync(int id)
    {
        using var ctx = _factory.CreateDbContext();
        var fotos = await ctx.SsomaAmonestacionFotos
            .Where(f => f.AmonestacionId == id)
            .ToListAsync();
        foreach (var f in fotos)
            f.Base64Data = null;
        await ctx.SaveChangesAsync();
    }

    public async Task ConfirmarEstadoAsync(int id)
    {
        using var ctx = _factory.CreateDbContext();
        var entity = await ctx.SsomaAmonestaciones.FindAsync(id)
            ?? throw new Exception("Amonestación no encontrada.");
        entity.Estado = "Registrada";
        entity.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    public async Task<(int WorkerId, string CambiosResumen)> EditarAsync(int id, AmonestacionEditRequest req, int userId)
    {
        using var ctx = _factory.CreateDbContext();
        var entity = await ctx.SsomaAmonestaciones.FirstOrDefaultAsync(a => a.Id == id && a.State)
            ?? throw new Exception("Amonestación no encontrada.");

        var cambios = new List<string>();

        if (req.TipoSancionId.HasValue && req.TipoSancionId != entity.TipoSancionId)
        {
            cambios.Add($"TipoSancionId: {entity.TipoSancionId} -> {req.TipoSancionId}");
            entity.TipoSancionId = req.TipoSancionId.Value;
        }
        if (req.InfraccionTipoId.HasValue && req.InfraccionTipoId != entity.InfraccionTipoId)
        {
            cambios.Add($"InfraccionTipoId: {entity.InfraccionTipoId} -> {req.InfraccionTipoId}");
            entity.InfraccionTipoId = req.InfraccionTipoId.Value;
        }
        if (req.Descripcion is not null && req.Descripcion != entity.Descripcion)
        {
            cambios.Add("Descripción corregida");
            entity.Descripcion = req.Descripcion;
        }
        if (req.PuntosInfraccion.HasValue && req.PuntosInfraccion != entity.PuntosInfraccion)
        {
            cambios.Add($"PuntosInfraccion: {entity.PuntosInfraccion} -> {req.PuntosInfraccion}");
            entity.PuntosInfraccion = req.PuntosInfraccion.Value;
        }
        if (req.AplicaPenalizacion.HasValue && req.AplicaPenalizacion != entity.AplicaPenalizacion)
        {
            cambios.Add($"AplicaPenalizacion: {entity.AplicaPenalizacion} -> {req.AplicaPenalizacion}");
            entity.AplicaPenalizacion = req.AplicaPenalizacion.Value;
        }
        if (req.SancionInfraccionId.HasValue && req.SancionInfraccionId != entity.SancionInfraccionId)
        {
            cambios.Add($"SancionInfraccionId: {entity.SancionInfraccionId} -> {req.SancionInfraccionId}");
            entity.SancionInfraccionId = req.SancionInfraccionId;
        }
        if (req.DiasSuspension.HasValue && req.DiasSuspension != entity.DiasSuspension)
        {
            cambios.Add($"DiasSuspension: {entity.DiasSuspension} -> {req.DiasSuspension}");
            entity.DiasSuspension = req.DiasSuspension;
        }
        if (req.FechaInicioSuspension is not null && DateOnly.TryParse(req.FechaInicioSuspension, out var fi))
            entity.FechaInicioSuspension = fi;
        if (req.FechaFinSuspension is not null && DateOnly.TryParse(req.FechaFinSuspension, out var ff))
            entity.FechaFinSuspension = ff;

        entity.UpdatedBy = userId > 0 ? userId : null;
        entity.UpdatedAt = DateTime.UtcNow;

        await ctx.SaveChangesAsync();

        var resumen = cambios.Count > 0 ? string.Join("; ", cambios) : "Sin cambios detectados";
        return (entity.WorkerId, resumen);
    }
}

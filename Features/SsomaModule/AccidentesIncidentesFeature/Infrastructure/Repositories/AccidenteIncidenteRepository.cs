using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.AccidentesIncidentesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.AccidentesIncidentesFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.AccidentesIncidentesFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Constants;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Abril_Backend.Features.SsomaModule.AccidentesIncidentesFeature.Infrastructure.Repositories;

public class AccidenteIncidenteRepository : IAccidenteIncidenteRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IConfiguration _config;

    public AccidenteIncidenteRepository(IDbContextFactory<AppDbContext> factory, IConfiguration config)
    {
        _factory = factory;
        _config = config;
    }

    private NpgsqlConnection Conn() => new(_config["Database:PostgreSQL"]);

    public async Task<FlashReportInicializarDto> GetInicializarAsync()
    {
        // Interpolada, no `const`: un `const string` se lleva el `{WorkersEstadoIds.Activo}` tal cual
        // al servidor y Postgres revienta con un error de sintaxis en tiempo de ejecucion, sin que el
        // compilador diga nada. El `$` obliga a que deje de ser const, asi que no puede volver a pasar.
        var sql = $"""
            SELECT DISTINCT p.project_id AS id, p.project_description AS nombre, p.abbreviation AS abreviatura, p.email_coord_ssoma AS emailCoordsoma
            FROM project p
            LEFT JOIN ss_proyecto_habilitado h ON h.proyecto_id = p.project_id AND h.state = true AND h.active = true
            WHERE (p.active = true AND NOT EXISTS (
                    SELECT 1 FROM proyecto_filtro f
                    JOIN proyecto_filtro_funcionalidad fn ON fn.id = f.funcionalidad_id
                    WHERE f.project_id = p.project_id
                      AND fn.codigo = 'SSOMA_ACCIDENTES' AND f.active = false))
               OR h.id IS NOT NULL
            ORDER BY p.project_description;
            SELECT id, nombre, codigo FROM ssoma_flash_tipo ORDER BY orden;
            SELECT id, nombre FROM ssoma_flash_etapa_proyecto ORDER BY nombre;
            SELECT id, nombre FROM ssoma_flash_parte_afectada ORDER BY nombre;
            SELECT contributor_id AS id, contributor_name AS nombre FROM contributor WHERE active = true AND es_abril = true ORDER BY contributor_name;
            SELECT partida_id AS id, partida_description AS nombre FROM partida WHERE active = true ORDER BY partida_description;
            SELECT contributor_id AS id, contributor_name AS razon_social, contributor_ruc AS ruc FROM contributor WHERE active = true AND es_abril = false ORDER BY contributor_name;
            SELECT w.id, p.full_name AS nombre_completo, p.document_identity_code AS documento,
                   pu.nombre AS cargo,
                   CASE WHEN p.fecha_nacimiento IS NOT NULL THEN DATE_PART('year', AGE(p.fecha_nacimiento))::int ELSE NULL END AS edad,
                   w.anios_experiencia, w.contributor_id
            FROM workers w
            JOIN person p ON p.person_id = w.person_id
            LEFT JOIN puesto pu ON pu.puesto_id = w.puesto_id
            WHERE w.state AND w.workers_estado_id = {WorkersEstadoIds.Activo} ORDER BY p.full_name;
            SELECT psc.project_id, c.contributor_id
            FROM project_sub_contractor psc JOIN contractor c ON c.contractor_id = psc.contractor_id WHERE c.active = true;
            """;

        await using var conn = Conn();
        await conn.OpenAsync();
        await using var multi = await conn.QueryMultipleAsync(sql);

        var proyectos = (await multi.ReadAsync<FlashProyectoDto>()).ToList();
        var tipos = (await multi.ReadAsync<CatalogoItemDto>()).ToList();
        var etapas = (await multi.ReadAsync<CatalogoItemDto>()).ToList();
        var partes = (await multi.ReadAsync<CatalogoItemDto>()).ToList();
        var empresas = (await multi.ReadAsync<CatalogoItemDto>()).ToList();
        var partidas = (await multi.ReadAsync<CatalogoItemDto>()).ToList();
        var contratistas = (await multi.ReadAsync<ContratistaCatalogoDto>()).ToList();
        var trabajadores = (await multi.ReadAsync<TrabajadorCatalogoDto>()).ToList();
        var proyectoContratistas = (await multi.ReadAsync<ProyectoContratistaDto>()).ToList();

        return new FlashReportInicializarDto
        {
            Proyectos = proyectos,
            Tipos = tipos,
            EtapasProyecto = etapas,
            PartesAfectadas = partes,
            EmpresasAbril = empresas,
            Partidas = partidas,
            Contratistas = contratistas,
            Trabajadores = trabajadores,
            ProyectoContratistas = proyectoContratistas
        };
    }

    public async Task<(List<FlashReportListItemDto> Items, int Total)> GetListAsync(
        int? proyectoId, int? tipoId, string? estado,
        DateTime? fechaDesde, DateTime? fechaHasta,
        bool? soloEnviados, string? areaOrigen, int page, int pageSize)
    {
        var where = new List<string>();
        var p = new DynamicParameters();
        p.Add("offset", (page - 1) * pageSize);
        p.Add("limit", pageSize);

        if (proyectoId.HasValue)  { where.Add("a.proyecto_id = @pid"); p.Add("pid", proyectoId); }
        if (tipoId.HasValue)      { where.Add("a.tipo_id = @tid"); p.Add("tid", tipoId); }
        if (!string.IsNullOrEmpty(estado)) { where.Add("a.estado = @estado"); p.Add("estado", estado); }
        if (fechaDesde.HasValue)  { where.Add("a.fecha >= @fd"); p.Add("fd", fechaDesde.Value.Date); }
        if (fechaHasta.HasValue)  { where.Add("a.fecha <= @fh"); p.Add("fh", fechaHasta.Value.Date); }
        if (soloEnviados == true) { where.Add("a.enviado = true"); }
        if (!string.IsNullOrEmpty(areaOrigen)) { where.Add("a.area_origen = @area"); p.Add("area", areaOrigen); }

        var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";

        var sql = $"""
            SELECT
                a.id, a.codigo, p.project_description AS proyecto_nombre, a.fecha,
                t.nombre AS tipo_nombre, t.codigo AS tipo_codigo, a.area_origen,
                COALESCE(a.trabajador_nombre, (
                    SELECT t.trabajador_nombre FROM ssoma_accidente_trabajador t
                    WHERE t.accidente_incidente_id = a.id ORDER BY t.id LIMIT 1
                )) AS trabajador_nombre,
                a.estado, a.enviado, a.fecha_envio,
                a.consecuencia_real_personal, a.consecuencia_potencial_personal, a.descripcion,
                at_vinc.id AS accidente_trabajo_id,
                CASE WHEN at_vinc.id IS NULL THEN NULL
                     ELSE (at_vinc.estado = 'Cerrado' AND at_vinc.fecha_alta IS NOT NULL)
                END AS cerrado_con_alta_medica,
                COALESCE((
                    SELECT SUM((d.fecha_fin::date - d.fecha_inicio::date) + 1)
                    FROM ssoma_flash_descanso d
                    WHERE d.accidente_incidente_id = a.id
                ), 0) AS dias_perdidos
            FROM ss_accidente_incidente a
            JOIN project p ON p.project_id = a.proyecto_id
            JOIN ssoma_flash_tipo t ON t.id = a.tipo_id
            LEFT JOIN ss_accidente_trabajo at_vinc ON at_vinc.flash_report_id = a.id
            {whereClause}
            ORDER BY a.fecha DESC, a.id DESC
            LIMIT @limit OFFSET @offset;

            SELECT COUNT(*) FROM ss_accidente_incidente a {whereClause};
            """;

        await using var conn = Conn();
        await conn.OpenAsync();
        await using var multi = await conn.QueryMultipleAsync(sql, p);
        var items = (await multi.ReadAsync<FlashReportListItemDto>()).ToList();
        var total = await multi.ReadSingleAsync<int>();
        return (items, total);
    }

    public async Task<FlashReportDetalleDto?> GetDetalleAsync(int id)
    {
        const string sql = """
            SELECT
                a.id, a.codigo,
                a.proyecto_id, p.project_description AS proyecto_nombre, p.abbreviation AS proyecto_abreviatura,
                a.tipo_id, t.nombre AS tipo_nombre, t.codigo AS tipo_codigo, a.area_origen,
                a.fecha, a.hora, a.lugar_exacto, a.descripcion, a.estado,
                a.empresa_abril_id, ea.contributor_name AS empresa_abril_nombre,
                a.contributor_id, c.contributor_name AS contributor_nombre,
                a.jefe_inmediato_nombre,
                a.etapa_proyecto_id, ep.nombre AS etapa_proyecto_nombre,
                a.partida_id, pa.partida_description AS partida_nombre,
                a.worker_id, a.trabajador_nombre, a.puesto_trabajo, a.edad, a.anios_experiencia, a.celular_trabajador,
                a.parte_afectada_id, paf.nombre AS parte_afectada_nombre,
                a.turno, a.tipo_contacto, a.danio_proceso_flag, a.atencion_medica, a.centro_atencion,
                a.dano_proceso, a.consecuencia_real_personal, a.consecuencia_potencial_personal,
                a.acciones_inmediatas,
                a.elaborado_por_id, a.elaborado_por_nombre, a.elaborado_por_cargo, a.elaborado_por_email, a.elaborado_por_telefono,
                a.url_foto1, a.url_foto2,
                a.enviado, a.fecha_envio, a.url_pdf_sharepoint,
                a.created_at,
                at_vinc.id AS accidente_trabajo_id
            FROM ss_accidente_incidente a
            LEFT JOIN ss_accidente_trabajo at_vinc ON at_vinc.flash_report_id = a.id
            JOIN project p ON p.project_id = a.proyecto_id
            JOIN ssoma_flash_tipo t ON t.id = a.tipo_id
            LEFT JOIN contributor ea ON ea.contributor_id = a.empresa_abril_id
            LEFT JOIN contributor c ON c.contributor_id = a.contributor_id
            LEFT JOIN ssoma_flash_etapa_proyecto ep ON ep.id = a.etapa_proyecto_id
            LEFT JOIN partida pa ON pa.partida_id = a.partida_id
            LEFT JOIN ssoma_flash_parte_afectada paf ON paf.id = a.parte_afectada_id
            WHERE a.id = @id;

            SELECT id, fecha_inicio, fecha_fin, observacion FROM ssoma_flash_descanso WHERE accidente_incidente_id = @id ORDER BY fecha_inicio;

            SELECT t.id, t.worker_id, t.trabajador_nombre, t.puesto_trabajo, t.edad,
                   t.anios_experiencia, t.celular_trabajador, t.parte_afectada_id,
                   paf.nombre AS parte_afectada_nombre
            FROM ssoma_accidente_trabajador t
            LEFT JOIN ssoma_flash_parte_afectada paf ON paf.id = t.parte_afectada_id
            WHERE t.accidente_incidente_id = @id
            ORDER BY t.id;
            """;

        await using var conn = Conn();
        await conn.OpenAsync();
        await using var multi = await conn.QueryMultipleAsync(sql, new { id });
        var detalle = await multi.ReadFirstOrDefaultAsync<FlashReportDetalleDto>();
        if (detalle == null) return null;
        detalle.Descansos = (await multi.ReadAsync<DescansoDto>()).ToList();
        detalle.Trabajadores = (await multi.ReadAsync<TrabajadorAfectadoDto>()).ToList();
        return detalle;
    }

    public async Task<string> GenerarCodigoAsync(int proyectoId, string tipoCodigoCorto)
    {
        // El correlativo se calcula a partir del máximo número YA USADO dentro de
        // códigos que calzan con el patrón "{ABREV}-{TIPO}-NN" del proyecto/tipo actual,
        // no con COUNT(*) de filas: COUNT(*) se desalinea cuando hay registros legacy
        // (importados de SharePoint) que no siguen el patrón, o cuando se elimina un registro,
        // provocando que se reasigne un correlativo ya usado (código duplicado).
        const string sql = """
            SELECT COALESCE(p.abbreviation, 'XXX') FROM project p WHERE p.project_id = @pid;
            SELECT COALESCE(MAX((regexp_match(a.codigo, '-([0-9]+)$'))[1]::int), 0)
            FROM ss_accidente_incidente a
            JOIN project p ON p.project_id = a.proyecto_id
            WHERE a.proyecto_id = @pid
              AND a.tipo_id = (SELECT id FROM ssoma_flash_tipo WHERE codigo = @tipoCodigo)
              AND a.codigo ~ ('^' || UPPER(p.abbreviation) || '-' || @tipoCodigo || '-[0-9]+$');
            """;

        await using var conn = Conn();
        await conn.OpenAsync();
        await using var multi = await conn.QueryMultipleAsync(sql, new { pid = proyectoId, tipoCodigo = tipoCodigoCorto });
        var abrev = (await multi.ReadFirstAsync<string>()).ToUpperInvariant();
        var maxCorrelativo = await multi.ReadFirstAsync<int>();
        var correlativo = (maxCorrelativo + 1).ToString("D2");
        return $"{abrev}-{tipoCodigoCorto}-{correlativo}";
    }

    public async Task<int> CrearAsync(CrearFlashReportRequest req, string codigo, string? urlFoto1, string? urlFoto2, int? usuarioId, bool generarEntregables)
    {
        using var ctx = _factory.CreateDbContext();

        TimeSpan? hora = null;
        if (!string.IsNullOrWhiteSpace(req.Hora) && TimeSpan.TryParse(req.Hora, out var ts))
            hora = ts;

        var entity = new SsomaAccidenteIncidente
        {
            Codigo = codigo,
            ProyectoId = req.ProyectoId,
            TipoId = req.TipoId,
            AreaOrigen = string.IsNullOrWhiteSpace(req.AreaOrigen) ? "Produccion" : req.AreaOrigen,
            Fecha = DateTime.SpecifyKind(req.Fecha.Date, DateTimeKind.Utc),
            Hora = hora,
            LugarExacto = req.LugarExacto,
            Descripcion = req.Descripcion,
            Estado = "Borrador",
            EmpresaAbrilId = req.EmpresaAbrilId,
            ContributorId = req.ContributorId,
            JefeInmediatoNombre = req.JefeInmediatoNombre,
            EtapaProyectoId = req.EtapaProyectoId,
            PartidaId = req.PartidaId,
            WorkerId = req.WorkerId,
            TrabajadorNombre = req.TrabajadorNombre,
            PuestoTrabajo = req.PuestoTrabajo,
            Edad = req.Edad,
            AniosExperiencia = req.AniosExperiencia,
            CelularTrabajador = req.CelularTrabajador,
            ParteAfectadaId = req.ParteAfectadaId,
            Turno = req.Turno,
            TipoContacto = req.TipoContacto,
            DanioProcesoFlag = req.DanioProcesoFlag,
            AtencionMedica = req.AtencionMedica,
            CentroAtencion = req.CentroAtencion,
            DanoProceso = req.DanoProceso,
            ConsecuenciaRealPersonal = req.ConsecuenciaRealPersonal,
            ConsecuenciaPotencialPersonal = req.ConsecuenciaPotencialPersonal,
            AccionesInmediatas = req.AccionesInmediatas,
            ElaboradoPorId = usuarioId,
            ElaboradoPorNombre = req.ElaboradoPorNombre,
            ElaboradoPorCargo = req.ElaboradoPorCargo,
            ElaboradoPorEmail = req.ElaboradoPorEmail,
            ElaboradoPorTelefono = req.ElaboradoPorTelefono,
            UrlFoto1 = urlFoto1,
            UrlFoto2 = urlFoto2,
            Descansos = req.Descansos.Select(d => new SsomaFlashDescanso
            {
                FechaInicio = DateTime.SpecifyKind(d.FechaInicio.Date, DateTimeKind.Utc),
                FechaFin = DateTime.SpecifyKind(d.FechaFin.Date, DateTimeKind.Utc),
                Observacion = d.Observacion
            }).ToList()
        };

        ctx.Set<SsomaAccidenteIncidente>().Add(entity);
        await ctx.SaveChangesAsync();

        if (generarEntregables)
        {
            // Determinar qué entregables aplican según el tipo de evento
            var tipoCodigo = req.TipoId > 0
                ? (await ctx.Set<SsomaFlashTipo>().FindAsync(req.TipoId))?.Codigo ?? "AC"
                : "AC";
            var esIncidente = tipoCodigo.Equals("IN", StringComparison.OrdinalIgnoreCase);
            var potencial = req.ConsecuenciaPotencialPersonal ?? 0;
            var esHIPI = esIncidente && potencial >= 5;
            var esAccidenteGrave = !esIncidente && (req.ConsecuenciaRealPersonal ?? 0) >= 4;

            var tiposEntregable = await ctx.Set<SsomaEntregableTipo>()
                .Where(t => t.Activo)
                .OrderBy(t => t.Orden)
                .ToListAsync();

            var entregablesFiltrados = tiposEntregable.Where(t =>
            {
                var aplica = t.AplicaA ?? "Todos";
                if (aplica == "Todos") return true;
                if (aplica == "Accidente") return !esIncidente;
                if (aplica == "AccidenteGrave") return esAccidenteGrave;
                if (aplica == "AccidenteIncapac") return !esIncidente && (req.ConsecuenciaRealPersonal ?? 0) >= 3;
                if (aplica == "AccidenteHIPI") return !esIncidente || esHIPI;
                return true;
            }).ToList();

            var entregables = entregablesFiltrados.Select(t => new SsomaEntregable
            {
                AccidenteIncidenteId = entity.Id,
                TipoId = t.Id,
                Estado = "Pendiente"
            }).ToList();

            ctx.Set<SsomaEntregable>().AddRange(entregables);
            await ctx.SaveChangesAsync();
        }

        if (req.Trabajadores.Count > 0)
        {
            var trabajadores = req.Trabajadores.Select(t => new SsomaAccidenteTrabajador
            {
                AccidenteIncidenteId = entity.Id,
                WorkerId = t.WorkerId,
                TrabajadorNombre = t.TrabajadorNombre,
                PuestoTrabajo = t.PuestoTrabajo,
                Edad = t.Edad,
                AniosExperiencia = t.AniosExperiencia,
                CelularTrabajador = t.CelularTrabajador,
                ParteAfectadaId = t.ParteAfectadaId,
            });
            ctx.SsomaAccidenteTrabajador.AddRange(trabajadores);
            await ctx.SaveChangesAsync();
        }

        // Auto-crear RM-050 stub + 3 medidas de control por defecto
        var inv = new SsomaInvestigacionRm050 { AccidenteIncidenteId = entity.Id };
        ctx.Set<SsomaInvestigacionRm050>().Add(inv);
        await ctx.SaveChangesAsync();
        for (int i = 1; i <= 3; i++)
        {
            ctx.Set<SsomaAccionCorrectiva>().Add(new SsomaAccionCorrectiva
            {
                InvestigacionId = inv.Id,
                Descripcion = $"Medida de control {i}",
                Tipo = "Correctiva",
                Estado = "Pendiente",
            });
        }
        await ctx.SaveChangesAsync();

        return entity.Id;
    }

    public async Task ActualizarAsync(int id, ActualizarFlashReportRequest req, string? urlFoto1, string? urlFoto2)
    {
        using var ctx = _factory.CreateDbContext();
        var entity = await ctx.Set<SsomaAccidenteIncidente>()
            .Include(a => a.Descansos)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new AbrilException("Flash Report no encontrado.", 404);

        TimeSpan? hora = null;
        if (!string.IsNullOrWhiteSpace(req.Hora) && TimeSpan.TryParse(req.Hora, out var ts))
            hora = ts;

        entity.TipoId = req.TipoId;
        entity.AreaOrigen = string.IsNullOrWhiteSpace(req.AreaOrigen) ? "Produccion" : req.AreaOrigen;
        entity.Fecha = DateTime.SpecifyKind(req.Fecha.Date, DateTimeKind.Utc);
        entity.Hora = hora;
        entity.LugarExacto = req.LugarExacto;
        entity.Descripcion = req.Descripcion;
        entity.EmpresaAbrilId = req.EmpresaAbrilId;
        entity.ContributorId = req.ContributorId;
        entity.JefeInmediatoNombre = req.JefeInmediatoNombre;
        entity.EtapaProyectoId = req.EtapaProyectoId;
        entity.PartidaId = req.PartidaId;
        entity.WorkerId = req.WorkerId;
        entity.TrabajadorNombre = req.TrabajadorNombre;
        entity.PuestoTrabajo = req.PuestoTrabajo;
        entity.Edad = req.Edad;
        entity.AniosExperiencia = req.AniosExperiencia;
        entity.CelularTrabajador = req.CelularTrabajador;
        entity.ParteAfectadaId = req.ParteAfectadaId;
        entity.Turno = req.Turno;
        entity.TipoContacto = req.TipoContacto;
        entity.DanioProcesoFlag = req.DanioProcesoFlag;
        entity.AtencionMedica = req.AtencionMedica;
        entity.CentroAtencion = req.CentroAtencion;
        entity.DanoProceso = req.DanoProceso;
        entity.ConsecuenciaRealPersonal = req.ConsecuenciaRealPersonal;
        entity.ConsecuenciaPotencialPersonal = req.ConsecuenciaPotencialPersonal;
        entity.AccionesInmediatas = req.AccionesInmediatas;
        entity.ElaboradoPorNombre = req.ElaboradoPorNombre;
        entity.ElaboradoPorCargo = req.ElaboradoPorCargo;
        entity.ElaboradoPorEmail = req.ElaboradoPorEmail;
        entity.ElaboradoPorTelefono = req.ElaboradoPorTelefono;
        entity.UpdatedAt = DateTime.UtcNow;

        if (urlFoto1 != null) entity.UrlFoto1 = urlFoto1;
        if (urlFoto2 != null) entity.UrlFoto2 = urlFoto2;

        // Reemplazar descansos
        ctx.Set<SsomaFlashDescanso>().RemoveRange(entity.Descansos);
        entity.Descansos = req.Descansos.Select(d => new SsomaFlashDescanso
        {
            AccidenteIncidenteId = id,
            FechaInicio = DateTime.SpecifyKind(d.FechaInicio.Date, DateTimeKind.Utc),
            FechaFin = DateTime.SpecifyKind(d.FechaFin.Date, DateTimeKind.Utc),
            Observacion = d.Observacion
        }).ToList();

        // Reemplazar trabajadores afectados
        var existentes = ctx.SsomaAccidenteTrabajador.Where(t => t.AccidenteIncidenteId == id);
        ctx.SsomaAccidenteTrabajador.RemoveRange(existentes);
        if (req.Trabajadores.Count > 0)
        {
            var nuevos = req.Trabajadores.Select(t => new SsomaAccidenteTrabajador
            {
                AccidenteIncidenteId = id,
                WorkerId = t.WorkerId,
                TrabajadorNombre = t.TrabajadorNombre,
                PuestoTrabajo = t.PuestoTrabajo,
                Edad = t.Edad,
                AniosExperiencia = t.AniosExperiencia,
                CelularTrabajador = t.CelularTrabajador,
                ParteAfectadaId = t.ParteAfectadaId,
            });
            ctx.SsomaAccidenteTrabajador.AddRange(nuevos);
        }

        await ctx.SaveChangesAsync();
    }

    public async Task ActualizarSeveridadAsync(int id, int? consecuenciaRealPersonal, int? consecuenciaPotencialPersonal)
    {
        using var ctx = _factory.CreateDbContext();
        var entity = await ctx.Set<SsomaAccidenteIncidente>().FindAsync(id)
            ?? throw new AbrilException("Flash Report no encontrado.", 404);
        entity.ConsecuenciaRealPersonal = consecuenciaRealPersonal;
        entity.ConsecuenciaPotencialPersonal = consecuenciaPotencialPersonal;
        entity.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    public async Task MarcarEnviadoAsync(int id, string urlPdf)
    {
        using var ctx = _factory.CreateDbContext();
        var entity = await ctx.Set<SsomaAccidenteIncidente>().FindAsync(id)
            ?? throw new AbrilException("Flash Report no encontrado.", 404);
        entity.Enviado = true;
        entity.FechaEnvio = DateTime.UtcNow;
        entity.UrlPdfSharepoint = urlPdf;
        entity.Estado = "Enviado";
        entity.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    public async Task<int?> CrearAccidenteTrabajoVinculadoAsync(FlashReportDetalleDto fr, int registradoPorId)
    {
        using var ctx = _factory.CreateDbContext();

        // Evitar duplicado si ya existe uno vinculado
        var existe = await ctx.Set<Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models.SsAccidenteTrabajo>()
            .AnyAsync(a => a.FlashReportId == fr.Id);
        if (existe) return null;

        var primerTrabajador = fr.Trabajadores.Count > 0 ? fr.Trabajadores[0] : null;
        var workerId = primerTrabajador?.WorkerId ?? fr.WorkerId;
        if (workerId == null) return null;

        var accidente = new Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models.SsAccidenteTrabajo
        {
            WorkerId = workerId.Value,
            FechaAccidente = DateOnly.FromDateTime(fr.Fecha),
            HoraAccidente = fr.Hora.HasValue ? TimeOnly.FromTimeSpan(fr.Hora.Value) : null,
            ProyectoId = fr.ProyectoId,
            EmpresaId = fr.ContributorId,
            LugarAccidente = fr.LugarExacto,
            TipoAccidente = fr.TipoNombre,
            Mecanismo = fr.TipoContacto,
            ParteCuerpoAfectada = primerTrabajador?.ParteAfectadaNombre ?? fr.ParteAfectadaNombre,
            Descripcion = fr.Descripcion,
            DescripcionLesion = fr.DanoProceso,
            Estado = "Registrado",
            FlashReportId = fr.Id,
            RegistradoPorId = registradoPorId,
            RequiereReinduccion = true,
        };

        ctx.Set<Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models.SsAccidenteTrabajo>().Add(accidente);
        await ctx.SaveChangesAsync();
        return accidente.Id;
    }

    public async Task EliminarAsync(int id)
    {
        using var ctx = _factory.CreateDbContext();
        var entity = await ctx.Set<SsomaAccidenteIncidente>().FindAsync(id)
            ?? throw new AbrilException("Flash Report no encontrado.", 404);
        ctx.Set<SsomaAccidenteIncidente>().Remove(entity);
        await ctx.SaveChangesAsync();
    }

    public async Task<List<EntregableDto>> GetEntregablesAsync(int accidenteId)
    {
        const string sql = """
            SELECT e.id, e.tipo_id, t.nombre AS tipo_nombre, t.orden, e.estado,
                   e.fecha_limite, e.observacion, e.updated_at
            FROM ss_entregable e
            JOIN ss_entregable_tipo t ON t.id = e.tipo_id
            WHERE e.accidente_incidente_id = @id
              AND t.activo = true
            ORDER BY t.orden;

            SELECT r.id, r.entregable_id, r.worker_id, r.nombre
            FROM ss_entregable_responsable r
            JOIN ss_entregable e ON e.id = r.entregable_id
            WHERE e.accidente_incidente_id = @id;

            SELECT a.id, a.entregable_id, a.url_archivo, a.nombre_archivo, a.created_at
            FROM ss_entregable_archivo a
            JOIN ss_entregable e ON e.id = a.entregable_id
            WHERE e.accidente_incidente_id = @id
            ORDER BY a.created_at;
            """;

        await using var conn = Conn();
        await conn.OpenAsync();
        await using var multi = await conn.QueryMultipleAsync(sql, new { id = accidenteId });

        var entregables = (await multi.ReadAsync<EntregableDto>()).ToList();
        var responsables = (await multi.ReadAsync<dynamic>()).ToList();
        var archivos = (await multi.ReadAsync<dynamic>()).ToList();

        foreach (var e in entregables)
        {
            e.Responsables = responsables
                .Where(r => r.entregable_id == e.Id)
                .Select(r => new EntregableResponsableDto
                {
                    Id = r.id,
                    WorkerId = r.worker_id,
                    Nombre = r.nombre
                }).ToList();

            e.Archivos = archivos
                .Where(a => a.entregable_id == e.Id)
                .Select(a => new EntregableArchivoDto
                {
                    Id = a.id,
                    UrlArchivo = a.url_archivo,
                    NombreArchivo = a.nombre_archivo,
                    CreatedAt = a.created_at
                }).ToList();
        }

        return entregables;
    }

    public async Task ActualizarEntregableAsync(int entregableId, ActualizarEntregableRequest req)
    {
        using var ctx = _factory.CreateDbContext();
        var entity = await ctx.Set<SsomaEntregable>()
            .Include(e => e.Responsables)
            .FirstOrDefaultAsync(e => e.Id == entregableId)
            ?? throw new AbrilException("Entregable no encontrado.", 404);

        entity.Estado = req.Estado;
        entity.FechaLimite = req.FechaLimite;
        entity.Observacion = req.Observacion;
        entity.UpdatedAt = DateTime.UtcNow;

        ctx.Set<SsomaEntregableResponsable>().RemoveRange(entity.Responsables);
        entity.Responsables = req.Responsables
            .Select(n => new SsomaEntregableResponsable { EntregableId = entregableId, Nombre = n })
            .Concat(req.ResponsableWorkerIds.Select(wid => new SsomaEntregableResponsable
                { EntregableId = entregableId, WorkerId = wid, Nombre = "" }))
            .ToList();

        await ctx.SaveChangesAsync();
    }

    public async Task SubirArchivoEntregableAsync(int entregableId, string urlArchivo, string nombreArchivo)
    {
        using var ctx = _factory.CreateDbContext();
        var entity = await ctx.Set<SsomaEntregable>().FindAsync(entregableId)
            ?? throw new AbrilException("Entregable no encontrado.", 404);

        ctx.Set<SsomaEntregableArchivo>().Add(new SsomaEntregableArchivo
        {
            EntregableId = entregableId,
            UrlArchivo = urlArchivo,
            NombreArchivo = nombreArchivo,
        });

        if (entity.Estado == "Pendiente") entity.Estado = "Presentado";
        entity.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    public async Task EliminarArchivoEntregableAsync(int archivoId)
    {
        using var ctx = _factory.CreateDbContext();
        var entity = await ctx.Set<SsomaEntregableArchivo>().FindAsync(archivoId)
            ?? throw new AbrilException("Archivo no encontrado.", 404);
        ctx.Set<SsomaEntregableArchivo>().Remove(entity);
        await ctx.SaveChangesAsync();
    }

    // ── RM-050 ────────────────────────────────────────────────────────────────

    public async Task<Rm050Dto?> GetRm050Async(int accidenteId)
    {
        using var ctx = _factory.CreateDbContext();
        var inv = await ctx.Set<SsomaInvestigacionRm050>()
            .Include(i => i.AccionesCorrectivas)
            .FirstOrDefaultAsync(i => i.AccidenteIncidenteId == accidenteId);

        if (inv == null) return null;

        return new Rm050Dto
        {
            Id = inv.Id,
            DescripcionDetallada = inv.DescripcionDetallada,
            Mecanismo = inv.Mecanismo,
            AgenteCausante = inv.AgenteCausante,
            ActosSubestandar = inv.ActosSubestandar,
            CondicionesSubestandar = inv.CondicionesSubestandar,
            FactoresPersonales = inv.FactoresPersonales,
            FactoresTrabajo = inv.FactoresTrabajo,
            DiasPerdidos = inv.DiasPerdidos,
            TipoAccidente = inv.TipoAccidente,
            GravedadAccidente = inv.GravedadAccidente,
            NroTrabajadoresAfectados = inv.NroTrabajadoresAfectados,
            Testigos = inv.Testigos,
            ElaboradoPorNombre = inv.ElaboradoPorNombre,
            ElaboradoPorCargo = inv.ElaboradoPorCargo,
            ElaboradoPorFecha = inv.ElaboradoPorFecha,
            AprobadoPorNombre = inv.AprobadoPorNombre,
            AprobadoPorCargo = inv.AprobadoPorCargo,
            Estado = inv.Estado,
            UpdatedAt = inv.UpdatedAt,
            AccionesCorrectivas = inv.AccionesCorrectivas.Select(a => new AccionCorrectivaDto
            {
                Id = a.Id,
                Descripcion = a.Descripcion,
                Tipo = a.Tipo,
                ResponsableNombre = a.ResponsableNombre,
                ResponsableWorkerId = a.ResponsableWorkerId,
                FechaCompromiso = a.FechaCompromiso,
                FechaCumplimiento = a.FechaCumplimiento,
                Estado = a.Estado,
                EvidenciaUrl = a.EvidenciaUrl,
            }).ToList(),
        };
    }

    public async Task<List<AccionCorrectivaVencidaDto>> GetAccionesVencidasAsync()
    {
        const string sql = """
            SELECT
                ac.id AS accion_id,
                ai.id AS accidente_id,
                ai.codigo AS codigo_accidente,
                ac.descripcion,
                ac.tipo,
                ac.responsable_nombre,
                ac.fecha_compromiso,
                (CURRENT_DATE - ac.fecha_compromiso) AS dias_vencida,
                ac.estado
            FROM ss_accion_correctiva ac
            JOIN ss_investigacion_rm050 rm ON rm.id = ac.investigacion_id
            JOIN ss_accidente_incidente ai ON ai.id = rm.accidente_incidente_id
            WHERE ac.fecha_compromiso < CURRENT_DATE
              AND ac.estado NOT IN ('Cumplido', 'No aplica')
            ORDER BY ac.fecha_compromiso ASC
            """;

        await using var conn = Conn();
        await conn.OpenAsync();
        return (await conn.QueryAsync<AccionCorrectivaVencidaDto>(sql)).ToList();
    }

    public async Task<SsomaAccionCorrectiva?> GetAccionCorrectivaAsync(int accionId)
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.Set<SsomaAccionCorrectiva>().FindAsync(accionId);
    }

    public async Task GuardarRm050Async(int accidenteId, GuardarRm050Request req)
    {
        using var ctx = _factory.CreateDbContext();
        var inv = await ctx.Set<SsomaInvestigacionRm050>()
            .Include(i => i.AccionesCorrectivas)
            .FirstOrDefaultAsync(i => i.AccidenteIncidenteId == accidenteId);

        if (inv == null)
        {
            inv = new SsomaInvestigacionRm050 { AccidenteIncidenteId = accidenteId };
            ctx.Set<SsomaInvestigacionRm050>().Add(inv);
        }

        inv.DescripcionDetallada = req.DescripcionDetallada;
        inv.Mecanismo = req.Mecanismo;
        inv.AgenteCausante = req.AgenteCausante;
        inv.ActosSubestandar = req.ActosSubestandar;
        inv.CondicionesSubestandar = req.CondicionesSubestandar;
        inv.FactoresPersonales = req.FactoresPersonales;
        inv.FactoresTrabajo = req.FactoresTrabajo;
        inv.DiasPerdidos = req.DiasPerdidos;
        inv.TipoAccidente = req.TipoAccidente;
        inv.GravedadAccidente = req.GravedadAccidente;
        inv.NroTrabajadoresAfectados = req.NroTrabajadoresAfectados;
        inv.Testigos = req.Testigos;
        inv.ElaboradoPorNombre = req.ElaboradoPorNombre;
        inv.ElaboradoPorCargo = req.ElaboradoPorCargo;
        inv.ElaboradoPorFecha = req.ElaboradoPorFecha;
        inv.AprobadoPorNombre = req.AprobadoPorNombre;
        inv.AprobadoPorCargo = req.AprobadoPorCargo;
        inv.UpdatedAt = DateTime.UtcNow;

        // Reemplazar acciones correctivas
        ctx.Set<SsomaAccionCorrectiva>().RemoveRange(inv.AccionesCorrectivas);
        inv.AccionesCorrectivas = req.AccionesCorrectivas.Select(a => new SsomaAccionCorrectiva
        {
            Descripcion = a.Descripcion,
            Tipo = a.Tipo,
            ResponsableNombre = a.ResponsableNombre,
            ResponsableWorkerId = a.ResponsableWorkerId,
            FechaCompromiso = a.FechaCompromiso,
            FechaCumplimiento = a.FechaCumplimiento,
            Estado = a.Estado,
        }).ToList();

        await ctx.SaveChangesAsync();
    }

    public async Task ReclasificarTipoAsync(int id, int tipoId, string tipoCodigo, string tipoNombre)
    {
        using var ctx = _factory.CreateDbContext();

        var entity = await ctx.SsomaAccidenteIncidente.FindAsync(id)
            ?? throw new AbrilException("Flash Report no encontrado.", 404);

        if (entity.Enviado)
            throw new AbrilException("No se puede reclasificar un Flash Report ya enviado.", 400);

        entity.TipoId = tipoId;
        entity.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    // ── Medidas de control ────────────────────────────────────────────────────

    public async Task<List<AccionCorrectivaDto>> GetMedidasAsync(int accidenteId)
    {
        const string sql = """
            SELECT ac.id, ac.descripcion, ac.tipo, ac.responsable_nombre,
                   ac.responsable_worker_id, ac.fecha_compromiso, ac.fecha_cumplimiento,
                   ac.estado, ac.evidencia_url
            FROM ss_accion_correctiva ac
            JOIN ss_investigacion_rm050 inv ON inv.id = ac.investigacion_id
            WHERE inv.accidente_incidente_id = @accidenteId
            ORDER BY ac.id
            """;
        await using var conn = Conn();
        await conn.OpenAsync();
        var rows = await conn.QueryAsync<AccionCorrectivaDto>(sql, new { accidenteId });
        return rows.ToList();
    }

    public async Task<int> AddMedidaAsync(int accidenteId, GuardarAccionCorrectivaRequest req)
    {
        using var ctx = _factory.CreateDbContext();
        var inv = await ctx.Set<SsomaInvestigacionRm050>()
            .FirstOrDefaultAsync(i => i.AccidenteIncidenteId == accidenteId);
        if (inv == null)
        {
            inv = new SsomaInvestigacionRm050 { AccidenteIncidenteId = accidenteId };
            ctx.Set<SsomaInvestigacionRm050>().Add(inv);
            await ctx.SaveChangesAsync();
        }
        var accion = new SsomaAccionCorrectiva
        {
            InvestigacionId = inv.Id,
            Descripcion = req.Descripcion,
            Tipo = req.Tipo ?? "Correctiva",
            ResponsableNombre = req.ResponsableNombre,
            ResponsableWorkerId = req.ResponsableWorkerId,
            FechaCompromiso = req.FechaCompromiso,
            Estado = req.Estado ?? "Pendiente",
        };
        ctx.Set<SsomaAccionCorrectiva>().Add(accion);
        await ctx.SaveChangesAsync();
        return accion.Id;
    }

    public async Task UpdateMedidaAsync(int accionId, GuardarAccionCorrectivaRequest req)
    {
        using var ctx = _factory.CreateDbContext();
        var accion = await ctx.Set<SsomaAccionCorrectiva>().FindAsync(accionId)
            ?? throw new AbrilException("Medida de control no encontrada.", 404);
        accion.Descripcion = req.Descripcion;
        accion.Tipo = req.Tipo ?? accion.Tipo;
        accion.ResponsableNombre = req.ResponsableNombre;
        accion.ResponsableWorkerId = req.ResponsableWorkerId;
        accion.FechaCompromiso = req.FechaCompromiso;
        accion.FechaCumplimiento = req.FechaCumplimiento;
        accion.Estado = req.Estado ?? accion.Estado;
        await ctx.SaveChangesAsync();
    }

    public async Task DeleteMedidaAsync(int accionId)
    {
        using var ctx = _factory.CreateDbContext();
        var accion = await ctx.Set<SsomaAccionCorrectiva>().FindAsync(accionId)
            ?? throw new AbrilException("Medida de control no encontrada.", 404);
        ctx.Set<SsomaAccionCorrectiva>().Remove(accion);
        await ctx.SaveChangesAsync();
    }

    public async Task<List<string>> GetDestinatariosFlashReportAsync()
    {
        // "w.area ILIKE '%proyecto%'" dependía del texto legacy workers.area, que puede quedar
        // vacío aunque el trabajador tenga bien resuelto workers.area_scope_id (el árbol de áreas,
        // que es la fuente de verdad real y lo que muestra la ficha del trabajador). Caso real:
        // COORDINADOR SSOMA con area_scope_id apuntando a "Ssoma" (colgado de "Gerencia de
        // Proyectos") pero area='' -> nunca entraba por el filtro de texto. Por eso el área se
        // resuelve subiendo por area_scope_parent_id hasta encontrar un nodo "%proyecto%", en vez
        // de depender de que el texto legacy esté sincronizado.
        // Interpolada por la misma razon que en GetInicializarAsync: con `const` el
        // `{WorkersEstadoIds.Activo}` viaja literal a Postgres y la consulta falla al ejecutarse.
        var sql = $"""
            WITH RECURSIVE area_scope_ancestros AS (
                SELECT area_scope_id AS origen_id, area_scope_id, area_item_id, area_scope_parent_id
                FROM area_scope
                WHERE active = true AND state = true
                UNION ALL
                SELECT a.origen_id, p.area_scope_id, p.area_item_id, p.area_scope_parent_id
                FROM area_scope_ancestros a
                JOIN area_scope p ON p.area_scope_id = a.area_scope_parent_id
                WHERE p.active = true AND p.state = true
            ),
            area_scope_proyectos AS (
                SELECT DISTINCT a.origen_id
                FROM area_scope_ancestros a
                JOIN area_item ai ON ai.area_item_id = a.area_item_id
                WHERE ai.area_item_name ILIKE '%proyecto%' AND ai.state = true
            )
            SELECT DISTINCT w.email_corporativo
            FROM workers w
            LEFT JOIN puesto pu ON pu.puesto_id = w.puesto_id
            WHERE w.state AND w.workers_estado_id = {WorkersEstadoIds.Activo}
              AND w.email_corporativo ILIKE '%@abril.pe'
              AND w.contrata_casa = 'Casa'
              AND w.email_corporativo IS NOT NULL
              AND w.email_corporativo <> ''
              AND (
                  w.area     ILIKE '%proyecto%'
                  OR w.area_scope_id IN (SELECT origen_id FROM area_scope_proyectos)
                  OR w.subarea  ILIKE '%talento%'
                  OR w.subarea  ILIKE '%humano%'
                  OR w.subarea  ILIKE '%gth%'
                  -- Antes esto buscaba texto suelto en w.ocupacion ('%medico%',
                  -- '%gerente%general%', '%gerente%administr%'). Al unificar los catálogos
                  -- esos tres casos se convirtieron en categorías propias, asignadas a los
                  -- mismos trabajadores que la búsqueda alcanzaba.
                  OR pu.categoria_id = ANY(@categorias)
                  OR w.jefatura  ILIKE '%gerente%general%'
                  OR w.jefatura  ILIKE '%gerente%administr%'
              );
            """;
        await using var conn = Conn();
        await conn.OpenAsync();
        var rows = await conn.QueryAsync<string>(
            sql, new { categorias = CategoriaIds.DestinatariosFlashReport });
        return rows.ToList();
    }
}

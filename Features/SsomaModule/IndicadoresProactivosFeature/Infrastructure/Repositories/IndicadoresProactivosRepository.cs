using Microsoft.EntityFrameworkCore;
using Abril_Backend.Features.Ssoma.Paso.Entities;
using Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Application;
using Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Infrastructure.Models;
using Abril_Backend.Features.SsomaModule.Shared;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Infrastructure.Repositories;

public class IndicadoresProactivosRepository : IIndicadoresProactivosRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    /// <summary>
    /// Contratistas que nunca deben aparecer como TARJETA con metas propias en los
    /// indicadores proactivos (RACS, OPT, capacitaciones, ATS, inspecciones): proveedores
    /// de grúas/concreto/equipo/vigilancia con un solo trabajador o sin supervisor, que no
    /// encajan en el modelo de programa proactivo. Decisión de negocio confirmada — no hay
    /// flag en Contributor que las distinga de una contratista de mano de obra real.
    /// Agregar aquí el contributor_id de "Etac" cuando se registre en el sistema.
    ///
    /// OJO: no tener tarjeta NO significa que su actividad se descarte. Estas empresas se
    /// tratan como Casa (ver <c>CasaEmpresaIds</c>): la auditoría ATS / OPT / RAC que el
    /// staff de Abril ejecuta SOBRE sus trabajadores es esfuerzo proactivo de Casa —
    /// precisamente porque no tienen supervisor propio que lo haga. Antes se descartaban
    /// en silencio: no calzaban como contratista (empresa excluida de la lista) ni como
    /// Casa (empresa != null y EsAbril = false), así que las 3 auditorías ATS de AGRUMAQ en
    /// SAUCE ZEN aparecían como "Ejec: 0" (incidencia Corilla, ago-2026).
    /// </summary>
    private static readonly HashSet<int> EmpresasSinIndicadoresProactivos = new()
    {
        570, // ARAGONESA GRUAS Y MAQUINARIAS S.A.C. - AGRUMAQ
        591, // UNIÓN DE CONCRETERAS S.A.
        572, // CASEVIP S.R.L (vigilancia)
    };

    /// <summary>
    /// Empresas cuya actividad proactiva se contabiliza dentro de "Casa — Staff Propio":
    /// las entidades internas de Abril (<c>Contributor.EsAbril</c>) más las que no tienen
    /// programa proactivo propio (<see cref="EmpresasSinIndicadoresProactivos"/>). Es el
    /// mismo criterio que decide qué empresas NO se listan como contratista, para que
    /// ninguna quede en el limbo entre ambas categorías.
    /// </summary>
    private static List<int> CasaEmpresaIds(IEnumerable<int> esAbrilIds)
        => esAbrilIds.Concat(EmpresasSinIndicadoresProactivos).Distinct().ToList();

    /// <summary>
    /// Metas de inspecciones del mes: FIJAS por tipo de empresa (no proporcionales al promedio
    /// de trabajadores como las de RACS/OPT/ATS/capacitaciones). Ser independientes del tareo
    /// es deliberado: en una obra sin tareo cargado son el único indicador medible.
    /// </summary>
    private const int MetaInspeccionesCasa = 20;
    private const int MetaInspeccionesContratista = 15;

    public IndicadoresProactivosRepository(IDbContextFactory<AppDbContext> factory)
        => _factory = factory;

    /// <summary>
    /// Samuel Justiniani (sjustiniani@abril.pe) — excepción explícita a pedido suyo,
    /// igual que en Desempeño Supervisor. Su ocupación real es "SSOMA", no calza con
    /// el texto "Coordinador SSOMA" que sí usan los demás coordinadores.
    /// </summary>
    private const int WorkerIdSamuel = 12305;

    // ─────────────────────────────────────────────────────────────────────────
    // OCULTAR/MOSTRAR EMPRESAS EN EL SEGUIMIENTO
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<bool> EsCoordinadorSsomaAsync(int userId)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var datos = await ctx.Person
            .Where(p => p.UserId == userId)
            .Join(ctx.Worker, p => p.PersonId, w => w.PersonId,
                  (p, w) => new
                  {
                      w.Id,
                      // La categoría sale del puesto: workers ya no la guarda.
                      CategoriaId = w.PuestoCatalogo != null ? w.PuestoCatalogo.CategoriaId : (int?)null
                  })
            .FirstOrDefaultAsync();
        if (datos is null) return false;
        if (datos.Id == WorkerIdSamuel) return true;

        // Antes se concatenaban categoría y ocupación para buscar "Coordinador" y "SSOMA"
        // sueltos, porque el dato no siempre vivía en el mismo campo. Al unificar los
        // catálogos se creó la categoría COORDINADOR SSOMA con exactamente esos mismos
        // trabajadores.
        return datos.CategoriaId == CategoriaIds.CoordinadorSsoma;
    }

    public async Task<HashSet<int>> GetEmpresaExcluidaIdsAsync()
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var ids = await ctx.SsIndicadorEmpresaExcluidas.Select(e => e.EmpresaId).ToListAsync();
        return ids.ToHashSet();
    }

    public async Task OcultarEmpresaAsync(int empresaId, string? motivo, int userId)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var existe = await ctx.SsIndicadorEmpresaExcluidas.FirstOrDefaultAsync(e => e.EmpresaId == empresaId);
        if (existe is not null) return;
        ctx.SsIndicadorEmpresaExcluidas.Add(new SsIndicadorEmpresaExcluida
        {
            EmpresaId = empresaId,
            Motivo = motivo,
            ExcluidoPor = userId,
            CreatedAt = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync();
    }

    public async Task MostrarEmpresaAsync(int empresaId)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var existe = await ctx.SsIndicadorEmpresaExcluidas.FirstOrDefaultAsync(e => e.EmpresaId == empresaId);
        if (existe is null) return;
        ctx.SsIndicadorEmpresaExcluidas.Remove(existe);
        await ctx.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PROGRAMACIÓN DE INSPECCIONES
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<List<InspeccionTipoDto>> GetTiposInspeccionAsync()
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsomaInspeccionTipo
            .OrderBy(t => t.Id)
            .Select(t => new InspeccionTipoDto(t.Id, t.Nombre))
            .ToListAsync();
    }

    public async Task<ProgInspeccionResumenDto> GetProgInspeccionAsync(int proyectoId, int mes, int anio)
    {
        using var ctx = _factory.CreateDbContext();

        var empresasContratistas = await ctx.SsTareoDetalleContratista
            .Where(d => d.Tareo!.ProyectoId == proyectoId
                     && d.Tareo.Fecha.Month == mes
                     && d.Tareo.Fecha.Year == anio
                     && d.CantidadPersonas > 0)
            .Select(d => new { d.EmpresaId, NombreEmpresa = d.Empresa!.ContributorName })
            .Distinct()
            .ToListAsync();

        var programados = await ctx.SsomaProgInspeccionEmpresa
            .Where(p => p.ProyectoId == proyectoId && p.Mes == mes && p.Anio == anio)
            .ToListAsync();

        var empresasDto = new List<EmpresaProgInspeccionDto>();

        var casaTipos = programados.Where(p => p.EmpresaTipo == "Casa").Select(p => p.InspeccionTipoId).ToList();
        empresasDto.Add(new EmpresaProgInspeccionDto(null, "Casa", "Casa — Staff Propio", casaTipos));

        foreach (var emp in empresasContratistas)
        {
            var tipos = programados
                .Where(p => p.EmpresaTipo == "Contratista" && p.EmpresaId == emp.EmpresaId)
                .Select(p => p.InspeccionTipoId).ToList();
            empresasDto.Add(new EmpresaProgInspeccionDto(emp.EmpresaId, "Contratista",
                emp.NombreEmpresa ?? $"Empresa {emp.EmpresaId}", tipos));
        }

        return new ProgInspeccionResumenDto(proyectoId, mes, anio, empresasDto);
    }

    public async Task GuardarProgInspeccionAsync(GuardarProgInspeccionRequest req, int userId)
    {
        using var ctx = _factory.CreateDbContext();

        var existentes = await ctx.SsomaProgInspeccionEmpresa
            .Where(p => p.ProyectoId == req.ProyectoId
                     && p.Mes == req.Mes
                     && p.Anio == req.Anio
                     && p.EmpresaTipo == req.EmpresaTipo
                     && p.EmpresaId == req.EmpresaId)
            .ToListAsync();

        ctx.SsomaProgInspeccionEmpresa.RemoveRange(existentes);

        var nuevos = req.TiposAplicables.Select(tipoId => new SsomaProgInspeccionEmpresa
        {
            ProyectoId = req.ProyectoId,
            EmpresaId = req.EmpresaId,
            EmpresaTipo = req.EmpresaTipo,
            Mes = req.Mes,
            Anio = req.Anio,
            InspeccionTipoId = tipoId,
            CreadoPor = userId,
            CreatedAt = DateTime.UtcNow
        });

        ctx.SsomaProgInspeccionEmpresa.AddRange(nuevos);
        await ctx.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CÁLCULO DE METAS E INDICADORES POR EMPRESA
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<List<MetaEmpresaDto>> GetMetasEmpresaAsync(int proyectoId, int mes, int anio)
    {
        using var ctx = _factory.CreateDbContext();
        var hoy = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var esMesActual = hoy.Month == mes && hoy.Year == anio;
        var fechaIni = new DateTime(anio, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var fechaFin = new DateTime(anio, mes, DateTime.DaysInMonth(anio, mes), 0, 0, 0, DateTimeKind.Utc).AddDays(1);
        var fechaCorte = esMesActual ? hoy.AddDays(1) : fechaFin;

        var resultado = new List<MetaEmpresaDto>();
        var fechaIniOnly  = DateOnly.FromDateTime(fechaIni);
        var fechaFinOnly  = DateOnly.FromDateTime(fechaFin);
        var fechaCorteOnly = DateOnly.FromDateTime(fechaCorte);
        var diasHabilesMes = ContarDiasHabilesDelMes(mes, anio);

        // OPT/ATS/Charlas se atribuyen a la empresa con la que el trabajador estaba vinculado
        // EL DÍA DEL EVENTO (no la vinculación de hoy) — si cambió de contratista a mitad de
        // mes, cada registro debe quedar con la empresa que correspondía en esa fecha, igual
        // que RACS/Inspecciones (que guardan el proyecto/empresa directo en el registro).
        var vincActivaQ = ctx.WorkerVinculacion.Where(v => v.FechaFin == null);

        // Empresas marcadas Contributor.EsAbril son entidades internas/administrativas
        // de Abril (ej. inmobiliarias propias), no contratistas de obra reales — su
        // actividad se cuenta como Casa, no como una contratista más en el dashboard.
        // A partir de aquí se usa casaEmpresaIds (EsAbril + las que no tienen programa
        // proactivo propio) para que ninguna empresa quede fuera de ambas categorías.
        var esAbrilIds = await ctx.Contributor.Where(c => c.EsAbril).Select(c => c.ContributorId).ToListAsync();
        var casaEmpresaIds = CasaEmpresaIds(esAbrilIds);

        // Subquery reutilizable — no carga IDs en memoria (solo para saber qué empresas
        // mostrar aunque no tengan tareo; el conteo real usa EmpresaDelTrabajadorEnFecha).
        var casaWorkerQ = vincActivaQ
            .Where(v => v.ProyectoId == proyectoId && (v.EmpresaId == null || casaEmpresaIds.Contains(v.EmpresaId!.Value)))
            .Select(v => v.WorkerId);

        // OPT/ATS/Charlas del mes completo, con fecha, para resolver empresa por evento.
        var optMesRaw = await ctx.SsomaOpt
            .Where(o => o.ProyectoId == proyectoId && o.Fecha >= fechaIni && o.Fecha < fechaCorte)
            .SelectMany(o => o.Trabajadores.Select(t => new { o.Fecha, t.TrabajadorId, t.OptId }))
            .ToListAsync();
        // Se guarda también AuditorWorkerId: una auditoría ATS cuenta para la empresa tanto si
        // uno de sus trabajadores fue AUDITADO como si uno de sus supervisores fue quien AUDITÓ
        // (mismo criterio ya aplicado a la lista de Auditoría de ATS y al bug ya corregido en
        // RAC) — antes solo se contaba el lado auditado y las auditorías que un contratista
        // ejecuta con su propio supervisor aparecían con Ejec: 0 para esa empresa.
        var atsMesRaw = await ctx.SsomaAuditoriaAts
            .Where(a => a.ProyectoId == proyectoId && a.Fecha >= fechaIniOnly && a.Fecha < fechaCorteOnly)
            .Select(a => new { a.Fecha, WorkerId = a.AuditadoWorkerId, AuditorWorkerId = a.AuditorWorkerId })
            .ToListAsync();
        var charlasMesRaw = await ctx.SsCharlaAsistencias
            .Where(a => a.Charla!.ProyectoId == proyectoId
                     && a.Charla.Fecha >= fechaIni && a.Charla.Fecha < fechaCorte && a.Asistio)
            .Select(a => new { Fecha = a.Charla!.Fecha.Date, a.WorkerId })
            .ToListAsync();

        // Empresa a la que pertenecía cada trabajador EN LA FECHA del evento. La empresa de un
        // trabajador no depende de la obra (pertenece a una sola empresa a la vez): se cargan sus
        // vinculaciones en TODAS las obras (no solo la del proyecto consultado) y se prefiere la
        // de la MISMA obra del evento; si en esa fecha su vínculo activo quedó registrado en OTRA
        // obra (traslado a mitad de mes dentro de la misma empresa), se usa esa. Antes solo se
        // cargaba la vinculación del proyecto y un traslado de obra dejaba la actividad sin empresa
        // → se contaba como "Casa" por error (incidencia BERROCAL / CEDRO 33, jul-2026).
        var eventoWorkerIds = optMesRaw.Select(x => x.TrabajadorId)
            .Concat(atsMesRaw.Select(x => x.WorkerId))
            .Concat(atsMesRaw.Select(x => x.AuditorWorkerId))
            .Concat(charlasMesRaw.Select(x => x.WorkerId))
            .Distinct().ToList();
        var vincHist = await ctx.WorkerVinculacion
            .Where(v => v.ProyectoId != null && eventoWorkerIds.Contains(v.WorkerId))
            .Select(v => new { ProyectoId = v.ProyectoId!.Value, v.WorkerId, v.EmpresaId, v.FechaInicio, v.FechaFin })
            .ToListAsync();
        var vincPorWorker = vincHist.GroupBy(v => v.WorkerId).ToDictionary(g => g.Key, g => g.ToList());

        int? EmpresaDelTrabajadorEnFecha(int workerId, DateOnly fecha)
        {
            if (!vincPorWorker.TryGetValue(workerId, out var lista)) return null;
            var activas = lista
                .Where(v => v.FechaInicio <= fecha && (v.FechaFin == null || v.FechaFin >= fecha))
                .ToList();
            if (activas.Count == 0) return null;
            // Preferir la vinculación de la MISMA obra del evento (comportamiento original); si el
            // trabajador fue trasladado a otra obra en esa fecha, usar la más reciente activa.
            var elegida = activas.FirstOrDefault(v => v.ProyectoId == proyectoId)
                          ?? activas.OrderByDescending(v => v.FechaInicio).First();
            return elegida.EmpresaId;
        }

        // Charla diaria obligatoria del contratista (evidencia subida por Tareo, no asistencia
        // del módulo de Charlas de staff): para contratistas, "Capacitaciones" mide cuántas de
        // las evidencias exigidas por sus días tareados efectivamente subieron.
        var charlaContratistaMesRaw = await ctx.SsCharlaContratista
            .Where(c => c.State && c.ProyectoId == proyectoId
                     && c.Fecha >= fechaIniOnly && c.Fecha < fechaCorteOnly)
            .Select(c => new { c.EmpresaId, c.Fecha })
            .ToListAsync();

        // ── CASA ──────────────────────────────────────────────────────────────
        var tareoCasa = await ctx.SsTareoDetalleCasa
            .Where(d => d.Tareo!.ProyectoId == proyectoId
                     && d.Tareo.Fecha >= fechaIniOnly && d.Tareo.Fecha < fechaFinOnly)
            .GroupBy(d => d.Tareo!.Fecha)
            .Select(g => new { Fecha = g.Key, Total = g.Sum(x => x.CantidadPersonas) })
            .ToListAsync();

        if (tareoCasa.Any(d => d.Total > 0))
        {
            var diasCasa = tareoCasa.Count(d => d.Total > 0);
            var promCasa = diasCasa > 0 ? (decimal)tareoCasa.Where(d => d.Total > 0).Sum(d => d.Total) / diasCasa : 0;

            // Este bloque solo corre si Casa tuvo tareo, así que la meta fija siempre aplica.
            const int metaCasaInsp = MetaInspeccionesCasa;

            var fechasCasaPresencia = tareoCasa.Where(d => d.Total > 0).Select(d => d.Fecha.ToDateTime(TimeOnly.MinValue)).ToList();
            var metaCasaCharlas = ContarDiasHabilesConPresenciaDateTime(fechasCasaPresencia, mes, anio);

            // "Reportados por Casa" usa EmpresaReportanteId (quién lo reportó), igual que el
            // loop de contratistas más abajo — no EmpresaReportadaId (a quién se le reportó),
            // que es un concepto distinto ("atribuidos"). Mezclar ambos campos duplicaba RACs
            // (contados en Casa y en la contratista real a la vez) y perdía otros (reportados
            // por una empresa Es Abril contra una contratista real, que no calzaban en ningún filtro).
            var racsGenCasa = await ctx.SsomaRacs
                .CountAsync(r => r.ProyectoId == proyectoId
                              && (r.EmpresaReportanteId == null || casaEmpresaIds.Contains(r.EmpresaReportanteId!.Value))
                              && r.FechaReporte >= fechaIni && r.FechaReporte < fechaCorte);
            var racsAtribuidosCasa = await ctx.SsomaRacs
                .CountAsync(r => r.ProyectoId == proyectoId
                              && (r.EmpresaReportadaId == null || casaEmpresaIds.Contains(r.EmpresaReportadaId!.Value))
                              && r.FechaReporte >= fechaIni && r.FechaReporte < fechaCorte);
            var racsCerCasa = await ctx.SsomaRacs
                .CountAsync(r => r.ProyectoId == proyectoId
                              && (r.EmpresaReportadaId == null || casaEmpresaIds.Contains(r.EmpresaReportadaId!.Value))
                              && r.Estado == "Cerrado"
                              && r.FechaReporte >= fechaIni && r.FechaReporte < fechaCorte);

            bool EsCasa(int? empresaId) => empresaId == null || casaEmpresaIds.Contains(empresaId.Value);

            var optCasa = optMesRaw
                .Where(x => EsCasa(EmpresaDelTrabajadorEnFecha(x.TrabajadorId, DateOnly.FromDateTime(x.Fecha))))
                .Select(x => x.OptId).Distinct().Count();

            var atsCasa = atsMesRaw
                .Count(x => EsCasa(EmpresaDelTrabajadorEnFecha(x.WorkerId, x.Fecha))
                         || EsCasa(EmpresaDelTrabajadorEnFecha(x.AuditorWorkerId, x.Fecha)));

            var charlasCasa = charlasMesRaw
                .Where(x => EsCasa(EmpresaDelTrabajadorEnFecha(x.WorkerId, DateOnly.FromDateTime(x.Fecha))))
                .Select(x => x.Fecha).Distinct().Count();

            var inspCasa = await ctx.SsomaInspeccion
                .CountAsync(i => i.ProyectoId == proyectoId
                              && (i.EmpresaId == null || casaEmpresaIds.Contains(i.EmpresaId!.Value))
                              && i.Fecha >= fechaIni && i.Fecha < fechaCorte);

            resultado.Add(BuildMetaDto(null, "Casa", "Casa — Staff Propio", promCasa, diasCasa,
                (int)Math.Ceiling(promCasa * 0.40m), (int)Math.Ceiling(promCasa * 0.10m),
                (int)Math.Ceiling(promCasa * 0.30m), metaCasaCharlas, metaCasaInsp,
                racsGenCasa, racsAtribuidosCasa, racsCerCasa, optCasa, atsCasa, charlasCasa, inspCasa));
        }

        // ── CONTRATISTAS ──────────────────────────────────────────────────────
        // No basta con las empresas tareadas este mes: una contratista puede tener
        // trabajadores vinculados/haciendo OPT/ATS/charlas o RACS/inspecciones en el
        // proyecto sin que alguien haya cargado su tareo — si solo se listan las tareadas,
        // esa actividad real queda invisible (0%) aunque sí se ejecutó.
        var empresaIdsTareo = await ctx.SsTareoDetalleContratista
            .Where(d => d.Tareo!.ProyectoId == proyectoId
                     && d.Tareo.Fecha >= fechaIniOnly && d.Tareo.Fecha < fechaFinOnly)
            .Select(d => d.EmpresaId)
            .Distinct().ToListAsync();

        var empresaIdsVinc = await vincActivaQ
            .Where(v => v.ProyectoId == proyectoId && v.EmpresaId != null)
            .Select(v => v.EmpresaId!.Value)
            .Distinct().ToListAsync();

        var empresaIdsRacsInsp = await ctx.SsomaRacs
            .Where(r => r.ProyectoId == proyectoId && r.EmpresaReportadaId != null
                     && r.FechaReporte >= fechaIni && r.FechaReporte < fechaCorte)
            .Select(r => r.EmpresaReportadaId!.Value)
            .Union(ctx.SsomaInspeccion
                .Where(i => i.ProyectoId == proyectoId && i.EmpresaId != null
                         && i.Fecha >= fechaIni && i.Fecha < fechaCorte)
                .Select(i => i.EmpresaId!.Value))
            .Distinct().ToListAsync();

        var empresaIds = empresaIdsTareo.Union(empresaIdsVinc).Union(empresaIdsRacsInsp)
            .Except(casaEmpresaIds).Distinct().ToList();

        var empresas = await ctx.Contributor
            .Where(c => empresaIds.Contains(c.ContributorId))
            .Select(c => new { EmpresaId = (int?)c.ContributorId, Nombre = c.ContributorName })
            .ToListAsync();

        foreach (var emp in empresas)
        {
            var tareoEmp = await ctx.SsTareoDetalleContratista
                .Where(d => d.Tareo!.ProyectoId == proyectoId && d.EmpresaId == emp.EmpresaId
                         && d.Tareo.Fecha >= fechaIniOnly && d.Tareo.Fecha < fechaFinOnly
                         && d.CantidadPersonas > 0)
                .Select(d => new { d.Tareo!.Fecha, d.CantidadPersonas }).ToListAsync();

            var diasEmp = tareoEmp.Count;
            var promEmp = diasEmp > 0 ? (decimal)tareoEmp.Sum(d => d.CantidadPersonas) / diasEmp : 0;

            // La meta de inspecciones se prorratea por días tareados sobre el total de días
            // hábiles del mes (ej. 8 días tareados de ~22 hábiles → meta ≈ 6, no 15 fijo) —
            // para contratistas que estuvieron poco tiempo en obra. Si NO tareó ningún día se
            // mantiene el fijo de 15: sigue siendo la única meta independiente del tareo, para
            // sostener el indicador en obras sin tareo cargado (se probó condicionarla en
            // ago-2026: KAURÍ 15% → 0% y MÁXIMO ABRIL 30% → 0%, ambas sin tareo cargado).
            var metaInsp = CalcularMetaInspeccionesContratista(diasEmp, diasHabilesMes);

            // Meta de charlas = días efectivamente tareados (no solo días hábiles): si se
            // tareó 10 días, debe haber 10 evidencias subidas.
            var metaCharlas = diasEmp;

            var racsGen = await ctx.SsomaRacs
                .CountAsync(r => r.ProyectoId == proyectoId && r.EmpresaReportanteId == emp.EmpresaId
                              && r.FechaReporte >= fechaIni && r.FechaReporte < fechaCorte);
            var racsAtribuidos = await ctx.SsomaRacs
                .CountAsync(r => r.ProyectoId == proyectoId && r.EmpresaReportadaId == emp.EmpresaId
                              && r.FechaReporte >= fechaIni && r.FechaReporte < fechaCorte);
            var racsCer = await ctx.SsomaRacs
                .CountAsync(r => r.ProyectoId == proyectoId && r.EmpresaReportadaId == emp.EmpresaId
                              && r.Estado == "Cerrado"
                              && r.FechaReporte >= fechaIni && r.FechaReporte < fechaCorte);

            var opt = optMesRaw
                .Where(x => EmpresaDelTrabajadorEnFecha(x.TrabajadorId, DateOnly.FromDateTime(x.Fecha)) == emp.EmpresaId)
                .Select(x => x.OptId).Distinct().Count();

            var ats = atsMesRaw
                .Count(x => EmpresaDelTrabajadorEnFecha(x.WorkerId, x.Fecha) == emp.EmpresaId
                         || EmpresaDelTrabajadorEnFecha(x.AuditorWorkerId, x.Fecha) == emp.EmpresaId);

            var charlas = charlaContratistaMesRaw
                .Where(x => x.EmpresaId == emp.EmpresaId)
                .Select(x => x.Fecha).Distinct().Count();

            var insp = await ctx.SsomaInspeccion
                .CountAsync(i => i.ProyectoId == proyectoId && i.EmpresaId == emp.EmpresaId
                              && i.Fecha >= fechaIni && i.Fecha < fechaCorte);

            // Si la empresa está INACTIVA (promedio de trabajadores < 5 y ninguna
            // herramienta de gestión proactiva ejecutada) no se muestra — no tiene sentido
            // llenar el dashboard con contratistas sin actividad real.
            var esActiva = promEmp >= 5
                || racsGen > 0 || racsAtribuidos > 0 || opt > 0 || ats > 0 || charlas > 0 || insp > 0;
            if (!esActiva) continue;

            resultado.Add(BuildMetaDto(emp.EmpresaId, "Contratista", emp.Nombre ?? $"Empresa {emp.EmpresaId}",
                promEmp, diasEmp,
                (int)Math.Ceiling(promEmp * 0.40m), (int)Math.Ceiling(promEmp * 0.10m),
                (int)Math.Ceiling(promEmp * 0.30m), metaCharlas, metaInsp,
                racsGen, racsAtribuidos, racsCer, opt, ats, charlas, insp));
        }

        return resultado;
    }

    public async Task<List<IndicadorProactivoProyectoDto>> GetSeguimientoTodosProyectosAsync(int mes, int anio)
    {
        using var ctx = _factory.CreateDbContext();

        var hoy         = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var esMesActual = hoy.Month == mes && hoy.Year == anio;
        var fechaIni    = new DateTime(anio, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var fechaFin    = new DateTime(anio, mes, DateTime.DaysInMonth(anio, mes), 0, 0, 0, DateTimeKind.Utc).AddDays(1);
        var fechaCorte  = esMesActual ? hoy.AddDays(1) : fechaFin;
        var iniOnly     = DateOnly.FromDateTime(fechaIni);
        var finOnly     = DateOnly.FromDateTime(fechaFin);
        var corteOnly   = DateOnly.FromDateTime(fechaCorte);

        // "Proyecto activo" para SSOMA = habilitado en ss_proyecto_habilitado,
        // independiente del estado genérico de Project (ver ProyectoHabilitadoFeature).
        var proyectos = await ctx.Project
            .Where(p => ctx.SsProyectoHabilitado.Any(h => h.ProyectoId == p.ProjectId && h.State && h.Active))
            .Select(p => new { p.ProjectId, p.ProjectDescription })
            .ToListAsync();
        var proyIds = proyectos.Select(p => p.ProjectId).ToList();
        if (!proyIds.Any()) return [];

        // ── 8 bulk queries para todos los proyectos ───────────────────────────

        // 1. Tareo casa: promedio trabajadores por proyecto
        var tareoCasaBulk = await ctx.SsTareoDetalleCasa
            .Where(d => proyIds.Contains(d.Tareo!.ProyectoId)
                     && d.Tareo.Fecha >= iniOnly && d.Tareo.Fecha < finOnly)
            .GroupBy(d => new { d.Tareo!.ProyectoId, d.Tareo.Fecha })
            .Select(g => new { g.Key.ProyectoId, g.Key.Fecha, Total = g.Sum(x => x.CantidadPersonas) })
            .ToListAsync();

        // 2. Tareo contratista: promedio trabajadores por proyecto/empresa
        var tareoContrBulk = await ctx.SsTareoDetalleContratista
            .Where(d => proyIds.Contains(d.Tareo!.ProyectoId)
                     && d.Tareo.Fecha >= iniOnly && d.Tareo.Fecha < finOnly
                     && d.CantidadPersonas > 0)
            .Select(d => new { d.Tareo!.ProyectoId, d.EmpresaId, Nombre = d.Empresa!.ContributorName, d.Tareo.Fecha, d.CantidadPersonas })
            .ToListAsync();

        // 3. RACs por proyecto/empresa
        var racsBulk = await ctx.SsomaRacs
            .Where(r => proyIds.Contains(r.ProyectoId)
                     && r.FechaReporte >= fechaIni && r.FechaReporte < fechaCorte)
            .Select(r => new { r.ProyectoId, r.EmpresaReportadaId, r.EmpresaReportanteId, r.Estado })
            .ToListAsync();

        // 4. OPT por proyecto: contar OPTs distintos para casa y contratistas
        var optBulk = await ctx.SsomaOpt
            .Where(o => proyIds.Contains(o.ProyectoId) && o.Fecha >= fechaIni && o.Fecha < fechaCorte)
            .SelectMany(o => o.Trabajadores.Select(t => new { o.ProyectoId, o.Fecha, t.TrabajadorId, t.OptId }))
            .ToListAsync();

        // 5. ATS por proyecto/trabajador. Se guarda también AuditorWorkerId: una auditoría
        // cuenta para la empresa tanto si audita a uno de sus trabajadores como si el auditor
        // es su propio supervisor (mismo criterio que la lista de Auditoría de ATS y el bug ya
        // corregido en RAC).
        var atsBulk = await ctx.SsomaAuditoriaAts
            .Where(a => a.ProyectoId != null && proyIds.Contains(a.ProyectoId!.Value)
                     && a.Fecha >= iniOnly && a.Fecha < corteOnly)
            .Select(a => new { ProyectoId = a.ProyectoId!.Value, WorkerId = a.AuditadoWorkerId, AuditorWorkerId = a.AuditorWorkerId, a.Fecha })
            .ToListAsync();

        // 6. Charlas: días distintos con asistencia por proyecto/trabajador
        var charlasBulk = await ctx.SsCharlaAsistencias
            .Where(a => a.Charla!.ProyectoId != null
                     && proyIds.Contains(a.Charla.ProyectoId!.Value)
                     && a.Charla.Fecha >= fechaIni && a.Charla.Fecha < fechaCorte
                     && a.Asistio)
            .Select(a => new { ProyectoId = a.Charla!.ProyectoId!.Value, a.WorkerId, Fecha = a.Charla.Fecha.Date })
            .Distinct()
            .ToListAsync();

        // 6b. Charla diaria obligatoria del contratista (evidencia subida por Tareo): para
        // contratistas, "Capacitaciones" mide cuántas evidencias exigidas por sus días
        // tareados efectivamente subieron, no asistencia del módulo de Charlas de staff.
        var charlaContratistaBulk = await ctx.SsCharlaContratista
            .Where(c => c.State && proyIds.Contains(c.ProyectoId)
                     && c.Fecha >= iniOnly && c.Fecha < corteOnly)
            .Select(c => new { c.ProyectoId, c.EmpresaId, c.Fecha })
            .ToListAsync();

        // 7. Inspecciones por proyecto/empresa
        var inspBulk = await ctx.SsomaInspeccion
            .Where(i => proyIds.Contains(i.ProyectoId)
                     && i.Fecha >= fechaIni && i.Fecha < fechaCorte)
            .Select(i => new { i.ProyectoId, i.EmpresaId })
            .ToListAsync();

        // 8. Meta inspecciones: Casa fija por tipo de empresa; contratistas prorrateada por
        // días tareados sobre días hábiles del mes — ver comentario en GetMetasEmpresaAsync.
        const int metaInspCasa = MetaInspeccionesCasa;
        var diasHabilesMes = ContarDiasHabilesDelMes(mes, anio);

        // OPT/ATS/Charlas se atribuyen a la empresa con la que el trabajador estaba vinculado
        // EL DÍA DEL EVENTO (no la vinculación de hoy) — si cambió de contratista a mitad de
        // mes, cada registro debe quedar con la empresa que correspondía en esa fecha, igual
        // que RACS/Inspecciones (que guardan el proyecto/empresa directo al registrar).
        var vincActivaBulk = await ctx.WorkerVinculacion
            .Where(v => v.FechaFin == null && v.ProyectoId != null && proyIds.Contains(v.ProyectoId!.Value))
            .Select(v => new { ProyectoId = v.ProyectoId!.Value, v.WorkerId, v.EmpresaId })
            .ToListAsync();

        // Historial completo (no solo vinculación activa hoy) para resolver la empresa del
        // trabajador en la fecha exacta de cada OPT/ATS/Charla. Se agrupa por WORKER (no por
        // obra+worker): la empresa de un trabajador no depende de la obra —pertenece a una sola
        // empresa a la vez—, así que se prefiere su vinculación en la MISMA obra del evento y, si
        // en esa fecha su vínculo activo quedó registrado en OTRA obra (traslado a mitad de mes
        // dentro de la misma empresa), se usa esa. Antes se agrupaba por (obra, worker) y un
        // traslado de obra dejaba la actividad sin empresa → se contaba como "Casa" por error
        // (incidencia BERROCAL / CEDRO 33, jul-2026).
        var vincHistBulk = await ctx.WorkerVinculacion
            .Where(v => v.ProyectoId != null && proyIds.Contains(v.ProyectoId!.Value))
            .Select(v => new { ProyectoId = v.ProyectoId!.Value, v.WorkerId, v.EmpresaId, v.FechaInicio, v.FechaFin })
            .ToListAsync();
        var vincPorWorker = vincHistBulk
            .GroupBy(v => v.WorkerId)
            .ToDictionary(g => g.Key, g => g.ToList());

        int? EmpresaDelTrabajadorEnFecha(int proyectoId, int workerId, DateOnly fecha)
        {
            if (!vincPorWorker.TryGetValue(workerId, out var lista)) return null;
            var activas = lista
                .Where(v => v.FechaInicio <= fecha && (v.FechaFin == null || v.FechaFin >= fecha))
                .ToList();
            if (activas.Count == 0) return null;
            // Preferir la vinculación de la MISMA obra del evento (comportamiento original); si el
            // trabajador fue trasladado a otra obra en esa fecha, usar la más reciente activa.
            var elegida = activas.FirstOrDefault(v => v.ProyectoId == proyectoId)
                          ?? activas.OrderByDescending(v => v.FechaInicio).First();
            return elegida.EmpresaId;
        }

        // Empresas marcadas Contributor.EsAbril son entidades internas/administrativas
        // de Abril (ej. inmobiliarias propias), no contratistas de obra reales — su
        // actividad se cuenta como Casa, no como una contratista más en el dashboard.
        // A partir de aquí se usa casaEmpresaIds (EsAbril + las que no tienen programa
        // proactivo propio) para que ninguna empresa quede fuera de ambas categorías.
        var esAbrilIds = await ctx.Contributor.Where(c => c.EsAbril).Select(c => c.ContributorId).ToListAsync();
        var casaEmpresaIds = CasaEmpresaIds(esAbrilIds).ToHashSet();

        // Empresas a mostrar por proyecto: unión de tareadas + con vinculación activa +
        // con RACS/Inspecciones registrados. Si solo se listaran las tareadas, una
        // contratista con actividad real pero sin tareo cargado desaparece del todo (0%).
        var empresaIdsConActividad = vincActivaBulk.Where(v => v.EmpresaId != null).Select(v => v.EmpresaId!.Value)
            .Union(racsBulk.Where(r => r.EmpresaReportadaId != null).Select(r => r.EmpresaReportadaId!.Value))
            .Union(inspBulk.Where(i => i.EmpresaId != null).Select(i => i.EmpresaId!.Value))
            .Union(tareoContrBulk.Select(t => t.EmpresaId))
            .Except(casaEmpresaIds)
            .Distinct().ToList();

        var nombresEmpresa = await ctx.Contributor
            .Where(c => empresaIdsConActividad.Contains(c.ContributorId))
            .ToDictionaryAsync(c => c.ContributorId, c => c.ContributorName);

        // ── Agregar por proyecto ──────────────────────────────────────────────
        var result = new List<IndicadorProactivoProyectoDto>();

        foreach (var proy in proyectos)
        {
            var pid = proy.ProjectId;
            var empresasMes = tareoContrBulk
                .Where(t => t.ProyectoId == pid)
                .Select(t => new { t.EmpresaId, t.Nombre })
                .Concat(vincActivaBulk.Where(v => v.ProyectoId == pid && v.EmpresaId != null)
                    .Select(v => new { EmpresaId = v.EmpresaId!.Value, Nombre = nombresEmpresa.GetValueOrDefault(v.EmpresaId!.Value) ?? $"Empresa {v.EmpresaId}" }))
                .Concat(racsBulk.Where(r => r.ProyectoId == pid && r.EmpresaReportadaId != null)
                    .Select(r => new { EmpresaId = r.EmpresaReportadaId!.Value, Nombre = nombresEmpresa.GetValueOrDefault(r.EmpresaReportadaId!.Value) ?? $"Empresa {r.EmpresaReportadaId}" }))
                .Concat(inspBulk.Where(i => i.ProyectoId == pid && i.EmpresaId != null)
                    .Select(i => new { EmpresaId = i.EmpresaId!.Value, Nombre = nombresEmpresa.GetValueOrDefault(i.EmpresaId!.Value) ?? $"Empresa {i.EmpresaId}" }))
                .Where(e => !casaEmpresaIds.Contains(e.EmpresaId))
                .GroupBy(e => e.EmpresaId)
                .Select(g => g.First())
                .ToList();

            var empresasDtos = new List<MetaEmpresaDto>();

            var tcCasa = tareoCasaBulk.Where(d => d.ProyectoId == pid && d.Total > 0).ToList();
            if (tcCasa.Any())
            {
                var diasC = tcCasa.Count;
                var promC = (decimal)tcCasa.Sum(d => d.Total) / diasC;
                var metaInspC = metaInspCasa;
                var fechasC = tcCasa.Select(d => d.Fecha.ToDateTime(TimeOnly.MinValue)).ToList();
                var metaCharlasC = ContarDiasHabilesConPresenciaDateTime(fechasC, mes, anio);
                // "Reportados por Casa" usa EmpresaReportanteId (igual que las contratistas más
                // abajo); "atribuidos a Casa" usa EmpresaReportadaId — mezclarlos duplicaba RACs
                // (contados en Casa y en la contratista real a la vez).
                var racsGenC = racsBulk.Where(r => r.ProyectoId == pid
                    && (r.EmpresaReportanteId == null || casaEmpresaIds.Contains(r.EmpresaReportanteId!.Value)));
                var racsC = racsBulk.Where(r => r.ProyectoId == pid
                    && (r.EmpresaReportadaId == null || casaEmpresaIds.Contains(r.EmpresaReportadaId!.Value)));
                bool EsCasaPid(int? empresaId) => empresaId == null || casaEmpresaIds.Contains(empresaId.Value);
                var optC = optBulk.Where(o => o.ProyectoId == pid
                        && EsCasaPid(EmpresaDelTrabajadorEnFecha(pid, o.TrabajadorId, DateOnly.FromDateTime(o.Fecha))))
                    .Select(o => o.OptId).Distinct().Count();
                var atsC = atsBulk.Count(a => a.ProyectoId == pid
                    && (EsCasaPid(EmpresaDelTrabajadorEnFecha(pid, a.WorkerId, a.Fecha))
                        || EsCasaPid(EmpresaDelTrabajadorEnFecha(pid, a.AuditorWorkerId, a.Fecha))));
                var charlasC = charlasBulk.Where(a => a.ProyectoId == pid
                        && EsCasaPid(EmpresaDelTrabajadorEnFecha(pid, a.WorkerId, DateOnly.FromDateTime(a.Fecha))))
                    .Select(a => a.Fecha).Distinct().Count();
                var inspC = inspBulk.Count(i => i.ProyectoId == pid
                    && (i.EmpresaId == null || casaEmpresaIds.Contains(i.EmpresaId!.Value)));
                var racsGenCCount = racsGenC.Count();
                var racsCCount = racsC.Count();
                empresasDtos.Add(BuildMetaDto(null, "Casa", "Casa — Staff Propio", promC, diasC,
                    (int)Math.Ceiling(promC * 0.40m), (int)Math.Ceiling(promC * 0.10m),
                    (int)Math.Ceiling(promC * 0.30m), metaCharlasC, metaInspC,
                    racsGenCCount, racsCCount, racsC.Count(r => r.Estado == "Cerrado"), optC, atsC, charlasC, inspC));
            }

            // Contratistas
            foreach (var emp in empresasMes)
            {
                var te = tareoContrBulk.Where(t => t.ProyectoId == pid && t.EmpresaId == emp.EmpresaId).ToList();
                var diasE = te.Count;
                var promE = diasE > 0 ? (decimal)te.Sum(t => t.CantidadPersonas) / diasE : 0;
                // Prorrateada por días tareados — ver el comentario en GetMetasEmpresaAsync.
                var metaInspE = CalcularMetaInspeccionesContratista(diasE, diasHabilesMes);
                // Meta de charlas = días efectivamente tareados: si se tareó 10 días,
                // debe haber 10 evidencias subidas.
                var metaCharlasE = diasE;
                // "RACs reportados" = hallazgos que ESTA empresa levantó/reportó (EmpresaReportanteId) —
                // indicador proactivo de cuánto reporta. "RACs cerrados" = de los hallazgos que le fueron
                // atribuidos a ella (EmpresaReportadaId), cuántos ya cerró — indicador de cumplimiento.
                var racsReportadosE = racsBulk.Where(r => r.ProyectoId == pid && r.EmpresaReportanteId == emp.EmpresaId);
                var racsAtribuidosE = racsBulk.Where(r => r.ProyectoId == pid && r.EmpresaReportadaId == emp.EmpresaId);
                var optE = optBulk.Where(o => o.ProyectoId == pid
                        && EmpresaDelTrabajadorEnFecha(pid, o.TrabajadorId, DateOnly.FromDateTime(o.Fecha)) == emp.EmpresaId)
                    .Select(o => o.OptId).Distinct().Count();
                var atsE = atsBulk.Count(a => a.ProyectoId == pid
                    && (EmpresaDelTrabajadorEnFecha(pid, a.WorkerId, a.Fecha) == emp.EmpresaId
                        || EmpresaDelTrabajadorEnFecha(pid, a.AuditorWorkerId, a.Fecha) == emp.EmpresaId));
                var charlasE = charlaContratistaBulk
                    .Where(c => c.ProyectoId == pid && c.EmpresaId == emp.EmpresaId)
                    .Select(c => c.Fecha).Distinct().Count();
                var inspE = inspBulk.Count(i => i.ProyectoId == pid && i.EmpresaId == emp.EmpresaId);

                // Si la empresa está INACTIVA (promedio de trabajadores < 5 y ninguna
                // herramienta de gestión proactiva ejecutada) no se muestra.
                var racsGenE = racsReportadosE.Count();
                var racsAtribuidosCount = racsAtribuidosE.Count();
                var esActiva = promE >= 5
                    || racsGenE > 0 || racsAtribuidosCount > 0 || optE > 0 || atsE > 0 || charlasE > 0 || inspE > 0;
                if (!esActiva) continue;

                empresasDtos.Add(BuildMetaDto(emp.EmpresaId, "Contratista", emp.Nombre ?? $"Empresa {emp.EmpresaId}",
                    promE, diasE,
                    (int)Math.Ceiling(promE * 0.40m), (int)Math.Ceiling(promE * 0.10m),
                    (int)Math.Ceiling(promE * 0.30m), metaCharlasE, metaInspE,
                    racsGenE, racsAtribuidosCount, racsAtribuidosE.Count(r => r.Estado == "Cerrado"), optE, atsE, charlasE, inspE));
            }

            // No se omite el proyecto aunque no tenga tareo todavía: debe aparecer
            // con 0% en vez de desaparecer de la lista de "proyectos activos".

            // Usar todas las empresas con tareo (no filtrar por EsActiva = 10 días)
            // El umbral de 10 días solo afecta las metas proporcionales dentro de BuildMetaDto
            var activas = empresasDtos;
            result.Add(IndicadorProactivoCalculo.ConstruirProyecto(
                pid, proy.ProjectDescription ?? "", activas));
        }

        return result.OrderByDescending(p => p.PctProactivoGeneral).ToList();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUNTAJE DEL MES
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<PuntajeMesDto> GetPuntajeMesAsync(int proyectoId, int mes, int anio)
    {
        using var ctx = _factory.CreateDbContext();

        var proyecto = await ctx.Project
            .Where(p => p.ProjectId == proyectoId)
            .Select(p => p.ProjectDescription)
            .FirstOrDefaultAsync();

        var hoy = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var esMesActual = hoy.Month == mes && hoy.Year == anio;
        var fechaIni = new DateTime(anio, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var fechaFin = new DateTime(anio, mes, DateTime.DaysInMonth(anio, mes), 0, 0, 0, DateTimeKind.Utc).AddDays(1);
        var fechaCorte = esMesActual ? hoy.AddDays(1) : fechaFin;

        // % proactivos
        // Misma definición que la tarjeta del seguimiento (promedio de los 6 indicadores
        // agregados del proyecto) — el puntaje del mes no puede partir de un % distinto
        // del que se le muestra al usuario.
        var empresas = await GetMetasEmpresaAsync(proyectoId, mes, anio);
        var activas = empresas.Where(e => e.EsActiva).ToList();
        var pctProactivos = IndicadorProactivoCalculo
            .ConstruirProyecto(proyectoId, proyecto ?? "", activas).PctProactivoGeneral;

        // % PASSO — actividades teóricamente programadas este mes de ciclo vs ejecutadas.
        // Un proyecto puede tener una instancia de PASO por año (botón "Instanciar 2027",
        // por ejemplo); si no se filtra por Anio, se mezclan actividades/ejecuciones de
        // años distintos (incluida la plantilla corporativa, Anio = NULL) y el % queda
        // contaminado con datos que no corresponden al año consultado.
        //
        // Importante: "programadas" NO es la cantidad de filas ssoma_paso_ejecucion que
        // ya existen para el mes (esas se generan por un job y pueden estar incompletas),
        // sino la cantidad de actividades activas que -según su frecuencia/MesInicio/MesFin-
        // teóricamente corresponden a este mes de ciclo. Esta es la misma lógica que usa
        // PasoService.GetResumenMesAsync (pantalla /paso/lista) — si no se replica así, un
        // mes con pocas ejecuciones ya generadas infla el % (ej. 1/1 = 100% cuando en
        // realidad hay 40 actividades programadas ese mes y solo 1 completada).
        var pasoEntity = await ctx.SsomaPasos
            .Where(p => p.ProyectoId == proyectoId && !p.EsPlantilla && p.Anio == anio)
            .Include(p => p.Actividades.Where(a => a.Activo && a.DeletedAt == null))
                .ThenInclude(a => a.Ejecuciones)
            .FirstOrDefaultAsync();

        var (pasoProgramadas, pasoEjecutadas) = pasoEntity != null
            ? CalcularPassoDelMes(pasoEntity, mes, anio)
            : (0, 0);

        var pctPasso = pasoProgramadas > 0 ? (decimal)pasoEjecutadas / pasoProgramadas * 100 : 0;

        // % cierre accidentes — solo los ocurridos dentro del mes consultado
        var accidentesTotales = await ctx.SsomaAccidenteIncidente
            .CountAsync(a => a.ProyectoId == proyectoId && a.Fecha >= fechaIni && a.Fecha < fechaCorte);
        var accidentesCerrados = await ctx.SsomaAccidenteIncidente
            .CountAsync(a => a.ProyectoId == proyectoId && a.Fecha >= fechaIni && a.Fecha < fechaCorte && a.Estado == "Cerrado");
        var accidentesAbiertos = accidentesTotales - accidentesCerrados;
        var pctCierreAccidentes = accidentesTotales > 0
            ? (decimal)accidentesCerrados / accidentesTotales * 100 : 0;

        // % cierre hallazgos (join a través de inspección)
        var hallazgosTotales = await ctx.SsomaInspeccionHallazgo
            .Join(ctx.SsomaInspeccion,
                  h => h.InspeccionId,
                  i => i.Id,
                  (h, i) => new { h.Estado, i.ProyectoId, i.Fecha })
            .Where(x => x.ProyectoId == proyectoId && x.Fecha < fechaCorte)
            .CountAsync();

        var hallazgosCerrados = await ctx.SsomaInspeccionHallazgo
            .Join(ctx.SsomaInspeccion,
                  h => h.InspeccionId,
                  i => i.Id,
                  (h, i) => new { h.Estado, i.ProyectoId, i.Fecha })
            .Where(x => x.ProyectoId == proyectoId && x.Fecha < fechaCorte && x.Estado == "Cerrado")
            .CountAsync();

        var pctCierreHallazgos = hallazgosTotales > 0
            ? (decimal)hallazgosCerrados / hallazgosTotales * 100 : 0;

        // Cálculo del puntaje (sobre 100, con bonus hasta +10)
        var puntProactivos = Math.Min(pctProactivos, 100) * 0.40m;
        var puntPasso = Math.Min(pctPasso, 100) * 0.25m;
        var puntCierreAcc = Math.Min(pctCierreAccidentes, 100) * 0.20m;
        var puntCierreHall = Math.Min(pctCierreHallazgos, 100) * 0.15m;

        // Bonus: 0.2 pts por cada % sobre 100 en proactivos, máx +10 pts
        var bonusProactivos = pctProactivos > 100
            ? Math.Min(10m, (pctProactivos - 100) * 0.20m)
            : 0;

        var puntajeTotal = Math.Min(110m, puntProactivos + puntPasso + puntCierreAcc + puntCierreHall + bonusProactivos);

        return new PuntajeMesDto
        {
            ProyectoId = proyectoId,
            ProyectoNombre = proyecto ?? "",
            Mes = mes,
            Anio = anio,
            PctProactivos = Math.Round(pctProactivos, 1),
            PctPasso = Math.Round(pctPasso, 1),
            PctCierreAccidentes = Math.Round(pctCierreAccidentes, 1),
            PctCierreHallazgos = Math.Round(pctCierreHallazgos, 1),
            PuntajeProactivos = Math.Round(puntProactivos, 2),
            PuntajePasso = Math.Round(puntPasso, 2),
            PuntajeCierreAccidentes = Math.Round(puntCierreAcc, 2),
            PuntajeCierreHallazgos = Math.Round(puntCierreHall, 2),
            BonusProactivos = Math.Round(bonusProactivos, 2),
            PuntajeTotal = Math.Round(puntajeTotal, 1),
            AccidentesAbiertos = accidentesAbiertos,
            AccidentesTotales = accidentesTotales,
            HallazgosCerrados = hallazgosCerrados,
            HallazgosTotales = hallazgosTotales
        };
    }

    public async Task<List<PuntajeMesDto>> GetPuntajeTodosProyectosAsync(
        int mes, int anio, List<IndicadorProactivoProyectoDto>? seguimiento = null)
    {
        using var ctx = _factory.CreateDbContext();

        var hoy        = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var esMesActual = hoy.Month == mes && hoy.Year == anio;
        var fechaIni   = new DateTime(anio, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var fechaFin   = new DateTime(anio, mes, DateTime.DaysInMonth(anio, mes), 0, 0, 0, DateTimeKind.Utc).AddDays(1);
        var fechaCorte = esMesActual ? hoy.AddDays(1) : fechaFin;

        // "Proyecto activo" para SSOMA = habilitado en ss_proyecto_habilitado,
        // independiente del estado genérico de Project (ver ProyectoHabilitadoFeature).
        var proyectos = await ctx.Project
            .Where(p => ctx.SsProyectoHabilitado.Any(h => h.ProyectoId == p.ProjectId && h.State && h.Active))
            .Select(p => new { p.ProjectId, p.ProjectDescription })
            .ToListAsync();
        var proyIds = proyectos.Select(p => p.ProjectId).ToList();
        if (!proyIds.Any()) return [];

        // Bulk: seguimiento (proactivos) — reutiliza el resultado ya calculado si viene provisto
        // (evita repetir las 8 bulk queries cuando el controller ya lo tiene cacheado)
        var seguimientoTask = seguimiento != null
            ? Task.FromResult(seguimiento)
            : GetSeguimientoTodosProyectosAsync(mes, anio);

        // Bulk: PASSO — actividades teóricamente programadas este mes de ciclo vs
        // ejecutadas (misma lógica que GetPuntajeMesAsync / PasoService.GetResumenMesAsync;
        // ver el comentario en GetPuntajeMesAsync para el porqué no se puede contar
        // simplemente cuántas filas ssoma_paso_ejecucion ya existen para el mes).
        // Filtrar por Anio == anio (y excluir la plantilla corporativa, Anio = NULL):
        // sin esto, las instancias de PASO de otros años del mismo proyecto (p.ej. una
        // ya creada para 2027) contaminan el % del año/mes que se está consultando.
        var pasosConActividades = await ctx.SsomaPasos
            .Where(p => p.ProyectoId != null && proyIds.Contains(p.ProyectoId!.Value)
                     && !p.EsPlantilla && p.Anio == anio)
            .Include(p => p.Actividades.Where(a => a.Activo && a.DeletedAt == null))
                .ThenInclude(a => a.Ejecuciones)
            .ToListAsync();

        var pasoProgFinal = pasosConActividades
            .Where(p => p.ProyectoId.HasValue)
            .Select(p =>
            {
                var (programadas, ejecutadas) = CalcularPassoDelMes(p, mes, anio);
                return new { ProyectoId = p.ProyectoId!.Value, Total = programadas, Ejecutadas = ejecutadas };
            })
            .ToList();

        // Bulk: accidentes por proyecto — solo los ocurridos dentro del mes consultado
        var accBulk = await ctx.SsomaAccidenteIncidente
            .Where(a => proyIds.Contains(a.ProyectoId) && a.Fecha >= fechaIni && a.Fecha < fechaCorte)
            .GroupBy(a => new { a.ProyectoId, Cerrado = a.Estado == "Cerrado" })
            .Select(g => new { g.Key.ProyectoId, g.Key.Cerrado, Count = g.Count() })
            .ToListAsync();

        // Bulk: hallazgos por proyecto
        var hallBulk = await ctx.SsomaInspeccionHallazgo
            .Join(ctx.SsomaInspeccion, h => h.InspeccionId, i => i.Id,
                  (h, i) => new { h.Estado, i.ProyectoId, i.Fecha })
            .Where(x => proyIds.Contains(x.ProyectoId) && x.Fecha < fechaCorte)
            .GroupBy(x => new { x.ProyectoId, Cerrado = x.Estado == "Cerrado" })
            .Select(g => new { g.Key.ProyectoId, g.Key.Cerrado, Count = g.Count() })
            .ToListAsync();

        var seguimientoFinal = await seguimientoTask;

        var result = new List<PuntajeMesDto>();
        foreach (var proy in proyectos)
        {
            var pid = proy.ProjectId;
            var seg = seguimientoFinal.FirstOrDefault(s => s.ProyectoId == pid);
            var pctProactivos = seg != null ? seg.PctProactivoGeneral : 0m;

            var paso = pasoProgFinal.FirstOrDefault(p => p.ProyectoId == pid);
            var pctPasso = paso is { Total: > 0 } ? (decimal)paso.Ejecutadas / paso.Total * 100 : 0m;

            var accTotal = accBulk.Where(a => a.ProyectoId == pid).Sum(a => a.Count);
            var accCerr  = accBulk.Where(a => a.ProyectoId == pid && a.Cerrado).Sum(a => a.Count);
            var pctAcc   = accTotal > 0 ? (decimal)accCerr / accTotal * 100 : 0m;

            var hallTotal = hallBulk.Where(h => h.ProyectoId == pid).Sum(h => h.Count);
            var hallCerr  = hallBulk.Where(h => h.ProyectoId == pid && h.Cerrado).Sum(h => h.Count);
            var pctHall   = hallTotal > 0 ? (decimal)hallCerr / hallTotal * 100 : 0m;

            var puntProac = Math.Min(pctProactivos, 100) * 0.40m;
            var puntPasso = Math.Min(pctPasso, 100) * 0.25m;
            var puntAcc   = Math.Min(pctAcc, 100) * 0.20m;
            var puntHall  = Math.Min(pctHall, 100) * 0.15m;
            var bonus     = pctProactivos > 100 ? Math.Min(10m, (pctProactivos - 100) * 0.20m) : 0m;
            var total     = Math.Min(110m, puntProac + puntPasso + puntAcc + puntHall + bonus);

            result.Add(new PuntajeMesDto
            {
                ProyectoId = pid,
                ProyectoNombre = proy.ProjectDescription ?? "",
                Mes = mes, Anio = anio,
                PctProactivos = Math.Round(pctProactivos, 1),
                PctPasso = Math.Round(pctPasso, 1),
                PctCierreAccidentes = Math.Round(pctAcc, 1),
                PctCierreHallazgos = Math.Round(pctHall, 1),
                PuntajeProactivos = Math.Round(puntProac, 2),
                PuntajePasso = Math.Round(puntPasso, 2),
                PuntajeCierreAccidentes = Math.Round(puntAcc, 2),
                PuntajeCierreHallazgos = Math.Round(puntHall, 2),
                BonusProactivos = Math.Round(bonus, 2),
                PuntajeTotal = Math.Round(total, 1),
                AccidentesAbiertos = accTotal - accCerr,
                AccidentesTotales = accTotal,
                HallazgosCerrados = hallCerr,
                HallazgosTotales = hallTotal
            });
        }

        return result.OrderByDescending(p => p.PuntajeTotal).ToList();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS PRIVADOS
    // ─────────────────────────────────────────────────────────────────────────

    private static MetaEmpresaDto BuildMetaDto(
        int? empresaId, string tipo, string nombre,
        decimal prom, int dias,
        int metaRacs, int metaOpt, int metaAts, int metaCharlas, int metaInsp,
        int actualRacsReportados, int actualRacsAtribuidos, int actualRacsCerrados,
        int actualOpt, int actualAts, int actualCharlas, int actualInsp)
    {
        // "Reportados" = RACs que ESTA empresa levantó/reportó (empresa reportante) — indicador
        // proactivo de reporte. "Atribuidos"/"Cerrados" = RACs que le fueron cargados a ella
        // (empresa reportada) y cuántos de esos ya cerró — indicador de cumplimiento. Son
        // poblaciones distintas y no deben mezclarse en el mismo tope.
        var tieneActividadReal = actualRacsReportados > 0 || actualRacsAtribuidos > 0
            || actualOpt > 0 || actualAts > 0 || actualCharlas > 0 || actualInsp > 0;

        // El "Ejec" mostrado (Actual*) es el conteo REAL (sin tope) para TODOS los
        // indicadores — ejecutar de más es un comportamiento proactivo deseable y no debe
        // ocultarse (antes OPT/ATS/Charlas/Insp se topeaban a la meta y una usuaria con 16
        // auditorías veía "Ejec: 5"). El % de cada empresa sí se topea a su propia meta
        // (estos topes solo alimentan los Pct*, no los números visibles).
        var racsCerTope = Math.Min(actualRacsCerrados, actualRacsAtribuidos);
        var optTope = Math.Min(actualOpt, metaOpt);
        var atsTope = Math.Min(actualAts, metaAts);
        var charlasTope = Math.Min(actualCharlas, metaCharlas);
        var inspTope = Math.Min(actualInsp, metaInsp);

        var pctRacs = Pct(actualRacsReportados, metaRacs);
        var pctRacsCer = Pct(racsCerTope, actualRacsAtribuidos);
        var pctOpt = Pct(optTope, metaOpt);
        var pctAts = Pct(atsTope, metaAts);
        var pctCharlas = Pct(charlasTope, metaCharlas);
        var pctInsp = Pct(inspTope, metaInsp);

        // Un indicador SIN meta (Prog = 0) no es un 0% de cumplimiento: es "no aplica" —
        // exactamente lo que la tarjeta ya muestra como "N/A" cuando Prog llega en 0. Antes
        // se promediaba siempre sobre 6, así que cada indicador no aplicable metía un 0 y
        // hundía el % general: una empresa sin tareo en el mes (todas sus metas en 0) salía
        // 0% aunque hubiera ejecutado OPTs. Ojo: la "meta" de RACs cerrados no es una meta
        // configurada sino los RACs que le fueron atribuidos; sin atribuidos, cerrar no aplica.
        var aplica = new[]
        {
            (metaRacs > 0, pctRacs),
            (actualRacsAtribuidos > 0, pctRacsCer),
            (metaOpt > 0, pctOpt),
            (metaAts > 0, pctAts),
            (metaCharlas > 0, pctCharlas),
            (metaInsp > 0, pctInsp),
        };
        var pctGeneral = IndicadorProactivoCalculo.PromedioAplicables(aplica);

        return new MetaEmpresaDto
        {
            EmpresaId = empresaId,
            EmpresaTipo = tipo,
            EmpresaNombre = nombre,
            PromedioTrabajadores = Math.Round(prom, 1),
            DiasLaborados = dias,
            // Activa si el promedio de trabajadores tareados es 5 o más, O si ya está
            // ejecutando herramientas de gestión proactiva (con el dato real, sin tope).
            EsActiva = prom >= 5 || tieneActividadReal,
            MetaRacs = metaRacs,
            MetaOpt = metaOpt,
            MetaAts = metaAts,
            MetaCharlas = metaCharlas,
            MetaInspecciones = metaInsp,
            ActualRacs = actualRacsReportados,
            ActualRacsAtribuidos = actualRacsAtribuidos,
            ActualRacsCerrados = racsCerTope,
            ActualOpt = actualOpt,
            ActualAts = actualAts,
            ActualCharlas = actualCharlas,
            ActualInspecciones = actualInsp,
            PctRacs = Math.Round(pctRacs, 1),
            PctRacsCerrados = Math.Round(pctRacsCer, 1),
            PctOpt = Math.Round(pctOpt, 1),
            PctAts = Math.Round(pctAts, 1),
            PctCharlas = Math.Round(pctCharlas, 1),
            PctInspecciones = Math.Round(pctInsp, 1),
            PctProactivoGeneral = Math.Round(pctGeneral, 1),
            IndicadoresAplicables = aplica.Count(a => a.Item1)
        };
    }

    // Fórmula única de % e "indicador aplicable" — ver IndicadorProactivoCalculo.
    private static decimal Pct(int actual, int meta) => IndicadorProactivoCalculo.Pct(actual, meta);

    // ─────────────────────────────────────────────────────────────────────────
    // PASSO — "mes de ciclo": réplica de la lógica de
    // PasoService.GetResumenMesAsync (Features/SsomaModule/PasoFeature) para que
    // el % del dashboard de indicadores coincida con lo que muestra /paso/lista.
    // ─────────────────────────────────────────────────────────────────────────

    private static bool EsMesPlanificadoPasso(string frec, int mes, int mesInicio)
    {
        var diff = mes - mesInicio;
        if (diff < 0) return false;
        return frec switch
        {
            "Mensual" => true,
            "Bimestral" => diff % 2 == 0,
            "Trimestral" => diff % 3 == 0,
            "Semestral" => diff % 6 == 0,
            "Anual" or "Unica" => diff == 0,
            _ => false
        };
    }

    private static (int Programadas, int Ejecutadas) CalcularPassoDelMes(SsomaPaso paso, int mes, int anio)
    {
        int pasoAnio = paso.Anio ?? anio;
        // "Anio" es siempre el año en que arranca el ciclo (sin importar el mes de inicio) —
        // debe coincidir con la regla de PasoService (Features/SsomaModule/PasoFeature).
        int cicloStartYear = pasoAnio;
        int cicloStartAbs = cicloStartYear * 12 + paso.MesInicio - 1;
        int cicloEndAbs = cicloStartAbs + 11;
        int requestedAbs = anio * 12 + mes - 1;

        if (requestedAbs < cicloStartAbs || requestedAbs > cicloEndAbs) return (0, 0);

        int mesCiclo = requestedAbs - cicloStartAbs + 1;
        int programadas = 0, ejecutadas = 0;

        foreach (var act in paso.Actividades)
        {
            var tieneReprogramadaEnOtroMes = act.Ejecuciones.Any(e =>
                e.Estado == "Programado" &&
                !(e.FechaProgramada.Month == mes && e.FechaProgramada.Year == anio));

            var tieneEjecucionEsteMes = act.Ejecuciones.Any(e =>
                e.Estado == "Programado" &&
                e.FechaProgramada.Month == mes &&
                e.FechaProgramada.Year == anio);

            bool incluir;
            if (tieneEjecucionEsteMes) incluir = true;
            else if (tieneReprogramadaEnOtroMes) incluir = false;
            else if (act.Frecuencia == "Unica")
                incluir = act.MesInicio == mesCiclo || act.Ejecuciones.Any(e =>
                    e.FechaProgramada.Month == mes && e.FechaProgramada.Year == anio);
            else
                incluir = act.MesInicio <= mesCiclo && act.MesFin >= mesCiclo
                          && EsMesPlanificadoPasso(act.Frecuencia, mesCiclo, act.MesInicio);

            if (!incluir) continue;
            programadas++;

            var ej = act.Ejecuciones.FirstOrDefault(e =>
                e.FechaProgramada.Month == mes && e.FechaProgramada.Year == anio);
            if (ej?.Estado == "Ejecutado") ejecutadas++;
        }

        return (programadas, ejecutadas);
    }

    private static HashSet<DateTime> FeriadosPeruDateTime(int anio)
    {
        var easter = CalcularPascua(anio);
        return new HashSet<DateTime>
        {
            new(anio, 1, 1),
            easter.AddDays(-3),     // Jueves Santo
            easter.AddDays(-2),     // Viernes Santo
            new(anio, 5, 1),
            new(anio, 6, 7),
            new(anio, 6, 29),
            new(anio, 7, 23),
            new(anio, 7, 28),
            new(anio, 7, 29),
            new(anio, 8, 30),
            new(anio, 10, 8),
            new(anio, 11, 1),
            new(anio, 12, 8),
            new(anio, 12, 9),
            new(anio, 12, 25)
        };
    }

    private static DateTime CalcularPascua(int anio)
    {
        int a = anio % 19, b = anio / 100, c = anio % 100;
        int d = b / 4, e = b % 4, f = (b + 8) / 25, g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4, k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int mes = (h + l - 7 * m + 114) / 31;
        int dia = ((h + l - 7 * m + 114) % 31) + 1;
        return new DateTime(anio, mes, dia, 0, 0, 0, DateTimeKind.Utc);
    }

    private static bool EsDiaHabilDateTime(DateTime fecha, HashSet<DateTime> feriados)
        => fecha.DayOfWeek != DayOfWeek.Sunday && !feriados.Contains(fecha.Date);

    private static int ContarDiasHabilesConPresenciaDateTime(List<DateTime> fechas, int mes, int anio)
    {
        var feriados = FeriadosPeruDateTime(anio);
        return fechas
            .Where(f => f.Month == mes && f.Year == anio && EsDiaHabilDateTime(f, feriados))
            .Select(f => f.Date)
            .Distinct()
            .Count();
    }

    private static int ContarDiasHabilesDelMes(int mes, int anio)
    {
        var feriados = FeriadosPeruDateTime(anio);
        var totalDias = DateTime.DaysInMonth(anio, mes);
        var habiles = 0;
        for (var dia = 1; dia <= totalDias; dia++)
        {
            if (EsDiaHabilDateTime(new DateTime(anio, mes, dia), feriados)) habiles++;
        }
        return habiles;
    }

    // Prorratea la meta mensual fija (15) por los días que la contratista realmente tareó
    // sobre el total de días hábiles del mes (ej. 8 días tareados de ~22 hábiles → meta ≈ 6).
    // Si NO tareó ningún día se mantiene el fijo de 15 — sigue siendo la única meta
    // independiente del tareo, para sostener el indicador en obras sin tareo cargado
    // (ver ago-2026: KAURÍ 15% → 0%, MÁXIMO ABRIL 30% → 0% al condicionarla).
    private static int CalcularMetaInspeccionesContratista(int diasTareados, int diasHabilesMes)
    {
        if (diasTareados <= 0 || diasHabilesMes <= 0) return MetaInspeccionesContratista;
        return (int)Math.Ceiling(MetaInspeccionesContratista * (decimal)diasTareados / diasHabilesMes);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // INDICADORES REACTIVOS IF / IG / IA
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<IndicadorReactivoProyectoDto> GetIndicadoresReactivosAsync(int proyectoId, int mes, int anio)
    {
        using var ctx = _factory.CreateDbContext();
        return await CalcularReactivosAsync(ctx, proyectoId, mes, anio);
    }

    public async Task<MetaAnualDto> GetMetaAnualAsync(int anio)
    {
        using var ctx = _factory.CreateDbContext();
        var meta = await ctx.SsomaMetaAnual.FirstOrDefaultAsync(m => m.Anio == anio);
        return new MetaAnualDto
        {
            Anio = anio,
            MetaIndiceFrecuencia = meta?.MetaIndiceFrecuencia,
            MetaIndiceGravedad = meta?.MetaIndiceGravedad,
            MetaIndiceAccidentabilidad = meta?.MetaIndiceAccidentabilidad,
        };
    }

    public async Task<MetaAnualDto> GuardarMetaAnualAsync(GuardarMetaAnualRequest request, int userId)
    {
        using var ctx = _factory.CreateDbContext();
        var meta = await ctx.SsomaMetaAnual.FirstOrDefaultAsync(m => m.Anio == request.Anio);
        if (meta == null)
        {
            meta = new SsomaMetaAnual { Anio = request.Anio };
            ctx.SsomaMetaAnual.Add(meta);
        }
        meta.MetaIndiceFrecuencia = request.MetaIndiceFrecuencia;
        meta.MetaIndiceGravedad = request.MetaIndiceGravedad;
        meta.MetaIndiceAccidentabilidad = request.MetaIndiceAccidentabilidad;
        meta.ActualizadoPorId = userId;
        meta.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();

        return new MetaAnualDto
        {
            Anio = meta.Anio,
            MetaIndiceFrecuencia = meta.MetaIndiceFrecuencia,
            MetaIndiceGravedad = meta.MetaIndiceGravedad,
            MetaIndiceAccidentabilidad = meta.MetaIndiceAccidentabilidad,
        };
    }

    public async Task<List<IndicadorReactivoProyectoDto>> GetIndicadoresReactivosTodosAsync(int mes, int anio)
    {
        using var ctx = _factory.CreateDbContext();

        // Los reactivos (IF/IG/IA) deben considerar TODAS las horas-hombre del período,
        // sin importar si el proyecto está habilitado/activo o ya finalizó — a diferencia
        // de los indicadores proactivos, aquí no se filtra por ss_proyecto_habilitado.
        // Se toman: los proyectos habilitados en SSOMA (aunque no tengan horas este mes,
        // para no desaparecer de la vista) + cualquier proyecto con tareo registrado en
        // el mes (incluye proyectos ya finalizados que sí trabajaron horas ese período).
        var habilitadosIds = await ctx.SsProyectoHabilitado
            .Where(h => h.State && h.Active)
            .Select(h => h.ProyectoId)
            .ToListAsync();

        // Ojo: se filtra por AÑO (no por mes) — esta lista de proyectos se usa para
        // calcular los 3 niveles (Mes/Año/Total). Si se filtrara solo por el mes
        // consultado, un proyecto sin horas ESE mes puntual (pero con horas en
        // otros meses del año) quedaría afuera y su columna "Año" desaparecería
        // del total, aunque la pantalla de Horas Hombre sí las cuenta.
        var proyectosConTareoIds = await ctx.SsTareoDetalleCasa
            .Where(d => d.Tareo!.Fecha.Year == anio && d.CantidadPersonas > 0)
            .Select(d => d.Tareo!.ProyectoId)
            .Union(ctx.SsTareoDetalleContratista
                .Where(d => d.Tareo!.Fecha.Year == anio && d.CantidadPersonas > 0)
                .Select(d => d.Tareo!.ProyectoId))
            .ToListAsync();

        var proyIds = habilitadosIds.Union(proyectosConTareoIds).ToList();
        if (!proyIds.Any()) return [];

        var proyectos = await ctx.Project
            .Where(p => proyIds.Contains(p.ProjectId))
            .Select(p => new { p.ProjectId, p.ProjectDescription })
            .ToListAsync();

        // Se calculan los 3 niveles que necesita el dashboard: el mes consultado,
        // el año completo (para que IF/IG/IA no se disparen con una HHT chica de
        // un solo mes) y el histórico completo del proyecto. Un solo fetch a la base,
        // agregado 3 veces en memoria (antes se repetía la consulta por cada nivel).
        var crudo     = await FetchReactivosCrudoAsync(ctx, proyIds);
        var mesBulk   = AgregarReactivos(crudo, anio, mes);
        var anioBulk  = AgregarReactivos(crudo, anio, null);
        var totalBulk = AgregarReactivos(crudo, null, null);

        // ── Agregar por proyecto ─────────────────────────────────────────────────
        var resultado = new List<IndicadorReactivoProyectoDto>();
        foreach (var proy in proyectos)
        {
            var pid = proy.ProjectId;
            var (hhtMes, accMes, diasMes) = mesBulk.GetValueOrDefault(pid, (0m, 0, 0));
            var (hhtAnio, accAnio, diasAnio) = anioBulk.GetValueOrDefault(pid, (0m, 0, 0));
            var (hhtTotal, accTotal, diasTotal) = totalBulk.GetValueOrDefault(pid, (0m, 0, 0));

            var (ifMes, igMes, iaMes) = CalcularIndicesReactivos(hhtMes, accMes, diasMes);
            var (ifAnio, igAnio, iaAnio) = CalcularIndicesReactivos(hhtAnio, accAnio, diasAnio);
            var (ifTotal, igTotal, iaTotal) = CalcularIndicesReactivos(hhtTotal, accTotal, diasTotal);

            resultado.Add(new IndicadorReactivoProyectoDto
            {
                ProyectoId = pid,
                ProyectoNombre = proy.ProjectDescription ?? "",
                Mes = mes,
                Anio = anio,

                HorasHombreTrabajadas = hhtMes,
                TotalAccidentes = accMes,
                TotalDiasPerdidos = diasMes,
                IndiceFrecuencia = ifMes,
                IndiceGravedad = igMes,
                IndiceAccidentabilidad = iaMes,

                HorasHombreTrabajadasAnio = hhtAnio,
                TotalAccidentesAnio = accAnio,
                TotalDiasPerdidosAnio = diasAnio,
                IndiceFrecuenciaAnio = ifAnio,
                IndiceGravedadAnio = igAnio,
                IndiceAccidentabilidadAnio = iaAnio,

                HorasHombreTrabajadasTotal = hhtTotal,
                TotalAccidentesTotal = accTotal,
                TotalDiasPerdidosTotal = diasTotal,
                IndiceFrecuenciaTotal = ifTotal,
                IndiceGravedadTotal = igTotal,
                IndiceAccidentabilidadTotal = iaTotal,
            });
        }
        return resultado;
    }

    private static async Task<IndicadorReactivoProyectoDto> CalcularReactivosAsync(
        AppDbContext ctx, int proyectoId, int mes, int anio)
    {
        var proyectoNombre = await ctx.Project
            .Where(p => p.ProjectId == proyectoId)
            .Select(p => p.ProjectDescription ?? "")
            .FirstOrDefaultAsync() ?? "";

        var proyIds = new List<int> { proyectoId };
        (decimal Hht, int Accidentes, int DiasPerdidos) vacio = (0m, 0, 0);
        var crudo       = await FetchReactivosCrudoAsync(ctx, proyIds);
        var mesResult   = AgregarReactivos(crudo, anio, mes).GetValueOrDefault(proyectoId, vacio);
        var anioResult  = AgregarReactivos(crudo, anio, null).GetValueOrDefault(proyectoId, vacio);
        var totalResult = AgregarReactivos(crudo, null, null).GetValueOrDefault(proyectoId, vacio);

        var (ifMes, igMes, iaMes) = CalcularIndicesReactivos(mesResult.Hht, mesResult.Accidentes, mesResult.DiasPerdidos);
        var (ifAnio, igAnio, iaAnio) = CalcularIndicesReactivos(anioResult.Hht, anioResult.Accidentes, anioResult.DiasPerdidos);
        var (ifTotal, igTotal, iaTotal) = CalcularIndicesReactivos(totalResult.Hht, totalResult.Accidentes, totalResult.DiasPerdidos);

        return new IndicadorReactivoProyectoDto
        {
            ProyectoId = proyectoId,
            ProyectoNombre = proyectoNombre,
            Mes = mes,
            Anio = anio,

            HorasHombreTrabajadas = mesResult.Hht,
            TotalAccidentes = mesResult.Accidentes,
            TotalDiasPerdidos = mesResult.DiasPerdidos,
            IndiceFrecuencia = ifMes,
            IndiceGravedad = igMes,
            IndiceAccidentabilidad = iaMes,

            HorasHombreTrabajadasAnio = anioResult.Hht,
            TotalAccidentesAnio = anioResult.Accidentes,
            TotalDiasPerdidosAnio = anioResult.DiasPerdidos,
            IndiceFrecuenciaAnio = ifAnio,
            IndiceGravedadAnio = igAnio,
            IndiceAccidentabilidadAnio = iaAnio,

            HorasHombreTrabajadasTotal = totalResult.Hht,
            TotalAccidentesTotal = totalResult.Accidentes,
            TotalDiasPerdidosTotal = totalResult.DiasPerdidos,
            IndiceFrecuenciaTotal = ifTotal,
            IndiceGravedadTotal = igTotal,
            IndiceAccidentabilidadTotal = iaTotal,
        };
    }

    private static (decimal If, decimal Ig, decimal Ia) CalcularIndicesReactivos(decimal hht, int accidentes, int diasPerdidos)
    {
        if (hht <= 0) return (0, 0, 0);
        var if_ = Math.Round((decimal)accidentes * 1_000_000m / hht, 2);
        var ig  = Math.Round((decimal)diasPerdidos * 1_000_000m / hht, 2);
        var ia  = Math.Round(if_ * ig / 1000m, 2);
        return (if_, ig, ia);
    }

    // Datos crudos (sin filtrar por período) para calcular reactivos. Se traen UNA sola vez
    // por request y se agregan en memoria para Mes/Año/Total — antes se repetía la misma
    // consulta a la base 3 veces (una por nivel), lo que hacía lenta cada carga del dashboard.
    private sealed class ReactivosCrudo
    {
        public List<(int ProyectoId, DateOnly Fecha, long Total)> PersonasDia { get; init; } = [];
        public List<(int ProyectoId, DateOnly Fecha, int DiasPerdidos)> AccTopico { get; init; } = [];
        public List<(int Id, int ProyectoId, DateTime Fecha)> FlashSinVincular { get; init; } = [];
        public List<(int AccidenteIncidenteId, DateTime FechaInicio, DateTime FechaFin)> DescansosFlash { get; init; } = [];
    }

    private static async Task<ReactivosCrudo> FetchReactivosCrudoAsync(AppDbContext ctx, List<int> proyIds)
    {
        var personasCasaPorDia = await ctx.SsTareoDetalleCasa
            .Where(d => proyIds.Contains(d.Tareo!.ProyectoId) && d.CantidadPersonas > 0)
            .GroupBy(d => new { d.Tareo!.ProyectoId, d.Tareo.Fecha })
            .Select(g => new { g.Key.ProyectoId, g.Key.Fecha, Total = g.Sum(x => (long)x.CantidadPersonas) })
            .ToListAsync();

        var personasContrPorDia = await ctx.SsTareoDetalleContratista
            .Where(d => proyIds.Contains(d.Tareo!.ProyectoId) && d.CantidadPersonas > 0)
            .GroupBy(d => new { d.Tareo!.ProyectoId, d.Tareo.Fecha })
            .Select(g => new { g.Key.ProyectoId, g.Key.Fecha, Total = g.Sum(x => (long)x.CantidadPersonas) })
            .ToListAsync();

        var personasDia = personasCasaPorDia
            .Select(x => (x.ProyectoId, x.Fecha, x.Total))
            .Concat(personasContrPorDia.Select(x => (x.ProyectoId, x.Fecha, x.Total)))
            .ToList();

        var accTopico = await ctx.SsAccidenteTrabajo
            .Where(a => a.ProyectoId != null && proyIds.Contains(a.ProyectoId!.Value))
            .Select(a => new {
                ProyectoId = a.ProyectoId!.Value,
                a.FechaAccidente,
                DiasPerdidos = (int?)(a.DiasDescansoReales ?? a.DiasDescansoEstimados) ?? 0
            })
            .ToListAsync();

        // Flash Report tipo "AC" sin vincular a un accidente de Tópico (evita duplicar).
        var flashIdsYaVinculados = await ctx.SsAccidenteTrabajo
            .Where(a => a.FlashReportId != null)
            .Select(a => a.FlashReportId!.Value)
            .ToListAsync();

        var flashSinVincular = await (
            from fr in ctx.SsomaAccidenteIncidente
            join t in ctx.SsomaFlashTipo on fr.TipoId equals t.Id
            where proyIds.Contains(fr.ProyectoId)
               && t.Codigo == "AC"
               && !flashIdsYaVinculados.Contains(fr.Id)
            select new { fr.Id, fr.ProyectoId, fr.Fecha }
        ).ToListAsync();

        var flashIds = flashSinVincular.Select(f => f.Id).ToList();

        // Días perdidos de Flash Report: se toman de ssoma_flash_descanso (los descansos
        // cargados directamente en el propio Flash Report), no de la investigación RM050 —
        // es la misma fuente que ya usa la lista de Accidentes e Incidentes.
        var descansosFlash = flashIds.Any()
            ? await ctx.SsomaFlashDescanso
                .Where(d => flashIds.Contains(d.AccidenteIncidenteId))
                .Select(d => new { d.AccidenteIncidenteId, d.FechaInicio, d.FechaFin })
                .ToListAsync()
            : [];

        return new ReactivosCrudo
        {
            PersonasDia = personasDia,
            AccTopico = accTopico.Select(a => (a.ProyectoId, a.FechaAccidente, a.DiasPerdidos)).ToList(),
            FlashSinVincular = flashSinVincular.Select(f => (f.Id, f.ProyectoId, f.Fecha)).ToList(),
            DescansosFlash = descansosFlash.Select(d => (d.AccidenteIncidenteId, d.FechaInicio, d.FechaFin)).ToList(),
        };
    }

    // Agrega en memoria (sin tocar la base) el crudo ya traído para un período dado.
    // anio=null → histórico completo. mes=null (con anio) → año completo.
    private static Dictionary<int, (decimal Hht, int Accidentes, int DiasPerdidos)> AgregarReactivos(
        ReactivosCrudo crudo, int? anio, int? mes)
    {
        bool EnPeriodo(DateOnly f) => (!anio.HasValue || f.Year == anio.Value) && (!mes.HasValue || f.Month == mes.Value);
        bool EnPeriodoDt(DateTime f) => (!anio.HasValue || f.Year == anio.Value) && (!mes.HasValue || f.Month == mes.Value);

        var hhtPorProyecto = crudo.PersonasDia
            .Where(x => EnPeriodo(x.Fecha))
            .GroupBy(x => x.ProyectoId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Total * HorarioLaboralCalculator.HorasPorDia(x.Fecha)));

        var accTopicoBulk = crudo.AccTopico
            .Where(x => EnPeriodo(x.Fecha))
            .GroupBy(x => x.ProyectoId)
            .ToDictionary(g => g.Key, g => (Count: g.Count(), DiasPerdidos: g.Sum(x => x.DiasPerdidos)));

        var flashSinVincular = crudo.FlashSinVincular.Where(f => EnPeriodoDt(f.Fecha)).ToList();
        var flashProyMap = flashSinVincular.ToDictionary(f => f.Id, f => f.ProyectoId);
        var flashIds = flashSinVincular.Select(f => f.Id).ToHashSet();

        var flashCountBulk = flashSinVincular
            .GroupBy(f => f.ProyectoId)
            .ToDictionary(g => g.Key, g => g.Count());

        var diasPerdidosFlashBulk = crudo.DescansosFlash
            .Where(x => flashIds.Contains(x.AccidenteIncidenteId))
            .GroupBy(x => flashProyMap.GetValueOrDefault(x.AccidenteIncidenteId))
            .ToDictionary(g => g.Key, g => g.Sum(x => (x.FechaFin.Date - x.FechaInicio.Date).Days + 1));

        var proyIdsTodos = hhtPorProyecto.Keys
            .Concat(accTopicoBulk.Keys)
            .Concat(flashCountBulk.Keys)
            .Distinct();

        var resultado = new Dictionary<int, (decimal Hht, int Accidentes, int DiasPerdidos)>();
        foreach (var pid in proyIdsTodos)
        {
            var hht = hhtPorProyecto.GetValueOrDefault(pid, 0m);
            var (accCount, accDias) = accTopicoBulk.GetValueOrDefault(pid, (0, 0));
            var totalAccidentes = accCount + flashCountBulk.GetValueOrDefault(pid, 0);
            var totalDiasPerdidos = accDias + diasPerdidosFlashBulk.GetValueOrDefault(pid, 0);
            resultado[pid] = (hht, totalAccidentes, totalDiasPerdidos);
        }
        return resultado;
    }
}

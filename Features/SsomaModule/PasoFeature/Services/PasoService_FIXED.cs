using System.Text.Json;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using Abril_Backend.Features.Ssoma.Paso.Dtos;
using Abril_Backend.Features.Ssoma.Paso.Entities;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Abril_Backend.Features.Ssoma.Paso.Services;

public class PasoService : IPasoService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ISharePointHabService _sharePoint;
    private readonly ILogger<PasoService> _logger;

    public PasoService(IDbContextFactory<AppDbContext> factory, ISharePointHabService sharePoint, ILogger<PasoService> logger)
    {
        _factory = factory;
        _sharePoint = sharePoint;
        _logger = logger;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static decimal CalcularSpi(int plan, int ejec) =>
        plan == 0 ? 1m : Math.Round((decimal)ejec / plan, 2);

    private static string SpiColor(decimal spi) =>
        spi >= 0.95m ? "verde" : spi >= 0.80m ? "amarillo" : "rojo";

    private static bool EsMesPlanificado(string frec, int mes, int mesInicio)
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

    private static int AjustarMes(int mesOrig, int mesInicioPlantilla, int mesInicioInstancia)
    {
        var offset = mesInicioInstancia - mesInicioPlantilla;
        return ((mesOrig + offset - 1) % 12 + 12) % 12 + 1;
    }

    private static DateOnly UltimoDiaMes(int anio, int mes) =>
        new DateOnly(anio, mes, DateTime.DaysInMonth(anio, mes));

    private static PasoListItemDto BuildListItem(
        SsomaPaso p,
        Dictionary<int, string> proyectos,
        Dictionary<int, string> personas)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var acts = p.Actividades.Where(a => a.Activo).ToList();
        var ejs = acts.SelectMany(a => a.Ejecuciones).ToList();
        var totalPlan = ejs.Count;
        var totalEj = ejs.Count(e => e.Estado == "Ejecutado");
        var totalVenc = ejs.Count(e => e.Estado == "Vencido");
        var planHoy = ejs.Count(e => e.FechaProgramada <= hoy);
        var ejHoy = ejs.Count(e => e.Estado == "Ejecutado" && e.FechaProgramada <= hoy);
        var spi = CalcularSpi(planHoy, ejHoy);
        var pct = totalPlan == 0 ? 0m : Math.Round((decimal)totalEj / totalPlan * 100, 1);

        return new PasoListItemDto
        {
            Id = p.Id,
            ProyectoId = p.ProyectoId,
            ProyectoNombre = p.ProyectoId.HasValue ? proyectos.GetValueOrDefault(p.ProyectoId.Value) : null,
            PlantillaId = p.PlantillaId,
            Nombre = p.Nombre,
            Anio = p.Anio,
            MesInicio = p.MesInicio,
            EsPlantilla = p.EsPlantilla,
            Estado = p.Estado,
            AprobadoPorNombre = p.AprobadoPor.HasValue ? personas.GetValueOrDefault(p.AprobadoPor.Value) : null,
            AprobadoEn = p.AprobadoEn,
            CreatedAt = p.CreatedAt,
            TotalActividades = acts.Count,
            TotalPlanificadas = totalPlan,
            TotalEjecutadas = totalEj,
            TotalVencidas = totalVenc,
            Spi = spi,
            PorcentajeAvance = pct
        };
    }

    private static PasoEjecucionDto MapEjecucion(SsomaPasoEjecucion e, Dictionary<int, string> personas) =>
        new()
        {
            Id = e.Id,
            ActividadId = e.ActividadId,
            FechaProgramada = e.FechaProgramada,
            FechaVerificacion = e.FechaVerificacion,
            FechaEjecutada = e.FechaEjecutada,
            FechaReprogramada = e.FechaReprogramada,
            MotivoReprogramacion = e.MotivoReprogramacion,
            Estado = e.Estado,
            Observaciones = e.Observaciones,
            ParticipantesCount = e.ParticipantesCount,
            EvidenciaNombre = e.EvidenciaNombre,
            EvidenciaUrl = e.EvidenciaUrl,
            EvidenciaSpId = e.EvidenciaSpId,
            RegistradoPorNombre = e.RegistradoPor.HasValue ? personas.GetValueOrDefault(e.RegistradoPor.Value) : null,
            CreatedAt = e.CreatedAt,
            Archivos = e.Archivos.OrderBy(a => a.Orden).Select(a => new PasoEjecucionArchivoDto
            {
                Id = a.Id,
                EjecucionId = a.EjecucionId,
                ArchivoUrl = a.ArchivoUrl,
                ArchivoNombre = a.ArchivoNombre,
                ArchivoSpId = a.ArchivoSpId,
                Orden = a.Orden,
                CreatedAt = a.CreatedAt
            }).ToList()
        };

    private static PasoActividadDto MapActividad(
        SsomaPasoActividad a,
        Dictionary<int, string> personas)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var total = a.Ejecuciones.Count;
        var compl = a.Ejecuciones.Count(e => e.Estado == "Ejecutado");
        var venc = a.Ejecuciones.Count(e => e.Estado == "Vencido");
        var ph = a.Ejecuciones.Count(e => e.FechaProgramada <= hoy);
        var eh = a.Ejecuciones.Count(e => e.Estado == "Ejecutado" && e.FechaProgramada <= hoy);

        return new PasoActividadDto
        {
            Id = a.Id,
            PasoId = a.PasoId,
            CategoriaId = a.CategoriaId,
            CategoriaNombre = a.Categoria?.Nombre ?? "",
            CategoriaAmbito = a.Categoria?.Ambito ?? "",
            CategoriaIcono = a.Categoria?.Icono,
            Nombre = a.Nombre,
            Descripcion = a.Descripcion,
            Alcance = a.Alcance,
            Frecuencia = a.Frecuencia,
            ResponsableId = a.ResponsableId,
            ResponsableNombre = a.ResponsableId.HasValue ? personas.GetValueOrDefault(a.ResponsableId.Value) : null,
            ResponsableTexto = a.ResponsableTexto,
            MesInicio = a.MesInicio,
            MesFin = a.MesFin,
            CantidadPlanificada = a.CantidadPlanificada,
            Horas = a.Horas,
            Recursos = a.Recursos,
            Indicador = a.Indicador,
            Meta = a.Meta,
            Orden = a.Orden,
            Activo = a.Activo,
            EjecucionesTotal = total,
            EjecucionesCompletadas = compl,
            EjecucionesVencidas = venc,
            Spi = CalcularSpi(ph, eh),
            Ejecuciones = a.Ejecuciones.Select(e => MapEjecucion(e, personas)).ToList()
        };
    }

    // ── Categorías ────────────────────────────────────────────────────────────

    public async Task<List<PasoCategoriaDto>> GetCategoriasAsync()
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsomaPasoCategorias
            .Where(c => c.Activo)
            .OrderBy(c => c.Ambito).ThenBy(c => c.Nombre)
            .Select(c => new PasoCategoriaDto { Id = c.Id, Nombre = c.Nombre, Ambito = c.Ambito, Icono = c.Icono })
            .ToListAsync();
    }

    // ── Lista ─────────────────────────────────────────────────────────────────

    public async Task<PagedResult<PasoListItemDto>> GetListAsync(PasoListQuery q)
    {
        using var ctx = _factory.CreateDbContext();
        var query = ctx.SsomaPasos.AsQueryable();

        if (q.ProyectoId.HasValue) query = query.Where(p => p.ProyectoId == q.ProyectoId);
        // FIX-B7: when filtering by anio, exclude plantilla corporativa (anio=NULL)
        // unless EsPlantilla is explicitly requested
        if (q.Anio.HasValue) query = query.Where(p => !p.EsPlantilla && p.Anio == q.Anio);
        if (!string.IsNullOrEmpty(q.Estado)) query = query.Where(p => p.Estado == q.Estado);
        if (q.EsPlantilla.HasValue) query = query.Where(p => p.EsPlantilla == q.EsPlantilla);

        var total = await query.CountAsync();
        var pasos = await query
            .Include(p => p.Actividades.Where(a => a.Activo && a.DeletedAt == null))
                .ThenInclude(a => a.Ejecuciones)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync();

        var proyIds = pasos.Where(p => p.ProyectoId.HasValue).Select(p => p.ProyectoId!.Value).Distinct().ToList();
        var proyectos = await ctx.Project
            .Where(pr => proyIds.Contains(pr.ProjectId))
            .ToDictionaryAsync(pr => pr.ProjectId, pr => pr.ProjectDescription ?? "");

        var userIds = pasos.Where(p => p.AprobadoPor.HasValue).Select(p => p.AprobadoPor!.Value).Distinct().ToList();
        var personas = await ctx.Person
            .Where(p => p.UserId.HasValue && userIds.Contains(p.UserId.Value))
            .ToDictionaryAsync(p => p.UserId!.Value, p => p.FullName ?? "");

        return new PagedResult<PasoListItemDto>
        {
            Items = pasos.Select(p => BuildListItem(p, proyectos, personas)).ToList(),
            Total = total,
            Page = q.Page,
            PageSize = q.PageSize
        };
    }

    // ── Detalle ───────────────────────────────────────────────────────────────

    public async Task<PasoDetalleDto?> GetDetalleAsync(int id)
    {
        using var ctx = _factory.CreateDbContext();
        var paso = await ctx.SsomaPasos
            .Include(p => p.Actividades.Where(a => a.Activo && a.DeletedAt == null))
                .ThenInclude(a => a.Categoria)
            .Include(p => p.Actividades.Where(a => a.Activo && a.DeletedAt == null))
                .ThenInclude(a => a.Ejecuciones)
                    .ThenInclude(e => e.Archivos)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (paso is null) return null;

        var userIds = new HashSet<int>();
        if (paso.AprobadoPor.HasValue) userIds.Add(paso.AprobadoPor.Value);
        foreach (var a in paso.Actividades)
        {
            if (a.ResponsableId.HasValue) userIds.Add(a.ResponsableId.Value);
            foreach (var e in a.Ejecuciones)
                if (e.RegistradoPor.HasValue) userIds.Add(e.RegistradoPor.Value);
        }

        var personas = await ctx.Person
            .Where(p => p.UserId.HasValue && userIds.Contains(p.UserId.Value))
            .ToDictionaryAsync(p => p.UserId!.Value, p => p.FullName ?? "");

        var proyNombre = paso.ProyectoId.HasValue
            ? (await ctx.Project.Where(pr => pr.ProjectId == paso.ProyectoId).Select(pr => pr.ProjectDescription).FirstOrDefaultAsync())
            : null;

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var acts = paso.Actividades
            .OrderBy(a => a.Categoria?.Ambito)
            .ThenBy(a => a.Orden ?? 999)
            .ToList();
        var ejs = acts.SelectMany(a => a.Ejecuciones).ToList();
        var totalPlan = ejs.Count;
        var totalEj = ejs.Count(e => e.Estado == "Ejecutado");
        var totalVenc = ejs.Count(e => e.Estado == "Vencido");
        var planHoy = ejs.Count(e => e.FechaProgramada <= hoy);
        var ejHoy = ejs.Count(e => e.Estado == "Ejecutado" && e.FechaProgramada <= hoy);
        var spi = CalcularSpi(planHoy, ejHoy);
        var pct = totalPlan == 0 ? 0m : Math.Round((decimal)totalEj / totalPlan * 100, 1);

        var dto = new PasoDetalleDto
        {
            Id = paso.Id,
            ProyectoId = paso.ProyectoId,
            ProyectoNombre = proyNombre,
            PlantillaId = paso.PlantillaId,
            Nombre = paso.Nombre,
            Anio = paso.Anio,
            MesInicio = paso.MesInicio,
            EsPlantilla = paso.EsPlantilla,
            Estado = paso.Estado,
            AprobadoPorNombre = paso.AprobadoPor.HasValue ? personas.GetValueOrDefault(paso.AprobadoPor.Value) : null,
            AprobadoEn = paso.AprobadoEn,
            CreatedAt = paso.CreatedAt,
            TotalActividades = acts.Count,
            TotalPlanificadas = totalPlan,
            TotalEjecutadas = totalEj,
            TotalVencidas = totalVenc,
            Spi = spi,
            PorcentajeAvance = pct,
            Actividades = acts.Select(a => MapActividad(a, personas)).ToList()
        };

        return dto;
    }

    // ── CRUD PASO ─────────────────────────────────────────────────────────────

    public async Task<PasoListItemDto> CrearAsync(CrearPasoRequest req, int userId)
    {
        using var ctx = _factory.CreateDbContext();
        var paso = new SsomaPaso
        {
            ProyectoId = req.ProyectoId,
            Nombre = req.Nombre,
            Anio = req.Anio,
            MesInicio = req.MesInicio,
            EsPlantilla = req.EsPlantilla,
            Estado = "Borrador",
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };
        ctx.SsomaPasos.Add(paso);
        await ctx.SaveChangesAsync();

        var proyectos = req.ProyectoId.HasValue
            ? await ctx.Project.Where(pr => pr.ProjectId == req.ProyectoId).ToDictionaryAsync(pr => pr.ProjectId, pr => pr.ProjectDescription ?? "")
            : new Dictionary<int, string>();

        return BuildListItem(paso, proyectos, new Dictionary<int, string>());
    }

    public async Task<PasoListItemDto> EditarAsync(int id, EditarPasoRequest req)
    {
        using var ctx = _factory.CreateDbContext();
        var paso = await ctx.SsomaPasos
            .Include(p => p.Actividades.Where(a => a.Activo && a.DeletedAt == null))
                .ThenInclude(a => a.Ejecuciones)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException("PASO no encontrado.");

        if (paso.Estado != "Borrador")
            throw new InvalidOperationException("Solo se puede editar un PASO en estado Borrador.");

        paso.Nombre = req.Nombre;
        paso.Anio = req.Anio;
        paso.MesInicio = req.MesInicio;
        paso.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();

        var proyectos = paso.ProyectoId.HasValue
            ? await ctx.Project.Where(pr => pr.ProjectId == paso.ProyectoId).ToDictionaryAsync(pr => pr.ProjectId, pr => pr.ProjectDescription ?? "")
            : new Dictionary<int, string>();

        return BuildListItem(paso, proyectos, new Dictionary<int, string>());
    }

    public async Task<PasoListItemDto> AprobarAsync(int id, int userId)
    {
        using var ctx = _factory.CreateDbContext();
        var paso = await ctx.SsomaPasos
            .Include(p => p.Actividades.Where(a => a.Activo && a.DeletedAt == null))
                .ThenInclude(a => a.Ejecuciones)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException("PASO no encontrado.");

        paso.Estado = paso.EsPlantilla ? "Aprobado" : "Activo";
        paso.AprobadoPor = userId;
        paso.AprobadoEn = DateTime.UtcNow;
        paso.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();

        // Generar de inmediato las ejecuciones (entregables) del ciclo hasta el mes actual,
        // en vez de esperar al cron del día 1 — evita que un PASO recién aprobado quede en 0.
        if (!paso.EsPlantilla)
            await GenerarEjecucionesPasoAsync(ctx, paso);

        var proyectos = paso.ProyectoId.HasValue
            ? await ctx.Project.Where(pr => pr.ProjectId == paso.ProyectoId).ToDictionaryAsync(pr => pr.ProjectId, pr => pr.ProjectDescription ?? "")
            : new Dictionary<int, string>();

        var personas = userId > 0
            ? await ctx.Person.Where(p => p.UserId == userId).ToDictionaryAsync(p => p.UserId!.Value, p => p.FullName ?? "")
            : new Dictionary<int, string>();

        return BuildListItem(paso, proyectos, personas);
    }

    public async Task<PasoListItemDto> InstanciarAsync(int plantillaId, InstanciarPasoRequest req, int userId)
    {
        using var ctx = _factory.CreateDbContext();
        var plantilla = await ctx.SsomaPasos
            .Include(p => p.Actividades.Where(a => a.Activo && a.DeletedAt == null))
            .FirstOrDefaultAsync(p => p.Id == plantillaId)
            ?? throw new KeyNotFoundException("Plantilla no encontrada.");

        if (plantilla.Estado != "Aprobado")
            throw new InvalidOperationException("Solo se pueden instanciar plantillas en estado Aprobado.");

        // FIX-B8: prevent duplicate PASO for same project + year
        var yaExiste = await ctx.SsomaPasos.AnyAsync(p =>
            p.ProyectoId == req.ProyectoId &&
            p.Anio == req.Anio &&
            !p.EsPlantilla);
        if (yaExiste)
            throw new InvalidOperationException(
                $"Ya existe un PASO {req.Anio} para este proyecto. Solo puede haber uno por proyecto por año.");

        var instancia = new SsomaPaso
        {
            ProyectoId = req.ProyectoId,
            PlantillaId = plantillaId,
            Nombre = req.Nombre,
            Anio = req.Anio,
            MesInicio = req.MesInicio,
            EsPlantilla = false,
            Estado = "Borrador",
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var act in plantilla.Actividades)
        {
            instancia.Actividades.Add(new SsomaPasoActividad
            {
                CategoriaId = act.CategoriaId,
                Nombre = act.Nombre,
                Descripcion = act.Descripcion,
                Alcance = act.Alcance,
                Frecuencia = act.Frecuencia,
                ResponsableId = act.ResponsableId,
                ResponsableTexto = act.ResponsableTexto,
                MesInicio = act.MesInicio,
                MesFin    = act.MesFin,
                CantidadPlanificada = act.CantidadPlanificada,
                Horas = act.Horas,
                Recursos = act.Recursos,
                Indicador = act.Indicador,
                Meta = act.Meta,
                Orden = act.Orden,
                Activo = true
            });
        }

        _logger.LogInformation(
            "InstanciarAsync: plantilla.MesInicio={PlantillaMes}, req.MesInicio={ReqMes}",
            plantilla.MesInicio, req.MesInicio);

        if (plantilla.Actividades.Any())
            _logger.LogInformation(
                "Primera actividad: orig.MesInicio={Orig}, ajustado={Ajustado}",
                plantilla.Actividades.First().MesInicio,
                AjustarMes(plantilla.Actividades.First().MesInicio, plantilla.MesInicio, req.MesInicio));

        ctx.SsomaPasos.Add(instancia);
        await ctx.SaveChangesAsync();

        var proyectos = await ctx.Project.Where(pr => pr.ProjectId == req.ProyectoId).ToDictionaryAsync(pr => pr.ProjectId, pr => pr.ProjectDescription ?? "");
        return BuildListItem(instancia, proyectos, new Dictionary<int, string>());
    }

    // ── Histórico por proyecto ────────────────────────────────────────────────

    public async Task<List<PasoHistoricoAnioDto>> GetHistoricoProyectoAsync(int proyectoId)
    {
        using var ctx = _factory.CreateDbContext();
        var pasos = await ctx.SsomaPasos
            .Where(p => p.ProyectoId == proyectoId && !p.EsPlantilla && p.Anio != null)
            .Include(p => p.Actividades.Where(a => a.Activo && a.DeletedAt == null))
                .ThenInclude(a => a.Ejecuciones)
            .OrderByDescending(p => p.Anio)
            .ToListAsync();

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var result = new List<PasoHistoricoAnioDto>();

        foreach (var paso in pasos)
        {
            // No filtramos por ventana de ciclo (cicloStart/cicloEnd): la relación real
            // ejecucion -> actividad -> paso_id ya determina a qué PASO pertenece cada fila.
            // Filtrar además por fecha ocultaba ejecuciones reales cuando el mes_inicio/anio
            // del PASO se corrigió después de que ya existían ejecuciones generadas.
            var allEj = paso.Actividades
                .SelectMany(a => a.Ejecuciones)
                .ToList();

            var totalProg = allEj.Count;
            var totalEj   = allEj.Count(e => e.Estado == "Ejecutado");
            var totalVenc = allEj.Count(e =>
                e.Estado != "Ejecutado" &&
                new DateOnly(e.FechaProgramada.Year, e.FechaProgramada.Month,
                    DateTime.DaysInMonth(e.FechaProgramada.Year, e.FechaProgramada.Month)) < hoy
            );
            var spi    = totalProg == 0 ? 1m : Math.Round((decimal)totalEj / totalProg, 2);
            var avance = totalProg == 0 ? 0m : Math.Round((decimal)totalEj / totalProg * 100, 1);
            var color  = spi >= 0.95m ? "verde" : spi >= 0.80m ? "amarillo" : "rojo";

            result.Add(new PasoHistoricoAnioDto
            {
                Anio             = paso.Anio!.Value,
                TotalProgramadas = totalProg,
                TotalEjecutadas  = totalEj,
                TotalVencidas    = totalVenc,
                SpiGeneral       = spi,
                SpiColor         = color,
                PorcentajeAvance = avance,
            });
        }
        return result;
    }

    // ── SPI ───────────────────────────────────────────────────────────────────

    public async Task<PasoSpiDto> GetSpiAsync(int id)
    {
        using var ctx = _factory.CreateDbContext();
        var paso = await ctx.SsomaPasos
            .Include(p => p.Actividades.Where(a => a.Activo && a.DeletedAt == null))
                .ThenInclude(a => a.Categoria)
            .Include(p => p.Actividades.Where(a => a.Activo && a.DeletedAt == null))
                .ThenInclude(a => a.Ejecuciones)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException("PASO no encontrado.");

        if (!paso.Anio.HasValue)
            return new PasoSpiDto { SpiGeneral = 1m, SpiColor = "verde",
                PorcentajeAvance = 0, TotalProgramadas = 0, TotalEjecutadas = 0,
                TotalVencidas = 0, PlanificadasAHoy = 0, EjecutadasAHoy = 0,
                ProximasAVencer = 0,
                Seguridad = new SpiPorAmbitoDto { Spi = 1m, Color = "verde" },
                Salud     = new SpiPorAmbitoDto { Spi = 1m, Color = "verde" },
                Ambiente  = new SpiPorAmbitoDto { Spi = 1m, Color = "verde" },
                Ssoma     = new SpiPorAmbitoDto { Spi = 1m, Color = "verde" }
            };

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        // No filtramos por ventana de ciclo: ver nota en GetHistoricoProyectoAsync.
        var allEj = paso.Actividades.SelectMany(a => a.Ejecuciones).ToList();
        var planHoy = allEj.Count(e => e.FechaProgramada <= hoy);
        var ejHoy = allEj.Count(e => e.Estado == "Ejecutado" && e.FechaProgramada <= hoy);
        var totalProg = allEj.Count;
        var totalEj = allEj.Count(e => e.Estado == "Ejecutado");
        var totalVenc = allEj.Count(e =>
            e.Estado != "Ejecutado" &&
            new DateOnly(e.FechaProgramada.Year, e.FechaProgramada.Month,
                DateTime.DaysInMonth(e.FechaProgramada.Year, e.FechaProgramada.Month)) < hoy
        );
        var proxVencer = allEj.Count(e => e.Estado == "Programado" && e.FechaProgramada > hoy && e.FechaProgramada <= hoy.AddDays(7));
        var spiG = CalcularSpi(planHoy, ejHoy);

        SpiPorAmbitoDto CalcAmbito(string ambito)
        {
            var ejs = paso.Actividades
                .Where(a => string.Equals(a.Categoria?.Ambito, ambito, StringComparison.OrdinalIgnoreCase))
                .SelectMany(a => a.Ejecuciones)
                .ToList();
            var ph = ejs.Count(e => e.FechaProgramada <= hoy);
            var eh = ejs.Count(e => e.Estado == "Ejecutado" && e.FechaProgramada <= hoy);
            var spi = CalcularSpi(ph, eh);
            return new SpiPorAmbitoDto { Spi = spi, Color = SpiColor(spi), Planificadas = ph, Ejecutadas = eh, Vencidas = ejs.Count(e => e.Estado == "Vencido") };
        }

        return new PasoSpiDto
        {
            SpiGeneral = spiG,
            SpiColor = SpiColor(spiG),
            PorcentajeAvance = totalProg == 0 ? 0m : Math.Round((decimal)totalEj / totalProg * 100, 1),
            PlanificadasAHoy = planHoy,
            EjecutadasAHoy = ejHoy,
            TotalProgramadas = totalProg,
            TotalEjecutadas = totalEj,
            TotalVencidas = totalVenc,
            ProximasAVencer = proxVencer,
            Seguridad = CalcAmbito("Seguridad"),
            Salud = CalcAmbito("Salud"),
            Ambiente = CalcAmbito("Ambiente"),
            Ssoma = CalcAmbito("SSOMA")
        };
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────

    public async Task<PasoDashboardDto> GetDashboardAsync(int? anio)
    {
        using var ctx = _factory.CreateDbContext();
        var anioFiltro = anio ?? DateTime.Today.Year;

        var proyectosOperativos = await ctx.Project
            .Where(p => p.Operativo && p.Active)
            .Select(p => p.ProjectId)
            .ToListAsync();

        var pasos = await ctx.SsomaPasos
            .Where(p => !p.EsPlantilla && p.Anio == anioFiltro && p.ProyectoId != null && proyectosOperativos.Contains(p.ProyectoId!.Value))
            .Include(p => p.Actividades.Where(a => a.Activo && a.DeletedAt == null))
                .ThenInclude(a => a.Ejecuciones)
            .Include(p => p.Actividades.Where(a => a.Activo && a.DeletedAt == null))
                .ThenInclude(a => a.Categoria)
            .ToListAsync();

        var proyIds = pasos.Where(p => p.ProyectoId.HasValue).Select(p => p.ProyectoId!.Value).Distinct().ToList();
        var proyectos = await ctx.Project
            .Where(pr => proyIds.Contains(pr.ProjectId))
            .ToDictionaryAsync(pr => pr.ProjectId, pr => pr.ProjectDescription ?? "");

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var allEjs = pasos.SelectMany(p => p.Actividades.SelectMany(a => a.Ejecuciones)).ToList();
        var planHoyTotal = allEjs.Count(e => e.FechaProgramada <= hoy);
        var ejHoyTotal = allEjs.Count(e => e.Estado == "Ejecutado" && e.FechaProgramada <= hoy);
        var totalEjTotal = allEjs.Count(e => e.Estado == "Ejecutado");
        var totalPlanTotal = allEjs.Count;
        var totalVencTotal = allEjs.Count(e => e.Estado == "Vencido");
        var proxTotal = allEjs.Count(e => e.Estado == "Programado" && e.FechaProgramada > hoy && e.FechaProgramada <= hoy.AddDays(7));
        var spiCons = CalcularSpi(planHoyTotal, ejHoyTotal);
        var pctCons = totalPlanTotal == 0 ? 0m : Math.Round((decimal)totalEjTotal / totalPlanTotal * 100, 1);

        var porProyecto = pasos
            .Where(p => p.ProyectoId.HasValue)
            .Select(p =>
            {
                var ejs = p.Actividades.SelectMany(a => a.Ejecuciones).ToList();
                var ph = ejs.Count(e => e.FechaProgramada <= hoy);
                var eh = ejs.Count(e => e.Estado == "Ejecutado" && e.FechaProgramada <= hoy);
                var tp = ejs.Count;
                var te = ejs.Count(e => e.Estado == "Ejecutado");
                var spi = CalcularSpi(ph, eh);
                return new PasoPorProyectoDto
                {
                    ProyectoId = p.ProyectoId!.Value,
                    ProyectoNombre = proyectos.GetValueOrDefault(p.ProyectoId.Value, ""),
                    PasoId = p.Id,
                    Spi = spi,
                    SpiColor = SpiColor(spi),
                    PorcentajeAvance = tp == 0 ? 0m : Math.Round((decimal)te / tp * 100, 1),
                    Vencidas = ejs.Count(e => e.Estado == "Vencido")
                };
            }).ToList();

        SpiPorAmbitoDto CalcAmbitoConsolidado(string ambito)
        {
            var ejs = pasos
                .SelectMany(p => p.Actividades)
                .Where(a => string.Equals(a.Categoria?.Ambito, ambito, StringComparison.OrdinalIgnoreCase))
                .SelectMany(a => a.Ejecuciones).ToList();
            var ph = ejs.Count(e => e.FechaProgramada <= hoy);
            var eh = ejs.Count(e => e.Estado == "Ejecutado" && e.FechaProgramada <= hoy);
            var spi = CalcularSpi(ph, eh);
            return new SpiPorAmbitoDto { Spi = spi, Color = SpiColor(spi), Planificadas = ph, Ejecutadas = eh, Vencidas = ejs.Count(e => e.Estado == "Vencido") };
        }

        return new PasoDashboardDto
        {
            AnioActual = anioFiltro,
            TotalProgramas = pasos.Count,
            ProgramasActivos = pasos.Count(p => p.Estado == "Activo"),
            SpiConsolidado = spiCons,
            SpiColor = SpiColor(spiCons),
            PorcentajeAvanceConsolidado = pctCons,
            TotalVencidas = totalVencTotal,
            TotalProximasVencer = proxTotal,
            PorProyecto = porProyecto,
            Seguridad = CalcAmbitoConsolidado("Seguridad"),
            Salud     = CalcAmbitoConsolidado("Salud"),
            Ambiente  = CalcAmbitoConsolidado("Ambiente"),
        };
    }

    // ── Alertas ───────────────────────────────────────────────────────────────

    public async Task<List<PasoAlertaDto>> GetAlertasAsync()
    {
        using var ctx = _factory.CreateDbContext();
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var limite = hoy.AddDays(7);

        var ejecuciones = await ctx.SsomaPasoEjecuciones
            .Include(e => e.Actividad)
                .ThenInclude(a => a.Paso)
            .Where(e => e.Estado == "Vencido"
                || (e.Estado == "Programado" && e.FechaProgramada >= hoy && e.FechaProgramada <= limite))
            .ToListAsync();

        var respIds = ejecuciones
            .Where(e => e.Actividad.ResponsableId.HasValue)
            .Select(e => e.Actividad.ResponsableId!.Value)
            .Distinct().ToList();
        var personas = await ctx.Person
            .Where(p => p.UserId.HasValue && respIds.Contains(p.UserId.Value))
            .ToDictionaryAsync(p => p.UserId!.Value, p => p.FullName ?? "");

        var proyIds = ejecuciones
            .Where(e => e.Actividad.Paso.ProyectoId.HasValue)
            .Select(e => e.Actividad.Paso.ProyectoId!.Value)
            .Distinct().ToList();
        var proyectos = await ctx.Project
            .Where(pr => proyIds.Contains(pr.ProjectId))
            .ToDictionaryAsync(pr => pr.ProjectId, pr => pr.ProjectDescription ?? "");

        return ejecuciones.Select(e => new PasoAlertaDto
        {
            EjecucionId = e.Id,
            ActividadId = e.ActividadId,
            ActividadNombre = e.Actividad.Nombre,
            PasoId = e.Actividad.PasoId,
            PasoNombre = e.Actividad.Paso.Nombre,
            ProyectoNombre = e.Actividad.Paso.ProyectoId.HasValue ? proyectos.GetValueOrDefault(e.Actividad.Paso.ProyectoId.Value) : null,
            FechaProgramada = e.FechaProgramada,
            TipoAlerta = e.Estado == "Vencido" ? "Vencido" : "ProximaAVencer",
            ResponsableNombre = e.Actividad.ResponsableId.HasValue ? personas.GetValueOrDefault(e.Actividad.ResponsableId.Value) : null
        }).ToList();
    }

    // ── Actividades ───────────────────────────────────────────────────────────

    public async Task<PasoActividadDto> CrearActividadAsync(CrearActividadRequest req)
    {
        using var ctx = _factory.CreateDbContext();
        var act = new SsomaPasoActividad
        {
            PasoId = req.PasoId,
            CategoriaId = req.CategoriaId,
            Nombre = req.Nombre,
            Descripcion = req.Descripcion,
            Alcance = req.Alcance,
            Frecuencia = req.Frecuencia,
            ResponsableId = req.ResponsableId,
            ResponsableTexto = req.ResponsableTexto,
            MesInicio = req.MesInicio,
            MesFin = req.MesFin,
            CantidadPlanificada = req.CantidadPlanificada,
            Horas = req.Horas,
            Recursos = req.Recursos,
            Indicador = req.Indicador,
            Meta = req.Meta,
            Orden = req.Orden,
            Activo = true
        };
        ctx.SsomaPasoActividades.Add(act);
        await ctx.SaveChangesAsync();

        act.Categoria = await ctx.SsomaPasoCategorias.FindAsync(req.CategoriaId) ?? new SsomaPasoCategoria();

        var personas = new Dictionary<int, string>();
        if (req.ResponsableId.HasValue)
        {
            var p = await ctx.Person.FirstOrDefaultAsync(p => p.UserId == req.ResponsableId);
            if (p?.FullName is not null) personas[req.ResponsableId.Value] = p.FullName;
        }

        return MapActividad(act, personas);
    }

    public async Task<PasoActividadDto> EditarActividadAsync(int id, EditarActividadRequest req)
    {
        using var ctx = _factory.CreateDbContext();
        var act = await ctx.SsomaPasoActividades
            .Include(a => a.Categoria)
            .Include(a => a.Ejecuciones)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new KeyNotFoundException("Actividad no encontrada.");

        act.CategoriaId = req.CategoriaId;
        act.Nombre = req.Nombre;
        act.Descripcion = req.Descripcion;
        act.Alcance = req.Alcance;
        act.Frecuencia = req.Frecuencia;
        act.ResponsableId = req.ResponsableId;
        act.ResponsableTexto = req.ResponsableTexto;
        act.MesInicio = req.MesInicio;
        act.MesFin = req.MesFin;
        act.CantidadPlanificada = req.CantidadPlanificada;
        act.Horas = req.Horas;
        act.Recursos = req.Recursos;
        act.Indicador = req.Indicador;
        act.Meta = req.Meta;
        act.Orden = req.Orden;
        await ctx.SaveChangesAsync();

        var cat = await ctx.SsomaPasoCategorias.FindAsync(req.CategoriaId);
if (cat is not null) act.Categoria = cat;

        var personas = new Dictionary<int, string>();
        if (req.ResponsableId.HasValue)
        {
            var p = await ctx.Person.FirstOrDefaultAsync(p => p.UserId == req.ResponsableId);
            if (p?.FullName is not null) personas[req.ResponsableId.Value] = p.FullName;
        }

        return MapActividad(act, personas);
    }

    public async Task DeleteActividadAsync(int id, string motivo, int userId)
    {
        using var ctx = _factory.CreateDbContext();
        var act = await ctx.SsomaPasoActividades
            .Include(a => a.Ejecuciones)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new KeyNotFoundException("Actividad no encontrada.");

        var ejecutadas = act.Ejecuciones.Count(e => e.Estado == "Ejecutado");
        if (ejecutadas > 0)
            throw new AbrilException(
                $"No se puede eliminar: la actividad tiene {ejecutadas} ejecución(es) ya registradas como Ejecutado. " +
                "Si necesitas reemplazarla, usa Editar en lugar de Eliminar para conservar el historial.",
                400);

        var valorAnterior = JsonSerializer.Serialize(new
        {
            act.Id, act.PasoId, act.CategoriaId, act.Nombre, act.Descripcion,
            act.Alcance, act.Frecuencia, act.ResponsableId, act.ResponsableTexto,
            act.MesInicio, act.MesFin, act.CantidadPlanificada, act.Horas,
            act.Recursos, act.Indicador, act.Meta, act.Orden, act.Activo
        });

        act.Activo = false;
        act.DeletedAt = DateTime.UtcNow;
        act.DeletedBy = userId;
        act.MotivoEliminacion = motivo;

        ctx.SsomaPasoAuditorias.Add(new SsomaPasoAuditoria
        {
            Tipo = "ELIMINACION",
            Entidad = "ACTIVIDAD",
            EntidadId = id,
            PasoId = act.PasoId,
            Descripcion = act.Nombre,
            Motivo = motivo,
            ValorAnterior = valorAnterior,
            UsuarioId = userId,
            CreatedAt = DateTime.UtcNow
        });

        await ctx.SaveChangesAsync();
    }

    // ── Ejecuciones ───────────────────────────────────────────────────────────

    public async Task<PasoEjecucionDto> RegistrarEjecucionAsync(RegistrarEjecucionRequest req, int userId)
    {
        using var ctx = _factory.CreateDbContext();
        var ejecucion = await ctx.SsomaPasoEjecuciones
            .FirstOrDefaultAsync(e => e.ActividadId == req.ActividadId && e.FechaProgramada == req.FechaProgramada);

        var fechaVerif = req.FechaVerificacion ?? UltimoDiaMes(req.FechaEjecutada.Year, req.FechaEjecutada.Month);

        if (ejecucion is not null)
        {
            ejecucion.Estado = "Ejecutado";
            ejecucion.FechaEjecutada = req.FechaEjecutada;
            ejecucion.FechaVerificacion = fechaVerif;
            ejecucion.Observaciones = req.Observaciones;
            ejecucion.ParticipantesCount = req.ParticipantesCount;
            ejecucion.RegistradoPor = userId;
            ejecucion.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            ejecucion = new SsomaPasoEjecucion
            {
                ActividadId = req.ActividadId,
                FechaProgramada = req.FechaProgramada,
                FechaEjecutada = req.FechaEjecutada,
                FechaVerificacion = fechaVerif,
                Observaciones = req.Observaciones,
                ParticipantesCount = req.ParticipantesCount,
                Estado = "Ejecutado",
                RegistradoPor = userId,
                CreatedAt = DateTime.UtcNow
            };
            ctx.SsomaPasoEjecuciones.Add(ejecucion);
        }

        await ctx.SaveChangesAsync();

        var personas = new Dictionary<int, string>();
        var p = await ctx.Person.FirstOrDefaultAsync(p => p.UserId == userId);
        if (p?.FullName is not null) personas[userId] = p.FullName;

        return MapEjecucion(ejecucion, personas);
    }

    public async Task<PasoEjecucionDto> ProgramarEjecucionAsync(ProgramarEjecucionRequest req)
    {
        using var ctx = _factory.CreateDbContext();
        var yaExiste = await ctx.SsomaPasoEjecuciones
            .AnyAsync(e => e.ActividadId == req.ActividadId && e.FechaProgramada == req.FechaProgramada);
        if (yaExiste) throw new InvalidOperationException("Ya existe una ejecución en esa fecha.");
        var ejecucion = new SsomaPasoEjecucion
        {
            ActividadId       = req.ActividadId,
            FechaProgramada   = req.FechaProgramada,
            FechaVerificacion = UltimoDiaMes(req.FechaProgramada.Year, req.FechaProgramada.Month),
            Estado            = "Programado",
            CreatedAt         = DateTime.UtcNow
        };
        ctx.SsomaPasoEjecuciones.Add(ejecucion);
        await ctx.SaveChangesAsync();
        return MapEjecucion(ejecucion, new Dictionary<int, string>());
    }

    public async Task<PasoEjecucionDto> ReprogramarEjecucionAsync(int id, ReprogramarEjecucionRequest req, int userId)
    {
        using var ctx = _factory.CreateDbContext();
        var ejecucion = await ctx.SsomaPasoEjecuciones
            .Include(e => e.Actividad)
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new KeyNotFoundException("Ejecución no encontrada.");

        if (ejecucion.Estado == "Ejecutado")
            throw new InvalidOperationException("No se puede reprogramar una ejecución ya ejecutada.");

        var fechaAnterior = ejecucion.FechaProgramada;
        var estadoAnterior = ejecucion.Estado;

        // Marcar ejecución original como Reprogramado
        ejecucion.FechaReprogramada = req.NuevaFecha;
        ejecucion.MotivoReprogramacion = req.Motivo;
        ejecucion.Estado = "Reprogramado";
        ejecucion.RegistradoPor = userId;
        ejecucion.UpdatedAt = DateTime.UtcNow;

        // Crear nueva ejecución en la nueva fecha
        var nuevaEjecucion = new SsomaPasoEjecucion
        {
            ActividadId       = ejecucion.ActividadId,
            FechaProgramada   = req.NuevaFecha,
            FechaVerificacion = UltimoDiaMes(req.NuevaFecha.Year, req.NuevaFecha.Month),
            Estado            = "Programado",
            CreatedAt         = DateTime.UtcNow
        };
        ctx.SsomaPasoEjecuciones.Add(nuevaEjecucion);

        await ctx.SaveChangesAsync();

        ctx.SsomaPasoAuditorias.Add(new SsomaPasoAuditoria
        {
            Tipo = "REPROGRAMACION",
            Entidad = "EJECUCION",
            EntidadId = ejecucion.Id,
            PasoId = ejecucion.Actividad.PasoId,
            Descripcion = ejecucion.Actividad.Nombre,
            Motivo = req.Motivo,
            ValorAnterior = JsonSerializer.Serialize(new { FechaProgramada = fechaAnterior, Estado = estadoAnterior }),
            ValorNuevo = JsonSerializer.Serialize(new { ejecucion.FechaReprogramada, ejecucion.Estado, NuevaEjecucionFecha = req.NuevaFecha }),
            UsuarioId = userId,
            CreatedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();

        return MapEjecucion(ejecucion, new Dictionary<int, string>());
    }

    public async Task<PasoEjecucionDto> SubirEvidenciaAsync(int ejecucionId, IFormFile file, int userId)
    {
        using var ctx = _factory.CreateDbContext();
        var ejecucion = await ctx.SsomaPasoEjecuciones
            .Include(e => e.Actividad).ThenInclude(a => a.Paso)
            .FirstOrDefaultAsync(e => e.Id == ejecucionId)
            ?? throw new KeyNotFoundException("Ejecución no encontrada.");

        var actividad = ejecucion.Actividad;
        var paso = actividad.Paso;
        // FIX-B5: null-safe anio for plantilla corporativa
        int pasoAnio = paso.Anio ?? DateTime.Today.Year;
        var carpetaPath = $"PASO/{pasoAnio}/{paso.ProyectoId}/{actividad.Id}";

        string evidenciaUrl;
        try
        {
            // "paso-evidencias" → SharePoint:Sites:SSOMAApps:PasoEvidenciasLibraryId
            // (resuelto en SharePointHabService.ResolverLibraryId via c.Contains("paso-evidencias"))
            using var stream = file.OpenReadStream();
            evidenciaUrl = await _sharePoint.SubirArchivoEnRutaAsync(
                stream, file.FileName, "paso-evidencias", carpetaPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"No se pudo subir el archivo a SharePoint: {ex.Message}", ex);
        }

        ejecucion.EvidenciaNombre = file.FileName;
        ejecucion.EvidenciaUrl = evidenciaUrl;
        ejecucion.EvidenciaSpId = null;
        ejecucion.RegistradoPor = userId;
        ejecucion.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();

        return MapEjecucion(ejecucion, new Dictionary<int, string>());
    }

    // ── Archivos múltiples por ejecución ─────────────────────────────────────

    public async Task<PasoEjecucionArchivoDto> AgregarArchivoEjecucionAsync(int ejecucionId, IFormFile file, int userId)
    {
        using var ctx = _factory.CreateDbContext();
        var ejecucion = await ctx.SsomaPasoEjecuciones
            .Include(e => e.Actividad).ThenInclude(a => a.Paso)
            .Include(e => e.Archivos)
            .FirstOrDefaultAsync(e => e.Id == ejecucionId)
            ?? throw new KeyNotFoundException("Ejecución no encontrada.");

        var actividad = ejecucion.Actividad;
        var paso = actividad.Paso;
        int pasoAnio = paso.Anio ?? DateTime.Today.Year;
        var carpetaPath = $"PASO/{pasoAnio}/{paso.ProyectoId}/{actividad.Id}";

        string archivoUrl;
        try
        {
            using var stream = file.OpenReadStream();
            archivoUrl = await _sharePoint.SubirArchivoEnRutaAsync(
                stream, file.FileName, "paso-evidencias", carpetaPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"No se pudo subir el archivo a SharePoint: {ex.Message}", ex);
        }

        var orden = ejecucion.Archivos.Count > 0 ? ejecucion.Archivos.Max(a => a.Orden) + 1 : 1;
        var archivo = new SsomaPasoEjecucionArchivo
        {
            EjecucionId = ejecucionId,
            ArchivoUrl = archivoUrl,
            ArchivoNombre = file.FileName,
            ArchivoSpId = null,
            Orden = orden,
            CreatedAt = DateTime.UtcNow
        };
        ctx.SsomaPasoEjecucionArchivos.Add(archivo);
        await ctx.SaveChangesAsync();

        return new PasoEjecucionArchivoDto
        {
            Id = archivo.Id,
            EjecucionId = archivo.EjecucionId,
            ArchivoUrl = archivo.ArchivoUrl,
            ArchivoNombre = archivo.ArchivoNombre,
            ArchivoSpId = archivo.ArchivoSpId,
            Orden = archivo.Orden,
            CreatedAt = archivo.CreatedAt
        };
    }

    public async Task EliminarArchivoEjecucionAsync(int archivoId)
    {
        using var ctx = _factory.CreateDbContext();
        var archivo = await ctx.SsomaPasoEjecucionArchivos.FindAsync(archivoId)
            ?? throw new KeyNotFoundException("Archivo no encontrado.");
        ctx.SsomaPasoEjecucionArchivos.Remove(archivo);
        await ctx.SaveChangesAsync();
    }

    // ── Auditoría ─────────────────────────────────────────────────────────────

    public async Task<List<PasoAuditoriaDto>> GetAuditoriaAsync(int actividadId)
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsomaPasoAuditorias
            .Where(a => a.EntidadId == actividadId && a.Entidad == "ACTIVIDAD")
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new PasoAuditoriaDto
            {
                Id = a.Id,
                Tipo = a.Tipo,
                Descripcion = a.Descripcion,
                Motivo = a.Motivo,
                ValorAnterior = a.ValorAnterior,
                ValorNuevo = a.ValorNuevo,
                UsuarioId = a.UsuarioId,
                CreatedAt = a.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            })
            .ToListAsync();
    }

    // ── Actividades de Salud (todos los proyectos) ──────────────────────────────

    // Igual que GetResumenMesAsync, pero expandido a todos los proyectos y a todos los meses
    // (desde el inicio de cada PASO hasta el mes actual), no solo a un PASO/mes puntual.
    // Reutiliza EsMesPlanificado para "inventar" el mes teórico de cada actividad aunque
    // el cron todavía no haya generado su fila en ssoma_paso_ejecucion (ver ProcesarCronAsync).
    public async Task<PagedResult<PasoSaludActividadListItemDto>> GetActividadesSaludAsync(PasoSaludListQuery q)
    {
        using var ctx = _factory.CreateDbContext();
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        int mesActualAbs = hoy.Year * 12 + hoy.Month;

        var pasosQuery = ctx.SsomaPasos.Where(p => !p.EsPlantilla && p.Anio != null);
        if (q.ProyectoId.HasValue) pasosQuery = pasosQuery.Where(p => p.ProyectoId == q.ProyectoId);

        var pasos = await pasosQuery
            .Include(p => p.Actividades.Where(a => a.Activo && a.DeletedAt == null))
                .ThenInclude(a => a.Categoria)
            .Include(p => p.Actividades.Where(a => a.Activo && a.DeletedAt == null))
                .ThenInclude(a => a.Ejecuciones)
            .ToListAsync();

        var proyIds = pasos.Where(p => p.ProyectoId.HasValue).Select(p => p.ProyectoId!.Value).Distinct().ToList();
        var proyectos = await ctx.Project
            .Where(pr => proyIds.Contains(pr.ProjectId))
            .ToDictionaryAsync(pr => pr.ProjectId, pr => pr.ProjectDescription ?? "");

        var responsableIds = pasos.SelectMany(p => p.Actividades)
            .Where(a => a.ResponsableId.HasValue).Select(a => a.ResponsableId!.Value).Distinct().ToList();
        var personas = await ctx.Person
            .Where(p => p.UserId.HasValue && responsableIds.Contains(p.UserId.Value))
            .ToDictionaryAsync(p => p.UserId!.Value, p => p.FullName ?? "");

        var resultados = new List<PasoSaludActividadListItemDto>();

        foreach (var paso in pasos)
        {
            int pasoAnio = paso.Anio!.Value;
            // "Anio" es siempre el año en que arranca el ciclo (sin importar el mes de inicio).
            int cicloStartYear = pasoAnio;
            int cicloStartAbs = cicloStartYear * 12 + paso.MesInicio - 1;

            foreach (var act in paso.Actividades.Where(a => a.Categoria?.Ambito == "Salud"))
            {
                if (q.CategoriaId.HasValue && act.CategoriaId != q.CategoriaId) continue;

                // Meses del ciclo teórico según la frecuencia de la actividad.
                var mesesAbs = new HashSet<int>();
                for (int mesCiclo = act.MesInicio; mesCiclo <= Math.Min(act.MesFin, 12); mesCiclo++)
                {
                    if (act.Frecuencia == "Unica")
                    {
                        if (mesCiclo == act.MesInicio) mesesAbs.Add(cicloStartAbs + mesCiclo - 1);
                        continue;
                    }
                    if (EsMesPlanificado(act.Frecuencia, mesCiclo, act.MesInicio))
                        mesesAbs.Add(cicloStartAbs + mesCiclo - 1);
                }
                // Uniones con meses de ejecuciones reales (cubre reprogramaciones fuera del ciclo teórico).
                foreach (var ej in act.Ejecuciones)
                    mesesAbs.Add(ej.FechaProgramada.Year * 12 + ej.FechaProgramada.Month);

                foreach (var absMes in mesesAbs)
                {
                    if (absMes > mesActualAbs) continue; // hasta el mes actual, nada futuro

                    int calAnio = (absMes - 1) / 12;
                    int calMes = absMes - calAnio * 12;

                    if (q.Anio.HasValue && calAnio != q.Anio) continue;
                    if (q.Mes.HasValue && calMes != q.Mes) continue;

                    var ej = act.Ejecuciones
                        .Where(e => e.FechaProgramada.Year == calAnio && e.FechaProgramada.Month == calMes)
                        .OrderByDescending(e => e.Id)
                        .FirstOrDefault();

                    var estado = ej?.Estado ?? "SinProgramar";
                    var cumplida = estado == "Ejecutado";
                    if (q.Cumplida.HasValue && q.Cumplida.Value != cumplida) continue;

                    resultados.Add(new PasoSaludActividadListItemDto
                    {
                        EjecucionId = ej?.Id ?? 0,
                        ActividadId = act.Id,
                        PasoId = paso.Id,
                        ProyectoId = paso.ProyectoId,
                        ProyectoNombre = paso.ProyectoId.HasValue ? proyectos.GetValueOrDefault(paso.ProyectoId.Value, "") : "Corporativo",
                        CategoriaId = act.CategoriaId,
                        CategoriaNombre = act.Categoria?.Nombre ?? "",
                        CategoriaIcono = act.Categoria?.Icono,
                        ActividadNombre = act.Nombre,
                        Frecuencia = act.Frecuencia,
                        FechaProgramada = ej?.FechaProgramada ?? new DateOnly(calAnio, calMes, 1),
                        FechaEjecutada = ej?.FechaEjecutada,
                        Estado = estado,
                        Cumplida = cumplida,
                        Observaciones = ej?.Observaciones,
                        ParticipantesCount = ej?.ParticipantesCount,
                        EvidenciaUrl = ej?.EvidenciaUrl,
                        EvidenciaNombre = ej?.EvidenciaNombre,
                        ResponsableNombre = act.ResponsableId.HasValue ? personas.GetValueOrDefault(act.ResponsableId.Value) : null
                    });
                }
            }
        }

        var ordenados = resultados.OrderByDescending(r => r.FechaProgramada).ThenBy(r => r.ActividadNombre).ToList();
        var total = ordenados.Count;
        var items = ordenados.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToList();

        return new PagedResult<PasoSaludActividadListItemDto>
        {
            Items = items,
            Total = total,
            Page = q.Page,
            PageSize = q.PageSize
        };
    }

    // ── Resumen Mes ───────────────────────────────────────────────────────────

    public async Task<PasoResumenMesDto> GetResumenMesAsync(int id, int anio, int mes)
    {
        using var ctx = _factory.CreateDbContext();
        var fechaInicio = new DateOnly(anio, mes, 1);
        var fechaFin = new DateOnly(anio, mes, DateTime.DaysInMonth(anio, mes));
        var paso = await ctx.SsomaPasos
            .Include(p => p.Actividades.Where(a => a.Activo && a.DeletedAt == null))
                .ThenInclude(a => a.Categoria)
            .Include(p => p.Actividades.Where(a => a.Activo && a.DeletedAt == null))
                .ThenInclude(a => a.Ejecuciones.Where(e => e.FechaProgramada >= fechaInicio && e.FechaProgramada <= fechaFin))
            .Include(p => p.Actividades.Where(a => a.Activo && a.DeletedAt == null))
                .ThenInclude(a => a.Ejecuciones.Where(e => e.FechaProgramada >= fechaInicio && e.FechaProgramada <= fechaFin))
                    .ThenInclude(e => e.Archivos)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException("PASO no encontrado.");

        string[] nombresMes = { "", "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
                                 "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };
        string NombreMes() => mes >= 1 && mes <= 12 ? nombresMes[mes] : "";

        // FIX-B2: null-safe anio (plantilla corporativa tiene Anio=NULL en BD)
        int pasoAnio = paso.Anio ?? DateTime.Today.Year;

        // ── Calcular mes del ciclo ────────────────────────────────────────────
        // "Anio" es siempre el año en que arranca el ciclo (sin importar el mes de inicio).
        int cicloStartYear = pasoAnio;
        int cicloStartAbs  = cicloStartYear * 12 + paso.MesInicio - 1;
        int requestedAbs   = anio * 12 + mes - 1;

        // No cortamos con "fuera de ciclo": si el mes tiene ejecuciones reales (ya sea porque
        // el mes_inicio/anio del PASO se corrigió después de generarlas, o porque son datos
        // migrados), deben mostrarse igual. mesCiclo puede salir <1 o >12 aquí; eso solo hace
        // que la proyección teórica por frecuencia no aplique para ese mes, pero no oculta
        // ninguna ejecución real (el filtro por actividad ya la incluye si existe en el mes).
        int mesCiclo = requestedAbs - cicloStartAbs + 1; // 1-based

        var actividadesEnMes = paso.Actividades
            .Where(act =>
            {
                if (!act.Activo) return false;

                // Si tiene ejecución Programado en otro mes, excluir de este mes
                var tieneReprogramadaEnOtroMes = act.Ejecuciones.Any(e =>
                    e.Estado == "Programado" &&
                    !(e.FechaProgramada.Month == mes && e.FechaProgramada.Year == anio));

                // Si tiene ejecución Programado en ESTE mes, incluir siempre
                var tieneEjecucionEsteMes = act.Ejecuciones.Any(e =>
                    e.Estado == "Programado" &&
                    e.FechaProgramada.Month == mes &&
                    e.FechaProgramada.Year == anio);

                if (tieneEjecucionEsteMes) return true;
                if (tieneReprogramadaEnOtroMes) return false;

                if (act.Frecuencia == "Unica")
                    return act.MesInicio == mesCiclo || act.Ejecuciones.Any(e =>
                        e.FechaProgramada.Month == mes &&
                        e.FechaProgramada.Year == anio);

                return act.MesInicio <= mesCiclo && act.MesFin >= mesCiclo
                       && EsMesPlanificado(act.Frecuencia, mesCiclo, act.MesInicio);
            })
            .OrderBy(a => a.Categoria?.Ambito)
            .ThenBy(a => a.Orden ?? 999)
            .ToList();

        var items = actividadesEnMes.Select(act =>
        {
            var ej = act.Ejecuciones
                .FirstOrDefault(e => e.FechaProgramada.Month == mes && e.FechaProgramada.Year == anio);
            return new PasoResumenMesActividadDto
            {
                ActividadId = act.Id,
                Nombre = act.Nombre,
                CategoriaNombre = act.Categoria?.Nombre ?? "",
                CategoriaAmbito = act.Categoria?.Ambito ?? "",
                Frecuencia = act.Frecuencia,
                EjecucionId = ej?.Id,
                FechaProgramada = ej?.FechaProgramada,
                Estado = ej is null ? "SinProgramar" : ej.Estado,
                FechaEjecutada = ej?.FechaEjecutada,
                Observaciones = ej?.Observaciones,
                EvidenciaNombre = ej?.EvidenciaNombre,
                EvidenciaUrl = ej?.EvidenciaUrl,
                Archivos = ej is null ? new() : ej.Archivos.OrderBy(a => a.Orden).Select(a => new PasoEjecucionArchivoDto
                {
                    Id = a.Id,
                    EjecucionId = a.EjecucionId,
                    ArchivoUrl = a.ArchivoUrl,
                    ArchivoNombre = a.ArchivoNombre,
                    ArchivoSpId = a.ArchivoSpId,
                    Orden = a.Orden,
                    CreatedAt = a.CreatedAt
                }).ToList()
            };
        }).ToList();

        // totalProgramadas = TODAS las actividades del mes del ciclo (tengan o no ejecución)
        var totalProgramadas = items.Count;
        var completadas = items.Count(i => i.Estado == "Ejecutado");
        var pendientes  = items.Count(i => i.Estado == "Programado" || i.Estado == "SinProgramar");
        var vencidas    = items.Count(i => i.Estado == "Vencido");
        int porcentajeAvance = totalProgramadas > 0
            ? (int)Math.Round((double)completadas / totalProgramadas * 100)
            : 0;

        PasoResumenMesAmbitoDto CalcAmbito(string ambito)
        {
            var f = items.Where(i => i.CategoriaAmbito.Equals(ambito, StringComparison.OrdinalIgnoreCase)).ToList();
            return new PasoResumenMesAmbitoDto
            {
                Programadas = f.Count,
                Completadas = f.Count(i => i.Estado == "Ejecutado"),
                Pendientes  = f.Count(i => i.Estado == "Programado" || i.Estado == "SinProgramar"),
                Vencidas    = f.Count(i => i.Estado == "Vencido")
            };
        }

        return new PasoResumenMesDto
        {
            Anio = anio,
            Mes = mes,
            NombreMes = NombreMes(),
            TotalProgramadas = totalProgramadas,
            Completadas = completadas,
            Pendientes = pendientes,
            Vencidas = vencidas,
            PorcentajeAvance = porcentajeAvance,
            Seguridad = CalcAmbito("Seguridad"),
            Salud = CalcAmbito("Salud"),
            Ambiente = CalcAmbito("Ambiente"),
            Ssoma = CalcAmbito("SSOMA"),
            Actividades = items
        };
    }

    // ── Cron ─────────────────────────────────────────────────────────────────

    public async Task ProcesarCronAsync()
    {
        using var ctx = _factory.CreateDbContext();
        var hoy = DateOnly.FromDateTime(DateTime.Today);

        var vencidas = await ctx.SsomaPasoEjecuciones
            .Where(e => e.Estado == "Programado" && e.FechaProgramada < hoy)
            .ToListAsync();
        foreach (var e in vencidas)
        {
            e.Estado = "Vencido";
            e.UpdatedAt = DateTime.UtcNow;
        }
        if (vencidas.Count > 0) await ctx.SaveChangesAsync();

        if (hoy.Day == 1)
            await GenerarEjecucionesMesAsync(ctx, hoy.Year, hoy.Month);
    }

    // Genera, para un PASO recién activado, las ejecuciones de todos los meses del ciclo
    // ya transcurridos (desde MesInicio hasta el mes calendario actual), en lugar de
    // depender del cron del día 1 que solo cubre el mes en curso.
    private async Task GenerarEjecucionesPasoAsync(AppDbContext ctx, SsomaPaso paso)
    {
        if (!paso.Anio.HasValue) return;

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        int pasoAnio = paso.Anio.Value;
        // "Anio" es siempre el año en que arranca el ciclo (sin importar el mes de inicio).
        int cicloStartYear = pasoAnio;
        int cicloStartAbs = cicloStartYear * 12 + paso.MesInicio - 1;
        int hoyAbs = hoy.Year * 12 + hoy.Month - 1;
        int limiteAbs = Math.Min(hoyAbs, cicloStartAbs + 11);

        foreach (var act in paso.Actividades)
        {
            for (int abs = cicloStartAbs; abs <= limiteAbs; abs++)
            {
                int mesCiclo = abs - cicloStartAbs + 1;
                if (act.MesInicio > mesCiclo || act.MesFin < mesCiclo) continue;
                if (!EsMesPlanificado(act.Frecuencia, mesCiclo, act.MesInicio)) continue;

                int anioReal = abs / 12;
                int mesReal = abs % 12 + 1;

                if (act.Ejecuciones.Any(e => e.FechaProgramada.Year == anioReal && e.FechaProgramada.Month == mesReal))
                    continue;

                var primer = new DateOnly(anioReal, mesReal, 1);
                var ultimo = UltimoDiaMes(anioReal, mesReal);

                ctx.SsomaPasoEjecuciones.Add(new SsomaPasoEjecucion
                {
                    ActividadId = act.Id,
                    FechaProgramada = primer,
                    FechaVerificacion = ultimo,
                    Estado = primer < hoy ? "Vencido" : "Programado",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await ctx.SaveChangesAsync();
    }

    private async Task GenerarEjecucionesMesAsync(AppDbContext ctx, int anio, int mes)
    {
        // FIX-B3: fetch ALL active PASOs of the year, then calculate mesCiclo
        // individually per PASO (each PASO can have a different MesInicio).
        // Old code used calendario mes directly → wrong results for MesInicio != 1
        var actividades = await ctx.SsomaPasoActividades
            .Include(a => a.Paso)
            .Where(a => a.Activo && a.DeletedAt == null
                && a.Paso.Estado == "Activo"
                && !a.Paso.EsPlantilla
                && a.Paso.Anio == anio)
            .ToListAsync();

        var primer = new DateOnly(anio, mes, 1);
        var ultimo = UltimoDiaMes(anio, mes);

        foreach (var act in actividades)
        {
            var paso = act.Paso;
            int pasoAnio = paso.Anio ?? anio;

            // Calculate mesCiclo for this specific PASO
            // "Anio" es siempre el año en que arranca el ciclo (sin importar el mes de inicio).
            int cicloStartYear = pasoAnio;
            int cicloStartAbs  = cicloStartYear * 12 + paso.MesInicio - 1;
            int requestedAbs   = anio * 12 + mes - 1;

            // Skip if requested calendar month is outside this PASO's cycle
            if (requestedAbs < cicloStartAbs || requestedAbs > cicloStartAbs + 11) continue;

            int mesCiclo = requestedAbs - cicloStartAbs + 1;

            // Check if activity covers this cycle month
            if (act.MesInicio > mesCiclo || act.MesFin < mesCiclo) continue;
            if (!EsMesPlanificado(act.Frecuencia, mesCiclo, act.MesInicio)) continue;

            var existe = await ctx.SsomaPasoEjecuciones
                .AnyAsync(e => e.ActividadId == act.Id
                    && e.FechaProgramada.Year == anio
                    && e.FechaProgramada.Month == mes);
            if (existe) continue;

            ctx.SsomaPasoEjecuciones.Add(new SsomaPasoEjecucion
            {
                ActividadId      = act.Id,
                FechaProgramada  = primer,
                FechaVerificacion = ultimo,
                Estado           = "Programado",
                CreatedAt        = DateTime.UtcNow
            });
        }

        await ctx.SaveChangesAsync();
    }
}
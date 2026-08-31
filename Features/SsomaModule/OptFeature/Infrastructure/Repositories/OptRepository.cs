using Abril_Backend.Features.SsomaModule.OptFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.OptFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.OptFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.SsomaModule.OptFeature.Infrastructure.Repositories;

public class OptRepository : IOptRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public OptRepository(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<OptPetDto>> GetPetsAsync()
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsomaPet
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .Select(p => new OptPetDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Codigo = p.Codigo,
                SharepointUrl = p.SharepointUrl
            })
            .ToListAsync();
    }

    public async Task<List<OptCriterioVerificacionDto>> GetCriteriosVerificacionAsync()
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsomaOptCriterioVerificacion
            .Where(c => c.Activo)
            .OrderBy(c => c.Orden)
            .Select(c => new OptCriterioVerificacionDto
            {
                Id = c.Id,
                Pregunta = c.Pregunta,
                Orden = c.Orden
            })
            .ToListAsync();
    }

    public async Task<int> CrearOptAsync(CrearOptRequest request, string? firmaObservadorUrl,
        Dictionary<int, string> firmasTrabajadorUrls, List<string> fotosAreaUrls, int userId = 0)
    {
        using var ctx = _factory.CreateDbContext();

        var pasosEvaluados = request.Pasos
            .Where(p => p.Resultado == "Seguro" || p.Resultado == "Inseguro")
            .ToList();
        var totalSeguros   = pasosEvaluados.Count(p => p.Resultado == "Seguro");
        var totalInseguros = pasosEvaluados.Count(p => p.Resultado == "Inseguro");
        var totalEvaluados = totalSeguros + totalInseguros;
        decimal? scorePct  = totalEvaluados > 0
            ? Math.Round((decimal)totalSeguros / totalEvaluados * 100, 2)
            : null;

        var opt = new SsomaOpt
        {
            ProyectoId            = request.ProyectoId,
            PetId                 = request.PetId,
            Fecha                 = DateTime.SpecifyKind(request.Fecha.Date, DateTimeKind.Utc),
            TipoObservacion       = request.TipoObservacion,
            CuentaConPet          = request.CuentaConPet,
            Area                  = request.Area,
            SeInformaTrabajador   = request.SeInformaTrabajador,
            ObservadorId          = request.ObservadorId,
            ObservadorNombre      = request.ObservadorNombre,
            ObservadorCargo       = request.ObservadorCargo,
            FirmaObservadorUrl    = firmaObservadorUrl,
            SeFelicito            = request.SeFelicito,
            SeRecibieronComentarios = request.SeRecibieronComentarios,
            SeRetroalimento       = request.SeRetroalimento,
            SeObtuvoCCompromiso   = request.SeObtuvoCCompromiso,
            AccionRequerida       = request.AccionRequerida,
            AccionObservacion     = request.AccionObservacion,
            TotalPasos            = request.Pasos.Count,
            TotalSeguros          = totalSeguros,
            TotalInseguros        = totalInseguros,
            ScorePct              = scorePct,
            Estado                = "Completado",
            CreatedAt             = DateTime.UtcNow,
            CreatedBy             = userId > 0 ? userId : null
        };

        ctx.SsomaOpt.Add(opt);
        await ctx.SaveChangesAsync();

        foreach (var t in request.Trabajadores)
        {
            ctx.SsomaOptTrabajador.Add(new SsomaOptTrabajador
            {
                OptId              = opt.Id,
                TrabajadorId       = t.TrabajadorId,
                TipoTrabajador     = t.TipoTrabajador,
                TiempoEnObra       = t.TiempoEnObra,
                AniosExperiencia   = t.AniosExperiencia,
                FirmaTrabajadorUrl = firmasTrabajadorUrls.TryGetValue(t.TrabajadorId, out var url) ? url : null
            });
        }

        foreach (var v in request.Verificaciones)
        {
            ctx.SsomaOptVerificacion.Add(new SsomaOptVerificacion
            {
                OptId      = opt.Id,
                CriterioId = v.CriterioId,
                Resultado  = v.Resultado
            });
        }

        foreach (var p in request.Pasos)
        {
            ctx.SsomaOptPaso.Add(new SsomaOptPaso
            {
                OptId               = opt.Id,
                NumeroDisplay       = p.NumeroDisplay,
                Descripcion         = p.Descripcion,
                Nivel               = p.Nivel,
                Resultado           = p.Resultado,
                DesviacionObservada = p.DesviacionObservada,
                Orden               = p.Orden
            });
        }

        for (int j = 0; j < fotosAreaUrls.Count; j++)
        {
            ctx.SsomaOptFotoArea.Add(new SsomaOptFotoArea
            {
                OptId = opt.Id,
                Url = fotosAreaUrls[j],
                Orden = j,
                CreatedAt = DateTime.UtcNow
            });
        }

        await ctx.SaveChangesAsync();
        return opt.Id;
    }

    public async Task<OptDetalleDto?> GetDetalleAsync(int id)
    {
        using var ctx = _factory.CreateDbContext();
        var opt = await ctx.SsomaOpt
            .Include(o => o.Proyecto)
            .Include(o => o.Pet)
            .Include(o => o.Trabajadores).ThenInclude(t => t.Trabajador).ThenInclude(w => w!.Person)
            .Include(o => o.Trabajadores).ThenInclude(t => t.Trabajador).ThenInclude(w => w!.Contributor)
            .Include(o => o.Verificaciones).ThenInclude(v => v.Criterio)
            .Include(o => o.Pasos)
            .Include(o => o.FotosArea)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (opt == null) return null;

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var trabajadorIds = opt.Trabajadores.Select(t => t.TrabajadorId).ToList();
        var workerIdsParaEmpresa = trabajadorIds.ToList();
        if (opt.ObservadorId.HasValue) workerIdsParaEmpresa.Add(opt.ObservadorId.Value);

        var empresaVigentePorWorker = await ctx.WorkerVinculacion
            .Where(v => workerIdsParaEmpresa.Contains(v.WorkerId) && (v.FechaFin == null || v.FechaFin >= hoy))
            .GroupBy(v => v.WorkerId)
            .Select(g => g.OrderByDescending(v => v.FechaInicio).First())
            .ToDictionaryAsync(v => v.WorkerId, v => v.EmpresaId);

        var empresaIds = empresaVigentePorWorker.Values.Where(e => e.HasValue).Select(e => e!.Value).Distinct().ToList();
        var nombreEmpresaPorId = await ctx.Contributor
            .Where(c => empresaIds.Contains(c.ContributorId))
            .ToDictionaryAsync(c => c.ContributorId, c => c.ContributorNombreComercial ?? c.ContributorName);

        string? EmpresaNombreDe(int workerId) =>
            empresaVigentePorWorker.TryGetValue(workerId, out var eid) && eid.HasValue
                && nombreEmpresaPorId.TryGetValue(eid.Value, out var nombre) ? nombre : null;

        int? observadorEmpresaId = opt.ObservadorId.HasValue
            && empresaVigentePorWorker.TryGetValue(opt.ObservadorId.Value, out var oeid) ? oeid : null;

        return new OptDetalleDto
        {
            Id                      = opt.Id,
            ProyectoId              = opt.ProyectoId,
            ProyectoNombre          = opt.Proyecto?.ProjectDescription ?? "",
            PetId                   = opt.PetId,
            PetNombre               = opt.Pet?.Nombre,
            PetSharepointUrl        = opt.Pet?.SharepointUrl,
            Fecha                   = opt.Fecha,
            TipoObservacion         = opt.TipoObservacion,
            CuentaConPet            = opt.CuentaConPet,
            Area                    = opt.Area,
            SeInformaTrabajador     = opt.SeInformaTrabajador,
            ObservadorId            = opt.ObservadorId,
            ObservadorNombre        = opt.ObservadorNombre,
            ObservadorCargo         = opt.ObservadorCargo,
            ObservadorEmpresaId     = observadorEmpresaId,
            ObservadorEmpresaNombre = observadorEmpresaId.HasValue && nombreEmpresaPorId.TryGetValue(observadorEmpresaId.Value, out var oen) ? oen : null,
            FirmaObservadorUrl      = opt.FirmaObservadorUrl,
            SeFelicito              = opt.SeFelicito,
            SeRecibieronComentarios = opt.SeRecibieronComentarios,
            SeRetroalimento         = opt.SeRetroalimento,
            SeObtuvoCCompromiso     = opt.SeObtuvoCCompromiso,
            AccionRequerida         = opt.AccionRequerida,
            AccionObservacion       = opt.AccionObservacion,
            TotalPasos              = opt.TotalPasos,
            TotalSeguros            = opt.TotalSeguros,
            TotalInseguros          = opt.TotalInseguros,
            ScorePct                = opt.ScorePct,
            Estado                  = opt.Estado,
            CreatedAt               = opt.CreatedAt,
            Trabajadores = opt.Trabajadores.Select(t => new OptTrabajadorDto
            {
                Id                 = t.Id,
                TrabajadorId       = t.TrabajadorId,
                NombreTrabajador   = t.Trabajador?.Person?.FullName
                    ?? $"{t.Trabajador?.Person?.FirstName} {t.Trabajador?.Person?.FirstLastName}".Trim(),
                Dni                = t.Trabajador?.Person?.DocumentIdentityCode,
                TipoTrabajador     = t.TipoTrabajador,
                TiempoEnObra       = t.TiempoEnObra,
                AniosExperiencia   = t.AniosExperiencia,
                FirmaTrabajadorUrl = t.FirmaTrabajadorUrl,
                EmpresaId          = empresaVigentePorWorker.TryGetValue(t.TrabajadorId, out var eid) ? eid : null,
                EmpresaNombre      = EmpresaNombreDe(t.TrabajadorId)
            }).ToList(),
            Verificaciones = opt.Verificaciones
                .OrderBy(v => v.Criterio?.Orden)
                .Select(v => new OptVerificacionDto
                {
                    CriterioId = v.CriterioId,
                    Pregunta   = v.Criterio?.Pregunta ?? "",
                    Resultado  = v.Resultado
                }).ToList(),
            Pasos = opt.Pasos
                .OrderBy(p => p.Orden)
                .Select(p => new OptPasoDto
                {
                    Id                  = p.Id,
                    NumeroDisplay       = p.NumeroDisplay,
                    Descripcion         = p.Descripcion,
                    Nivel               = p.Nivel,
                    Resultado           = p.Resultado,
                    DesviacionObservada = p.DesviacionObservada,
                    Orden               = p.Orden
                }).ToList(),
            FotosArea = opt.FotosArea.OrderBy(f => f.Orden).Select(f => f.Url).ToList()
        };
    }

    public async Task<List<OptListItemDto>> GetListAsync(int? proyectoId, int? petId,
        string? tipoObservacion, DateTime? fechaDesde, DateTime? fechaHasta,
        int? trabajadorId, int page, int pageSize, int? empresaIdContratista = null,
        int? empresaObservadorId = null, int? empresaTrabajadorId = null)
    {
        using var ctx = _factory.CreateDbContext();
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var q = ctx.SsomaOpt
            .Include(o => o.Proyecto)
            .Include(o => o.Pet)
            .Include(o => o.Trabajadores).ThenInclude(t => t.Trabajador).ThenInclude(w => w!.Person)
            .AsQueryable();

        if (proyectoId.HasValue)            q = q.Where(o => o.ProyectoId == proyectoId.Value);
        if (petId.HasValue)                 q = q.Where(o => o.PetId == petId.Value);
        if (!string.IsNullOrEmpty(tipoObservacion)) q = q.Where(o => o.TipoObservacion == tipoObservacion);
        if (fechaDesde.HasValue)            q = q.Where(o => o.Fecha >= DateTime.SpecifyKind(fechaDesde.Value.Date, DateTimeKind.Utc));
        if (fechaHasta.HasValue)            q = q.Where(o => o.Fecha <= DateTime.SpecifyKind(fechaHasta.Value.Date, DateTimeKind.Utc));
        if (trabajadorId.HasValue)          q = q.Where(o => o.Trabajadores.Any(t => t.TrabajadorId == trabajadorId.Value));
        if (empresaIdContratista.HasValue)
            q = q.Where(o => o.Trabajadores.Any(t => ctx.WorkerVinculacion.Any(v =>
                v.WorkerId == t.TrabajadorId
                && v.EmpresaId == empresaIdContratista.Value
                && (v.FechaFin == null || v.FechaFin >= hoy))));
        if (empresaObservadorId.HasValue)
            q = q.Where(o => o.ObservadorId.HasValue && ctx.WorkerVinculacion.Any(v =>
                v.WorkerId == o.ObservadorId.Value
                && v.EmpresaId == empresaObservadorId.Value
                && (v.FechaFin == null || v.FechaFin >= hoy)));
        if (empresaTrabajadorId.HasValue)
            q = q.Where(o => o.Trabajadores.Any(t => ctx.WorkerVinculacion.Any(v =>
                v.WorkerId == t.TrabajadorId
                && v.EmpresaId == empresaTrabajadorId.Value
                && (v.FechaFin == null || v.FechaFin >= hoy))));

        var pagina = await q
            .OrderByDescending(o => o.Fecha)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                o.Id,
                ProyectoNombre = o.Proyecto != null ? o.Proyecto.ProjectDescription : "",
                PetNombre = o.Pet != null ? o.Pet.Nombre : null,
                o.Fecha,
                o.TipoObservacion,
                o.Area,
                o.ObservadorNombre,
                o.ObservadorId,
                TrabajadorPrincipalId = o.Trabajadores.OrderBy(t => t.Id).Select(t => (int?)t.TrabajadorId).FirstOrDefault(),
                TrabajadoresPrincipal = o.Trabajadores
                    .OrderBy(t => t.Id)
                    .Select(t => t.Trabajador != null && t.Trabajador.Person != null
                        ? t.Trabajador.Person.FullName
                          ?? $"{t.Trabajador.Person.FirstName} {t.Trabajador.Person.FirstLastName}".Trim()
                        : "")
                    .FirstOrDefault() ?? "",
                TotalTrabajadores = o.Trabajadores.Count,
                o.ScorePct,
                o.AccionRequerida,
                o.Estado,
                o.CreatedAt
            })
            .ToListAsync();

        var workerIds = pagina.SelectMany(o => new[] { o.ObservadorId, o.TrabajadorPrincipalId })
            .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var empresaVigentePorWorker = await ctx.WorkerVinculacion
            .Where(v => workerIds.Contains(v.WorkerId) && (v.FechaFin == null || v.FechaFin >= hoy))
            .GroupBy(v => v.WorkerId)
            .Select(g => g.OrderByDescending(v => v.FechaInicio).First())
            .ToDictionaryAsync(v => v.WorkerId, v => v.EmpresaId);
        var empresaIds = empresaVigentePorWorker.Values.Where(e => e.HasValue).Select(e => e!.Value).Distinct().ToList();
        var nombreEmpresaPorId = await ctx.Contributor
            .Where(c => empresaIds.Contains(c.ContributorId))
            .ToDictionaryAsync(c => c.ContributorId, c => c.ContributorNombreComercial ?? c.ContributorName);

        string? EmpresaNombreDe(int? workerId) =>
            workerId.HasValue && empresaVigentePorWorker.TryGetValue(workerId.Value, out var eid) && eid.HasValue
                && nombreEmpresaPorId.TryGetValue(eid.Value, out var nombre) ? nombre : null;

        return pagina.Select(o => new OptListItemDto
        {
            Id                      = o.Id,
            ProyectoNombre          = o.ProyectoNombre,
            PetNombre               = o.PetNombre,
            Fecha                   = o.Fecha,
            TipoObservacion         = o.TipoObservacion,
            Area                    = o.Area,
            ObservadorNombre        = o.ObservadorNombre,
            ObservadorEmpresaNombre = EmpresaNombreDe(o.ObservadorId),
            TrabajadoresPrincipal   = o.TrabajadoresPrincipal,
            TrabajadorPrincipalEmpresaNombre = EmpresaNombreDe(o.TrabajadorPrincipalId),
            TotalTrabajadores       = o.TotalTrabajadores,
            ScorePct                = o.ScorePct,
            AccionRequerida         = o.AccionRequerida,
            Estado                  = o.Estado,
            CreatedAt               = o.CreatedAt
        }).ToList();
    }

    public async Task<int> GetListCountAsync(int? proyectoId, int? petId,
        string? tipoObservacion, DateTime? fechaDesde, DateTime? fechaHasta, int? trabajadorId,
        int? empresaIdContratista = null, int? empresaObservadorId = null, int? empresaTrabajadorId = null)
    {
        using var ctx = _factory.CreateDbContext();
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var q = ctx.SsomaOpt.AsQueryable();
        if (proyectoId.HasValue)            q = q.Where(o => o.ProyectoId == proyectoId.Value);
        if (petId.HasValue)                 q = q.Where(o => o.PetId == petId.Value);
        if (!string.IsNullOrEmpty(tipoObservacion)) q = q.Where(o => o.TipoObservacion == tipoObservacion);
        if (fechaDesde.HasValue)            q = q.Where(o => o.Fecha >= DateTime.SpecifyKind(fechaDesde.Value.Date, DateTimeKind.Utc));
        if (fechaHasta.HasValue)            q = q.Where(o => o.Fecha <= DateTime.SpecifyKind(fechaHasta.Value.Date, DateTimeKind.Utc));
        if (trabajadorId.HasValue)          q = q.Where(o => o.Trabajadores.Any(t => t.TrabajadorId == trabajadorId.Value));
        if (empresaIdContratista.HasValue)
            q = q.Where(o => o.Trabajadores.Any(t => ctx.WorkerVinculacion.Any(v =>
                v.WorkerId == t.TrabajadorId
                && v.EmpresaId == empresaIdContratista.Value
                && (v.FechaFin == null || v.FechaFin >= hoy))));
        if (empresaObservadorId.HasValue)
            q = q.Where(o => o.ObservadorId.HasValue && ctx.WorkerVinculacion.Any(v =>
                v.WorkerId == o.ObservadorId.Value
                && v.EmpresaId == empresaObservadorId.Value
                && (v.FechaFin == null || v.FechaFin >= hoy)));
        if (empresaTrabajadorId.HasValue)
            q = q.Where(o => o.Trabajadores.Any(t => ctx.WorkerVinculacion.Any(v =>
                v.WorkerId == t.TrabajadorId
                && v.EmpresaId == empresaTrabajadorId.Value
                && (v.FechaFin == null || v.FechaFin >= hoy))));
        return await q.CountAsync();
    }

    public async Task UpdateFirmasAsync(int optId, string? firmaObservadorUrl, Dictionary<int, string> firmasTrabajadorUrls, List<string> fotosAreaUrls)
    {
        using var ctx = _factory.CreateDbContext();

        if (firmaObservadorUrl != null)
        {
            var opt = await ctx.SsomaOpt.FindAsync(optId);
            if (opt != null) opt.FirmaObservadorUrl = firmaObservadorUrl;
        }

        if (firmasTrabajadorUrls.Count > 0)
        {
            var trabajadores = await ctx.SsomaOptTrabajador
                .Where(t => t.OptId == optId && firmasTrabajadorUrls.Keys.Contains(t.TrabajadorId))
                .ToListAsync();
            foreach (var t in trabajadores)
                if (firmasTrabajadorUrls.TryGetValue(t.TrabajadorId, out var url))
                    t.FirmaTrabajadorUrl = url;
        }

        for (int j = 0; j < fotosAreaUrls.Count; j++)
        {
            ctx.SsomaOptFotoArea.Add(new SsomaOptFotoArea
            {
                OptId = optId,
                Url = fotosAreaUrls[j],
                Orden = j,
                CreatedAt = DateTime.UtcNow
            });
        }

        await ctx.SaveChangesAsync();
    }

    public async Task<OptDashboardDto> GetDashboardAsync(int? proyectoId, int? anio, int? empresaIdContratista = null)
    {
        using var ctx = _factory.CreateDbContext();
        var anioFiltro = anio ?? DateTime.UtcNow.Year;
        var mesActual  = DateTime.UtcNow.Month;
        var hoy = DateOnly.FromDateTime(DateTime.Today);

        var q = ctx.SsomaOpt.AsQueryable();
        if (proyectoId.HasValue) q = q.Where(o => o.ProyectoId == proyectoId.Value);
        if (empresaIdContratista.HasValue)
            q = q.Where(o => o.Trabajadores.Any(t => ctx.WorkerVinculacion.Any(v =>
                v.WorkerId == t.TrabajadorId
                && v.EmpresaId == empresaIdContratista.Value
                && (v.FechaFin == null || v.FechaFin >= hoy))));

        var all = await q
            .Include(o => o.Trabajadores).ThenInclude(t => t.Trabajador).ThenInclude(w => w!.Person)
            .Include(o => o.Trabajadores).ThenInclude(t => t.Trabajador).ThenInclude(w => w!.Contributor)
            .ToListAsync();

        var delAnio = all.Where(o => o.Fecha.Year == anioFiltro).ToList();
        var delMes  = delAnio.Where(o => o.Fecha.Month == mesActual).ToList();

        var tendencia = Enumerable.Range(1, 12).Select(m =>
        {
            var opts = delAnio.Where(o => o.Fecha.Month == m).ToList();
            return new OptScoreMensualDto
            {
                Anio         = anioFiltro,
                Mes          = m,
                MesNombre    = new DateTime(anioFiltro, m, 1).ToString("MMM", new System.Globalization.CultureInfo("es-PE")),
                ScorePromedio = opts.Any(o => o.ScorePct.HasValue)
                    ? Math.Round(opts.Where(o => o.ScorePct.HasValue).Average(o => o.ScorePct!.Value), 1)
                    : null,
                TotalOpts = opts.Count
            };
        }).ToList();

        var empresas = delAnio
            .SelectMany(o => o.Trabajadores.Select(t => new
            {
                o.ScorePct,
                EmpresaId     = t.Trabajador?.ContributorId ?? 0,
                EmpresaNombre = t.Trabajador?.Contributor?.ContributorNombreComercial ?? "Sin empresa"
            }))
            .Where(x => x.EmpresaId > 0)
            .GroupBy(x => new { x.EmpresaId, x.EmpresaNombre })
            .Select(g => new OptEmpresaRankingDto
            {
                EmpresaId     = g.Key.EmpresaId,
                EmpresaNombre = g.Key.EmpresaNombre,
                ScorePromedio = g.Any(x => x.ScorePct.HasValue)
                    ? Math.Round(g.Where(x => x.ScorePct.HasValue).Average(x => x.ScorePct!.Value), 1)
                    : null,
                TotalOpts = g.Select(x => x.ScorePct).Distinct().Count()
            })
            .OrderBy(e => e.ScorePromedio)
            .Take(10)
            .ToList();

        var trabajadoresRiesgo = delAnio
            .SelectMany(o => o.Trabajadores.Select(t => new
            {
                t.TrabajadorId,
                Nombre  = t.Trabajador?.Person?.FullName
                    ?? $"{t.Trabajador?.Person?.FirstName} {t.Trabajador?.Person?.FirstLastName}".Trim(),
                Empresa = t.Trabajador?.Contributor?.ContributorNombreComercial,
                o.ScorePct,
                o.TotalInseguros
            }))
            .GroupBy(x => new { x.TrabajadorId, x.Nombre, x.Empresa })
            .Select(g => new OptTrabajadorRiesgoDto
            {
                TrabajadorId     = g.Key.TrabajadorId,
                NombreTrabajador = g.Key.Nombre,
                Empresa          = g.Key.Empresa,
                ScorePromedio    = g.Any(x => x.ScorePct.HasValue)
                    ? Math.Round(g.Where(x => x.ScorePct.HasValue).Average(x => x.ScorePct!.Value), 1)
                    : null,
                TotalOpts        = g.Count(),
                TotalInseguros   = g.Sum(x => x.TotalInseguros)
            })
            .Where(t => t.ScorePromedio.HasValue && t.ScorePromedio < 80)
            .OrderBy(t => t.ScorePromedio)
            .Take(10)
            .ToList();

        var acciones = all
            .Where(o => !string.IsNullOrEmpty(o.AccionRequerida) && o.AccionRequerida != "Ninguna")
            .GroupBy(o => o.AccionRequerida!)
            .Select(g => new OptAccionResumenDto
            {
                TipoAccion = g.Key,
                Cantidad   = g.Count()
            })
            .ToList();

        return new OptDashboardDto
        {
            TotalOpts              = all.Count,
            TotalEsteMes           = delMes.Count,
            ScorePromedioGlobal    = all.Any(o => o.ScorePct.HasValue)
                ? Math.Round(all.Where(o => o.ScorePct.HasValue).Average(o => o.ScorePct!.Value), 1) : null,
            ScorePromedioEsteMes   = delMes.Any(o => o.ScorePct.HasValue)
                ? Math.Round(delMes.Where(o => o.ScorePct.HasValue).Average(o => o.ScorePct!.Value), 1) : null,
            AccionesPendientes     = acciones.Sum(a => a.Cantidad),
            TendenciaMensual       = tendencia,
            RankingEmpresas        = empresas,
            TopTrabajadoresRiesgo  = trabajadoresRiesgo,
            AccionesRequeridas     = acciones
        };
    }
}

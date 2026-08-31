using Abril_Backend.Features.SsomaModule.AuditoriaAtsFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.AuditoriaAtsFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.AuditoriaAtsFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.SsomaModule.AuditoriaAtsFeature.Infrastructure.Repositories;

public class AuditoriaAtsRepository : IAuditoriaAtsRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public AuditoriaAtsRepository(IDbContextFactory<AppDbContext> factory)
        => _factory = factory;

    public async Task<List<AuditoriaAtsPreguntaDto>> GetPreguntasAsync()
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsomaAuditoriaAtsPregunta
            .Where(p => p.Activo)
            .OrderBy(p => p.Orden)
            .Select(p => new AuditoriaAtsPreguntaDto
            {
                Id = p.Id,
                Orden = p.Orden,
                Texto = p.Texto,
            })
            .ToListAsync();
    }

    public async Task<(List<AuditoriaAtsListItemDto> Items, int Total)> GetListAsync(
        int? auditadoWorkerId, int? auditorWorkerId, int? proyectoId,
        DateOnly? fechaDesde, DateOnly? fechaHasta, string? estado,
        int page, int pageSize, int? empresaIdContratista = null)
    {
        using var ctx = _factory.CreateDbContext();
        var hoy = DateOnly.FromDateTime(DateTime.Today);

        var query = from a in ctx.SsomaAuditoriaAts
                    join auditor in ctx.Worker on a.AuditorWorkerId equals auditor.Id
                    join auditado in ctx.Worker on a.AuditadoWorkerId equals auditado.Id
                    join p in ctx.Project on a.ProyectoId equals p.ProjectId into pj
                    from proj in pj.DefaultIfEmpty()
                    select new { a, auditor, auditado, proj };

        if (auditadoWorkerId.HasValue)
            query = query.Where(x => x.a.AuditadoWorkerId == auditadoWorkerId.Value);
        if (auditorWorkerId.HasValue)
            query = query.Where(x => x.a.AuditorWorkerId == auditorWorkerId.Value);
        if (proyectoId.HasValue)
            query = query.Where(x => x.a.ProyectoId == proyectoId.Value);
        if (fechaDesde.HasValue)
            query = query.Where(x => x.a.Fecha >= fechaDesde.Value);
        if (fechaHasta.HasValue)
            query = query.Where(x => x.a.Fecha <= fechaHasta.Value);
        if (!string.IsNullOrEmpty(estado))
            query = query.Where(x => x.a.Estado == estado);
        if (empresaIdContratista.HasValue)
            // El contratista debe ver auditorias donde participa su empresa, ya sea
            // como auditada O como la que realizo la auditoria (antes solo se
            // consideraba la auditada, igual que el bug ya corregido en RAC).
            query = query.Where(x => ctx.WorkerVinculacion.Any(v =>
                (v.WorkerId == x.a.AuditadoWorkerId || v.WorkerId == x.a.AuditorWorkerId)
                && v.EmpresaId == empresaIdContratista.Value
                && (v.FechaFin == null || v.FechaFin >= hoy)));

        var total = await query.CountAsync();

        var rows = await query
            .OrderByDescending(x => x.a.Fecha)
            .ThenByDescending(x => x.a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.a.Id,
                x.a.Fecha,
                AuditorWorkerId = x.auditor.Id,
                AuditorNombre = x.auditor.ApellidoNombre ?? (x.auditor.Person != null ? x.auditor.Person.FullName : null) ?? string.Empty,
                AuditorCategoria = x.auditor.PuestoCatalogo == null || x.auditor.PuestoCatalogo.Categoria == null ? null : x.auditor.PuestoCatalogo.Categoria.Nombre,
                AuditorOcupacion = x.auditor.PuestoCatalogo == null ? null : x.auditor.PuestoCatalogo.Nombre,
                AuditadoWorkerId = x.auditado.Id,
                AuditadoNombre = x.auditado.ApellidoNombre ?? (x.auditado.Person != null ? x.auditado.Person.FullName : null) ?? string.Empty,
                AuditadoCategoria = x.auditado.PuestoCatalogo == null || x.auditado.PuestoCatalogo.Categoria == null ? null : x.auditado.PuestoCatalogo.Categoria.Nombre,
                AuditadoOcupacion = x.auditado.PuestoCatalogo == null ? null : x.auditado.PuestoCatalogo.Nombre,
                ProyectoNombre = x.proj != null ? x.proj.ProjectDescription : null,
                x.a.Actividad,
                x.a.Lugar,
                x.a.PuntajePromedio,
                x.a.Nivel,
                x.a.Estado,
                x.a.CreatedAt,
            })
            .ToListAsync();

        var workerIds = rows.Select(r => r.AuditorWorkerId).Concat(rows.Select(r => r.AuditadoWorkerId)).Distinct().ToList();

        var empresaVigentePorWorker = await ctx.WorkerVinculacion
            .Where(v => workerIds.Contains(v.WorkerId) && (v.FechaFin == null || v.FechaFin >= hoy))
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

        var items = rows.Select(x => new AuditoriaAtsListItemDto
        {
            Id = x.Id,
            Fecha = x.Fecha.ToString("yyyy-MM-dd"),
            AuditorNombre = x.AuditorNombre,
            AuditorEmpresaNombre = EmpresaNombreDe(x.AuditorWorkerId),
            AuditorCategoria = x.AuditorCategoria,
            AuditorOcupacion = x.AuditorOcupacion,
            AuditadoNombre = x.AuditadoNombre,
            AuditadoEmpresaNombre = EmpresaNombreDe(x.AuditadoWorkerId),
            AuditadoCategoria = x.AuditadoCategoria,
            AuditadoOcupacion = x.AuditadoOcupacion,
            ProyectoNombre = x.ProyectoNombre,
            Actividad = x.Actividad,
            Lugar = x.Lugar,
            PuntajePromedio = x.PuntajePromedio,
            Nivel = x.Nivel,
            Estado = x.Estado,
            CreatedAt = x.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        }).ToList();

        return (items, total);
    }

    public async Task<AuditoriaAtsDetalleDto?> GetDetalleAsync(int id)
    {
        using var ctx = _factory.CreateDbContext();

        var auditoria = await ctx.SsomaAuditoriaAts
            .Where(a => a.Id == id)
            .Select(a => new
            {
                a.Id, a.Fecha, a.AuditorWorkerId, a.AuditadoWorkerId,
                a.ProyectoId, a.EmailAuditado, a.Actividad, a.Lugar,
                a.PuntajePromedio, a.Nivel, a.Observaciones, a.Estado, a.CreatedAt,
            })
            .FirstOrDefaultAsync();

        if (auditoria is null) return null;

        var auditorInfo = await ctx.Worker
            .Where(w => w.Id == auditoria.AuditorWorkerId)
            .Select(w => new
            {
                Nombre = w.ApellidoNombre ?? (w.Person != null ? w.Person.FullName : null) ?? string.Empty,
                Categoria = w.PuestoCatalogo == null || w.PuestoCatalogo.Categoria == null ? null : w.PuestoCatalogo.Categoria.Nombre,
                Puesto = w.PuestoCatalogo == null ? null : w.PuestoCatalogo.Nombre,
            })
            .FirstOrDefaultAsync();

        var auditadoInfo = await ctx.Worker
            .Where(w => w.Id == auditoria.AuditadoWorkerId)
            .Select(w => new
            {
                Nombre = w.ApellidoNombre ?? (w.Person != null ? w.Person.FullName : null) ?? string.Empty,
                Categoria = w.PuestoCatalogo == null || w.PuestoCatalogo.Categoria == null ? null : w.PuestoCatalogo.Categoria.Nombre,
                Puesto = w.PuestoCatalogo == null ? null : w.PuestoCatalogo.Nombre,
            })
            .FirstOrDefaultAsync();

        var auditorNombre = auditorInfo?.Nombre ?? string.Empty;
        var auditadoNombre = auditadoInfo?.Nombre ?? string.Empty;

        string? proyectoNombre = null;
        if (auditoria.ProyectoId.HasValue)
            proyectoNombre = await ctx.Project
                .Where(p => p.ProjectId == auditoria.ProyectoId.Value)
                .Select(p => p.ProjectDescription)
                .FirstOrDefaultAsync();

        var respuestas = await ctx.SsomaAuditoriaAtsRespuesta
            .Where(r => r.AuditoriaId == id)
            .Join(ctx.SsomaAuditoriaAtsPregunta, r => r.PreguntaId, p => p.Id,
                (r, p) => new AuditoriaAtsRespuestaDto
                {
                    PreguntaId = p.Id,
                    PreguntaTexto = p.Texto,
                    Puntaje = r.Puntaje,
                    Comentario = r.Comentario,
                })
            .OrderBy(r => r.PreguntaId)
            .ToListAsync();

        var fotos = await ctx.SsomaAuditoriaAtsFoto
            .Where(f => f.AuditoriaId == id)
            .OrderBy(f => f.Orden)
            .Select(f => f.FotoBase64)
            .ToListAsync();

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var empresaId = await ctx.WorkerVinculacion
            .Where(v => v.WorkerId == auditoria.AuditadoWorkerId && (v.FechaFin == null || v.FechaFin >= hoy))
            .OrderByDescending(v => v.FechaInicio)
            .Select(v => v.EmpresaId)
            .FirstOrDefaultAsync();

        var empresaAuditorId = await ctx.WorkerVinculacion
            .Where(v => v.WorkerId == auditoria.AuditorWorkerId && (v.FechaFin == null || v.FechaFin >= hoy))
            .OrderByDescending(v => v.FechaInicio)
            .Select(v => v.EmpresaId)
            .FirstOrDefaultAsync();

        var empresaIds = new[] { empresaId, empresaAuditorId }.Where(e => e.HasValue).Select(e => e!.Value).Distinct().ToList();
        var nombreEmpresaPorId = await ctx.Contributor
            .Where(c => empresaIds.Contains(c.ContributorId))
            .ToDictionaryAsync(c => c.ContributorId, c => c.ContributorNombreComercial ?? c.ContributorName);

        return new AuditoriaAtsDetalleDto
        {
            Id = auditoria.Id,
            Fecha = auditoria.Fecha.ToString("yyyy-MM-dd"),
            AuditorWorkerId = auditoria.AuditorWorkerId,
            AuditorNombre = auditorNombre,
            AuditorEmpresaNombre = empresaAuditorId.HasValue && nombreEmpresaPorId.TryGetValue(empresaAuditorId.Value, out var an) ? an : null,
            AuditorCategoria = auditorInfo?.Categoria,
            AuditorOcupacion = auditorInfo?.Puesto,
            AuditadoWorkerId = auditoria.AuditadoWorkerId,
            AuditadoNombre = auditadoNombre,
            AuditadoEmpresaNombre = empresaId.HasValue && nombreEmpresaPorId.TryGetValue(empresaId.Value, out var dn) ? dn : null,
            AuditadoCategoria = auditadoInfo?.Categoria,
            AuditadoOcupacion = auditadoInfo?.Puesto,
            EmpresaId = empresaId,
            EmpresaAuditorId = empresaAuditorId,
            ProyectoId = auditoria.ProyectoId,
            ProyectoNombre = proyectoNombre,
            EmailAuditado = auditoria.EmailAuditado,
            Actividad = auditoria.Actividad,
            Lugar = auditoria.Lugar,
            PuntajePromedio = auditoria.PuntajePromedio,
            Nivel = auditoria.Nivel,
            Observaciones = auditoria.Observaciones,
            Estado = auditoria.Estado,
            CreatedAt = auditoria.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Respuestas = respuestas,
            Fotos = fotos,
        };
    }

    public async Task<int> CrearAsync(CrearAuditoriaAtsRequest request, decimal promedio, string nivel)
    {
        using var ctx = _factory.CreateDbContext();

        var auditoria = new SsomaAuditoriaAts
        {
            Fecha = request.Fecha,
            AuditorWorkerId = request.AuditorWorkerId,
            AuditadoWorkerId = request.AuditadoWorkerId,
            ProyectoId = request.ProyectoId,
            EmailAuditado = request.EmailAuditado,
            Actividad = request.Actividad,
            Lugar = request.Lugar,
            Observaciones = request.Observaciones,
            PuntajePromedio = promedio,
            Nivel = nivel,
            Estado = "Evaluado",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        ctx.SsomaAuditoriaAts.Add(auditoria);
        await ctx.SaveChangesAsync();

        // Respuestas
        if (request.Respuestas.Count > 0)
        {
            var respuestas = request.Respuestas.Select(r => new SsomaAuditoriaAtsRespuesta
            {
                AuditoriaId = auditoria.Id,
                PreguntaId = r.PreguntaId,
                Puntaje = r.Puntaje,
                Comentario = r.Comentario,
            }).ToList();
            ctx.SsomaAuditoriaAtsRespuesta.AddRange(respuestas);
        }

        // Fotos
        for (short i = 0; i < request.FotosBase64.Count; i++)
        {
            ctx.SsomaAuditoriaAtsFoto.Add(new SsomaAuditoriaAtsFoto
            {
                AuditoriaId = auditoria.Id,
                FotoBase64 = request.FotosBase64[i],
                Orden = i,
            });
        }

        await ctx.SaveChangesAsync();
        return auditoria.Id;
    }
}

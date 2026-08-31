using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.CostsModule.Shared.Models;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;
using Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Application.Services;

public class InhabilitadoListItemDto
{
    public int Id { get; set; }
    public int WorkerId { get; set; }
    public string? WorkerDni { get; set; }
    public string WorkerNombre { get; set; } = "";
    public string Empresa { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string? Motivo { get; set; }
    public int? PuntosAlMomento { get; set; }
    public DateTimeOffset FechaInicio { get; set; }
}

public class InhabilitarManualRequest
{
    public int WorkerId { get; set; }
    public string Motivo { get; set; } = "";
}

/// <summary>
/// Gestiona el ciclo de vida de inhabilitación SSOMA:
/// - Bloquea al trabajador cuando llega a 10 puntos netos o recibe retiro definitivo
/// - Desbloquea cuando la Escuelita reduce los puntos por debajo de 10
/// </summary>
public class SsomaInhabilitacionService
{
    private const int PUNTOS_BLOQUEO = 10;
    // Id del tipo sanción "Retiro definitivo del proyecto"
    private const int TIPO_SANCION_RETIRO_DEFINITIVO = 4;

    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ILogger<SsomaInhabilitacionService> _logger;

    public SsomaInhabilitacionService(
        IDbContextFactory<AppDbContext> factory,
        ILogger<SsomaInhabilitacionService> logger)
    {
        _factory = factory;
        _logger  = logger;
    }

    /// <summary>
    /// Calcula puntos netos del trabajador: SUM(amonestaciones) - SUM(escuelita).
    /// </summary>
    public async Task<int> GetPuntosNetosAsync(int workerId)
    {
        using var ctx = _factory.CreateDbContext();

        var ptosAmon = await ctx.SsomaAmonestaciones
            .Where(a => a.WorkerId == workerId && a.State)
            .SumAsync(a => (int?)a.PuntosInfraccion) ?? 0;

        var ptosEscuelita = await ctx.SsomaEscuelitas
            .Where(e => e.WorkerId == workerId)
            .SumAsync(e => (int?)e.PuntosDescontados) ?? 0;

        return Math.Max(0, ptosAmon - ptosEscuelita);
    }

    /// <summary>
    /// Llamar después de crear/confirmar una amonestación.
    /// Bloquea automáticamente si corresponde.
    /// </summary>
    public async Task EvaluarTrasBmonestacionAsync(int workerId, int tipoSancionId, int userId)
    {
        try
        {
            using var ctx = _factory.CreateDbContext();

            var worker = await ctx.Worker.FirstOrDefaultAsync(w => w.Id == workerId);
            if (worker is null) return;

            // Ya está inhabilitado — no hacer nada
            if (worker.WorkersEstadoId == WorkersEstadoIds.InhabilitadoSsoma) return;

            var puntosNetos = await GetPuntosNetosAsync(workerId);
            bool esRetiroDefinitivo = tipoSancionId == TIPO_SANCION_RETIRO_DEFINITIVO;

            if (puntosNetos < PUNTOS_BLOQUEO && !esRetiroDefinitivo) return;

            var tipo   = esRetiroDefinitivo ? "RETIRO_DEFINITIVO" : "PUNTOS";
            var motivo = esRetiroDefinitivo
                ? "Retiro definitivo del proyecto por sanción CRÍTICA."
                : $"Trabajador acumuló {puntosNetos} puntos de infracción (límite: {PUNTOS_BLOQUEO}).";

            await BloquearAsync(ctx, worker, tipo, motivo, puntosNetos, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluando inhabilitación SSOMA para worker {WorkerId}", workerId);
            throw;
        }
    }

    /// <summary>
    /// Llamar después de corregir una amonestación (edición manual). A diferencia de
    /// EvaluarTrasBmonestacionAsync (que solo puede bloquear, nunca desbloquear), esta
    /// recalcula desde cero y bloquea o desbloquea según corresponda — porque una corrección
    /// puede mover los puntos en cualquier dirección.
    /// </summary>
    public async Task ReevaluarAsync(int workerId, int userId)
    {
        using var ctx = _factory.CreateDbContext();

        var worker = await ctx.Worker.FirstOrDefaultAsync(w => w.Id == workerId);
        if (worker is null) return;

        var puntosNetos = await GetPuntosNetosAsync(workerId);
        var estaInhabilitado = worker.WorkersEstadoId == WorkersEstadoIds.InhabilitadoSsoma;

        if (puntosNetos >= PUNTOS_BLOQUEO && !estaInhabilitado)
        {
            await BloquearAsync(ctx, worker, "PUNTOS",
                $"Trabajador acumuló {puntosNetos} puntos de infracción tras corrección (límite: {PUNTOS_BLOQUEO}).",
                puntosNetos, userId);
        }
        else if (puntosNetos < PUNTOS_BLOQUEO && estaInhabilitado)
        {
            // Solo se desbloquea automáticamente si el bloqueo activo fue por puntos —
            // un retiro definitivo o una inhabilitación manual no se levantan por esta vía.
            var activa = await ctx.SsomaInhabilitaciones
                .Where(i => i.WorkerId == workerId && i.FechaFin == null)
                .OrderByDescending(i => i.FechaInicio)
                .FirstOrDefaultAsync();
            if (activa?.Tipo == "PUNTOS")
                await DesbloquearAsync(ctx, worker, escuelitaId: null, userId, puntosNetos);
        }
    }

    /// <summary>
    /// Llamar después de registrar un curso de Escuelita.
    /// Desbloquea si los puntos netos bajan de 10.
    /// </summary>
    public async Task EvaluarTrasEscuelitaAsync(int workerId, int escuelitaId, int userId)
    {
        try
        {
            using var ctx = _factory.CreateDbContext();

            var worker = await ctx.Worker.FirstOrDefaultAsync(w => w.Id == workerId);
            if (worker is null || worker.WorkersEstadoId != WorkersEstadoIds.InhabilitadoSsoma) return;

            var puntosNetos = await GetPuntosNetosAsync(workerId);

            if (puntosNetos >= PUNTOS_BLOQUEO) return; // sigue bloqueado

            await DesbloquearAsync(ctx, worker, escuelitaId, userId, puntosNetos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluando desbloqueo SSOMA para worker {WorkerId}", workerId);
            throw;
        }
    }

    // ── Acciones manuales ─────────────────────────────────────────────────────

    public async Task<List<InhabilitadoListItemDto>> GetListaInhabilitadosAsync()
    {
        using var ctx = _factory.CreateDbContext();

        // Obtenemos inhabilitados básicos con datos del worker
        var rows = await ctx.SsomaInhabilitaciones
            .Where(i => i.FechaFin == null)
            .OrderByDescending(i => i.FechaInicio)
            .Join(ctx.Worker, i => i.WorkerId, w => w.Id, (i, w) => new { i, w })
            .GroupJoin(ctx.Person, x => x.w.PersonId, p => p.PersonId, (x, ps) => new { x.i, x.w, ps })
            .SelectMany(x => x.ps.DefaultIfEmpty(), (x, p) => new { x.i, x.w, p })
            .Select(x => new
            {
                x.i.Id, x.i.WorkerId, x.i.Tipo, x.i.Motivo, x.i.PuntosAlMomento, x.i.FechaInicio,
                WorkerDni    = x.p != null ? x.p.DocumentIdentityCode : null,
                WorkerNombre = x.w.ApellidoNombre ?? "",
            })
            .ToListAsync();

        // Obtenemos empresa por worker vía vinculación más reciente
        var workerIds = rows.Select(r => r.WorkerId).Distinct().ToList();
        var empresaMap = await ctx.WorkerVinculacion
            .Where(v => workerIds.Contains(v.WorkerId) && v.EmpresaId.HasValue)
            .OrderByDescending(v => v.Id)
            .GroupBy(v => v.WorkerId)
            .Select(g => new { WorkerId = g.Key, EmpresaId = g.First().EmpresaId })
            .Join(ctx.Contributor, v => v.EmpresaId, c => (int?)c.ContributorId, (v, c) => new { v.WorkerId, c.ContributorName })
            .ToDictionaryAsync(x => x.WorkerId, x => x.ContributorName);

        var inhabilitados = rows.Select(r => new InhabilitadoListItemDto
        {
            Id              = r.Id,
            WorkerId        = r.WorkerId,
            WorkerDni       = r.WorkerDni,
            WorkerNombre    = r.WorkerNombre,
            Empresa         = empresaMap.TryGetValue(r.WorkerId, out var emp) ? emp : "",
            Tipo            = r.Tipo,
            Motivo          = r.Motivo,
            PuntosAlMomento = r.PuntosAlMomento,
            FechaInicio     = r.FechaInicio,
        }).ToList();

        return inhabilitados;
    }

    public async Task InhabilitarManualAsync(int workerId, string motivo, int userId)
    {
        using var ctx = _factory.CreateDbContext();

        var worker = await ctx.Worker.FirstOrDefaultAsync(w => w.Id == workerId)
            ?? throw new AbrilException("Trabajador no encontrado.", 404);

        if (worker.WorkersEstadoId == WorkersEstadoIds.InhabilitadoSsoma)
            throw new AbrilException("El trabajador ya se encuentra inhabilitado.", 409);

        await BloquearAsync(ctx, worker, "MANUAL", motivo, null, userId);
    }

    public async Task DesbloquearManualAsync(int inhabilitacionId, int userId)
    {
        using var ctx = _factory.CreateDbContext();

        var inh = await ctx.SsomaInhabilitaciones
            .FirstOrDefaultAsync(i => i.Id == inhabilitacionId && i.FechaFin == null)
            ?? throw new AbrilException("Inhabilitación activa no encontrada.", 404);

        var worker = await ctx.Worker.FirstOrDefaultAsync(w => w.Id == inh.WorkerId)
            ?? throw new AbrilException("Trabajador no encontrado.", 404);

        inh.FechaFin        = DateTimeOffset.UtcNow;
        inh.DesbloqueadoPor = userId > 0 ? userId : null;
        worker.WorkersEstadoId = WorkersEstadoIds.Activo;
        worker.UpdatedAt    = DateTimeOffset.UtcNow;

        ctx.WorkerEvento.Add(new WorkerEvento
        {
            WorkerId    = worker.Id,
            TipoEvento  = "DesbloqueoSsomaManual",
            Descripcion = $"Trabajador desbloqueado manualmente por usuario {userId}.",
            UsuarioId   = userId > 0 ? userId : null,
            CreatedAt   = DateTime.UtcNow,
        });

        await ctx.SaveChangesAsync();
    }

    // ── Privados ──────────────────────────────────────────────────────────────

    private static async Task BloquearAsync(AppDbContext ctx, Worker worker,
        string tipo, string motivo, int? puntos, int userId)
    {
        worker.WorkersEstadoId = WorkersEstadoIds.InhabilitadoSsoma;
        worker.UpdatedAt  = DateTimeOffset.UtcNow;

        ctx.SsomaInhabilitaciones.Add(new SsomaInhabilitacion
        {
            WorkerId        = worker.Id,
            Tipo            = tipo,
            Motivo          = motivo,
            PuntosAlMomento = puntos,
            FechaInicio     = DateTimeOffset.UtcNow,
        });

        ctx.WorkerEvento.Add(new WorkerEvento
        {
            WorkerId    = worker.Id,
            TipoEvento  = "InhabilitacionSsoma",
            Descripcion = motivo,
            UsuarioId   = userId > 0 ? userId : null,
            CreatedAt   = DateTime.UtcNow,
        });

        await ctx.SaveChangesAsync();
    }

    private static async Task DesbloquearAsync(AppDbContext ctx, Worker worker,
        int? escuelitaId, int userId, int puntosNetos)
    {
        // Cerrar inhabilitación activa
        var inhabilitacionActiva = await ctx.SsomaInhabilitaciones
            .Where(i => i.WorkerId == worker.Id && i.FechaFin == null)
            .OrderByDescending(i => i.FechaInicio)
            .FirstOrDefaultAsync();

        if (inhabilitacionActiva is not null)
        {
            inhabilitacionActiva.FechaFin         = DateTimeOffset.UtcNow;
            inhabilitacionActiva.DesbloqueadoPor  = userId > 0 ? userId : null;
            inhabilitacionActiva.EscuelitaId      = escuelitaId;
        }

        worker.WorkersEstadoId = WorkersEstadoIds.Activo;
        worker.UpdatedAt = DateTimeOffset.UtcNow;

        ctx.WorkerEvento.Add(new WorkerEvento
        {
            WorkerId    = worker.Id,
            TipoEvento  = "DesbloqueoSsoma",
            Descripcion = escuelitaId.HasValue
                ? $"Trabajador desbloqueado tras Escuelita Abril. Puntos netos actuales: {puntosNetos}."
                : $"Trabajador desbloqueado tras corrección de amonestación. Puntos netos actuales: {puntosNetos}.",
            UsuarioId   = userId > 0 ? userId : null,
            CreatedAt   = DateTime.UtcNow,
        });

        await ctx.SaveChangesAsync();
    }
}

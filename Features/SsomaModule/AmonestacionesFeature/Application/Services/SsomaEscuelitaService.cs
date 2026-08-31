using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Application.Services;

public record EscuelitaRegistrarRequest(
    int WorkerId,
    string Fecha,           // yyyy-MM-dd
    int PuntosDescontados,
    string? Observaciones);

public record EscuelitaItemDto(
    int Id,
    string WorkerNombre,
    string WorkerDni,
    DateOnly Fecha,
    int PuntosDescontados,
    string? Observaciones,
    int PuntosNetos,
    bool Inhabilitado,
    DateTime CreatedAt);

public class SsomaEscuelitaService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly SsomaInhabilitacionService _inhabilitacion;

    public SsomaEscuelitaService(
        IDbContextFactory<AppDbContext> factory,
        SsomaInhabilitacionService inhabilitacion)
    {
        _factory        = factory;
        _inhabilitacion = inhabilitacion;
    }

    public async Task<int> RegistrarAsync(EscuelitaRegistrarRequest req, int userId)
    {
        if (req.PuntosDescontados <= 0)
            throw new AbrilException("Los puntos a descontar deben ser mayor a 0.", 400);

        if (!DateOnly.TryParse(req.Fecha, out var fecha))
            throw new AbrilException("Fecha inválida.", 400);

        using var ctx = _factory.CreateDbContext();

        var worker = await ctx.Worker.FirstOrDefaultAsync(w => w.Id == req.WorkerId)
            ?? throw new AbrilException("Trabajador no encontrado.", 404);

        var escuelita = new SsomaEscuelita
        {
            WorkerId         = req.WorkerId,
            Fecha            = fecha,
            PuntosDescontados = req.PuntosDescontados,
            Observaciones    = req.Observaciones,
            RegistradoPor    = userId > 0 ? userId : null,
        };

        ctx.SsomaEscuelitas.Add(escuelita);
        await ctx.SaveChangesAsync();

        // Evaluar si corresponde desbloquear
        await _inhabilitacion.EvaluarTrasEscuelitaAsync(req.WorkerId, escuelita.Id, userId);

        return escuelita.Id;
    }

    public async Task<List<EscuelitaItemDto>> GetByWorkerAsync(int workerId)
    {
        using var ctx = _factory.CreateDbContext();

        // Datos del worker
        var worker = await ctx.Worker
            .Where(w => w.Id == workerId)
            .Select(w => new { w.Id, Nombre = w.Person != null ? w.Person.FullName : "", Dni = w.Person != null ? w.Person.DocumentIdentityCode : "" })
            .FirstOrDefaultAsync()
            ?? throw new AbrilException("Trabajador no encontrado.", 404);

        var cursos = await ctx.SsomaEscuelitas
            .Where(e => e.WorkerId == workerId)
            .OrderByDescending(e => e.Fecha)
            .ToListAsync();

        var puntosNetos = await _inhabilitacion.GetPuntosNetosAsync(workerId);

        var inhabilitado = await ctx.Worker
            .Where(w => w.Id == workerId)
            .Select(w => w.WorkersEstadoId == WorkersEstadoIds.InhabilitadoSsoma)
            .FirstOrDefaultAsync();

        return cursos.Select(c => new EscuelitaItemDto(
            c.Id,
            worker.Nombre ?? "",
            worker.Dni ?? "",
            c.Fecha,
            c.PuntosDescontados,
            c.Observaciones,
            puntosNetos,
            inhabilitado,
            c.CreatedAt.UtcDateTime
        )).ToList();
    }
}

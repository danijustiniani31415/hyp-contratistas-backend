using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.CharlasFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.CharlasFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.CharlasFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.SsomaModule.CharlasFeature.Application.Services;

public class CharlaContratistaService : ICharlaContratistaService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ISharePointHabService _sp;

    public CharlaContratistaService(IDbContextFactory<AppDbContext> factory, ISharePointHabService sp)
    {
        _factory = factory;
        _sp = sp;
    }

    public async Task<List<CharlaContratistaPendienteDto>> GetPendientesAsync(int empresaId, DateOnly fecha)
    {
        using var ctx = _factory.CreateDbContext();

        var tareados = await (
            from t in ctx.SsTareo
            join d in ctx.SsTareoDetalleContratista on t.Id equals d.TareoId
            join p in ctx.Project on t.ProyectoId equals p.ProjectId
            where t.Fecha == fecha && d.EmpresaId == empresaId
            select new { t.ProyectoId, ProyectoNombre = p.ProjectDescription, d.CantidadPersonas }
        ).ToListAsync();

        if (tareados.Count == 0) return new List<CharlaContratistaPendienteDto>();

        var proyectoIds = tareados.Select(t => t.ProyectoId).Distinct().ToList();
        var subidas = await ctx.SsCharlaContratista
            .Where(c => c.State && c.EmpresaId == empresaId && c.Fecha == fecha && proyectoIds.Contains(c.ProyectoId))
            .ToDictionaryAsync(c => c.ProyectoId, c => c.Id);

        return tareados
            .GroupBy(t => new { t.ProyectoId, t.ProyectoNombre })
            .Select(g => new CharlaContratistaPendienteDto
            {
                ProyectoId = g.Key.ProyectoId,
                ProyectoNombre = g.Key.ProyectoNombre,
                Fecha = fecha,
                CantidadPersonasTareadas = g.Sum(x => x.CantidadPersonas),
                YaSubida = subidas.ContainsKey(g.Key.ProyectoId),
                CharlaId = subidas.TryGetValue(g.Key.ProyectoId, out var id) ? id : null,
            })
            .OrderBy(p => p.YaSubida)
            .ThenBy(p => p.ProyectoNombre)
            .ToList();
    }

    public async Task<List<CharlaContratistaPendienteDto>> GetDiasFaltantesAsync(int empresaId)
    {
        using var ctx = _factory.CreateDbContext();
        var hoy = DateOnly.FromDateTime(DateTime.Today);

        var tareados = await (
            from t in ctx.SsTareo
            join d in ctx.SsTareoDetalleContratista on t.Id equals d.TareoId
            join p in ctx.Project on t.ProyectoId equals p.ProjectId
            where t.Fecha < hoy && d.EmpresaId == empresaId
            select new { t.ProyectoId, ProyectoNombre = p.ProjectDescription, t.Fecha, d.CantidadPersonas }
        ).ToListAsync();

        if (tareados.Count == 0) return new List<CharlaContratistaPendienteDto>();

        var subidas = await ctx.SsCharlaContratista
            .Where(c => c.State && c.EmpresaId == empresaId && c.Fecha < hoy)
            .Select(c => new { c.ProyectoId, c.Fecha })
            .ToListAsync();
        var subidasSet = subidas.Select(s => (s.ProyectoId, s.Fecha)).ToHashSet();

        return tareados
            .GroupBy(t => new { t.ProyectoId, t.ProyectoNombre, t.Fecha })
            .Where(g => !subidasSet.Contains((g.Key.ProyectoId, g.Key.Fecha)))
            .Select(g => new CharlaContratistaPendienteDto
            {
                ProyectoId = g.Key.ProyectoId,
                ProyectoNombre = g.Key.ProyectoNombre,
                Fecha = g.Key.Fecha,
                CantidadPersonasTareadas = g.Sum(x => x.CantidadPersonas),
                YaSubida = false,
            })
            .OrderByDescending(p => p.Fecha)
            .ThenBy(p => p.ProyectoNombre)
            .ToList();
    }

    public async Task<List<CharlaContratistaDto>> GetHistorialAsync(int empresaId, int page, int pageSize)
    {
        using var ctx = _factory.CreateDbContext();
        page = page < 1 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        var query =
            from c in ctx.SsCharlaContratista
            join p in ctx.Project on c.ProyectoId equals p.ProjectId
            where c.State && c.EmpresaId == empresaId
            orderby c.Fecha descending, c.Id descending
            select new { c, p.ProjectDescription };

        var rows = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var aprobadorIds = rows.Where(r => r.c.AprobadoPorId.HasValue).Select(r => r.c.AprobadoPorId!.Value).Distinct().ToList();
        var aprobadores = aprobadorIds.Count == 0
            ? new Dictionary<int, string>()
            : await ctx.User.Include(u => u.Person)
                .Where(u => aprobadorIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, u => u.Person?.FullName ?? string.Empty);

        return rows.Select(r => new CharlaContratistaDto
        {
            Id = r.c.Id,
            ProyectoId = r.c.ProyectoId,
            ProyectoNombre = r.ProjectDescription,
            Fecha = r.c.Fecha,
            Tema = r.c.Tema,
            Descripcion = r.c.Descripcion,
            EvidenciaUrl = r.c.EvidenciaUrl,
            EvidenciaNombre = r.c.EvidenciaNombre,
            CreatedAt = r.c.CreatedAt,
            Estado = r.c.Estado,
            AprobadoPorNombre = r.c.AprobadoPorId.HasValue && aprobadores.TryGetValue(r.c.AprobadoPorId.Value, out var nombre) ? nombre : null,
            AprobadoEn = r.c.AprobadoEn,
            MotivoRechazo = r.c.MotivoRechazo,
        }).ToList();
    }

    public async Task<CharlaContratistaDto> SubirAsync(int empresaId, CharlaContratistaUploadRequest req, int userId)
    {
        if (string.IsNullOrWhiteSpace(req.Tema))
            throw new AbrilException("El tema de la charla es requerido.", 400);
        if (!DateOnly.TryParse(req.Fecha, out var fecha))
            throw new AbrilException("Fecha inválida.", 400);

        using var ctx = _factory.CreateDbContext();

        // El contratista solo puede subir la charla de un día en el que su empresa
        // fue efectivamente tareada en ese proyecto (control de acceso).
        var fueTareado = await (
            from t in ctx.SsTareo
            join d in ctx.SsTareoDetalleContratista on t.Id equals d.TareoId
            where t.Fecha == fecha && t.ProyectoId == req.ProyectoId && d.EmpresaId == empresaId
            select t.Id
        ).AnyAsync();
        if (!fueTareado)
            throw new AbrilException("Tu empresa no fue tareada en ese proyecto para la fecha indicada.", 400);

        var yaExiste = await ctx.SsCharlaContratista.AnyAsync(c =>
            c.State && c.EmpresaId == empresaId && c.ProyectoId == req.ProyectoId && c.Fecha == fecha);
        if (yaExiste)
            throw new AbrilException("Ya registraste la charla de ese día para este proyecto.", 400);

        string? evidenciaUrl = null;
        if (!string.IsNullOrEmpty(req.EvidenciaBase64))
        {
            var base64 = req.EvidenciaBase64.Contains(',') ? req.EvidenciaBase64.Split(',')[1] : req.EvidenciaBase64;
            var bytes = Convert.FromBase64String(base64);
            var nombre = string.IsNullOrWhiteSpace(req.EvidenciaNombre) ? "evidencia.jpg" : req.EvidenciaNombre;
            var ext = Path.GetExtension(nombre);
            if (string.IsNullOrEmpty(ext)) ext = ".jpg";
            var fileName = $"charla-contratista-{empresaId}-{fecha:yyyyMMdd}-{DateTime.UtcNow:HHmmssfff}{ext}";
            using var stream = new MemoryStream(bytes);
            evidenciaUrl = await _sp.SubirArchivoYObtenerUrlAsync(
                stream, fileName, "charlas-evidencias", $"Contratistas/{empresaId}/{fecha:yyyy}");
        }

        var entidad = new SsCharlaContratista
        {
            ProyectoId = req.ProyectoId,
            EmpresaId = empresaId,
            Fecha = fecha,
            Tema = req.Tema.Trim(),
            Descripcion = req.Descripcion,
            EvidenciaUrl = evidenciaUrl,
            EvidenciaNombre = req.EvidenciaNombre,
            SubidoPorUserId = userId,
        };
        ctx.SsCharlaContratista.Add(entidad);
        await ctx.SaveChangesAsync();

        var proyectoNombre = await ctx.Project
            .Where(p => p.ProjectId == req.ProyectoId)
            .Select(p => p.ProjectDescription)
            .FirstOrDefaultAsync() ?? "";

        return new CharlaContratistaDto
        {
            Id = entidad.Id,
            ProyectoId = entidad.ProyectoId,
            ProyectoNombre = proyectoNombre,
            Fecha = entidad.Fecha,
            Tema = entidad.Tema,
            Descripcion = entidad.Descripcion,
            EvidenciaUrl = entidad.EvidenciaUrl,
            EvidenciaNombre = entidad.EvidenciaNombre,
            CreatedAt = entidad.CreatedAt,
            Estado = entidad.Estado,
        };
    }

    public async Task<List<CharlaContratistaPendienteDto>> GetIncumplimientosAsync(DateOnly fecha, int? proyectoId)
    {
        using var ctx = _factory.CreateDbContext();

        var tareadosQuery =
            from t in ctx.SsTareo
            join d in ctx.SsTareoDetalleContratista on t.Id equals d.TareoId
            join p in ctx.Project on t.ProyectoId equals p.ProjectId
            join emp in ctx.Contributor on d.EmpresaId equals emp.ContributorId
            where t.Fecha == fecha
            select new { t.ProyectoId, ProyectoNombre = p.ProjectDescription, d.EmpresaId, EmpresaNombre = emp.ContributorName, d.CantidadPersonas };

        if (proyectoId.HasValue)
            tareadosQuery = tareadosQuery.Where(x => x.ProyectoId == proyectoId.Value);

        var tareados = await tareadosQuery.ToListAsync();
        if (tareados.Count == 0) return new List<CharlaContratistaPendienteDto>();

        var subidas = await ctx.SsCharlaContratista
            .Where(c => c.State && c.Fecha == fecha)
            .Select(c => new { c.ProyectoId, c.EmpresaId })
            .ToListAsync();
        var subidasSet = subidas.Select(s => (s.ProyectoId, s.EmpresaId)).ToHashSet();

        return tareados
            .Where(t => !subidasSet.Contains((t.ProyectoId, t.EmpresaId)))
            .Select(t => new CharlaContratistaPendienteDto
            {
                ProyectoId = t.ProyectoId,
                ProyectoNombre = $"{t.ProyectoNombre} — {t.EmpresaNombre}",
                Fecha = fecha,
                CantidadPersonasTareadas = t.CantidadPersonas,
                YaSubida = false,
            })
            .OrderBy(p => p.ProyectoNombre)
            .ToList();
    }

    // ── NEW: Revisión SSOMA / Prevencionista ─────────────────────────────────

    public async Task<CharlaContratistaRevisionResultDto> GetRevisionAsync(string? estado, int? proyectoId, int page, int pageSize)
    {
        using var ctx = _factory.CreateDbContext();
        page = page < 1 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        var query =
            from c in ctx.SsCharlaContratista
            join p in ctx.Project on c.ProyectoId equals p.ProjectId
            join emp in ctx.Contributor on c.EmpresaId equals emp.ContributorId
            where c.State
            select new { c, p.ProjectDescription, emp.ContributorName };

        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(x => x.c.Estado == estado);
        if (proyectoId.HasValue)
            query = query.Where(x => x.c.ProyectoId == proyectoId.Value);

        var total = await query.CountAsync();

        var rows = await query
            .OrderByDescending(x => x.c.Fecha).ThenByDescending(x => x.c.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        var aprobadorIds = rows.Where(r => r.c.AprobadoPorId.HasValue).Select(r => r.c.AprobadoPorId!.Value).Distinct().ToList();
        var aprobadores = aprobadorIds.Count == 0
            ? new Dictionary<int, string>()
            : await ctx.User.Include(u => u.Person)
                .Where(u => aprobadorIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, u => u.Person?.FullName ?? string.Empty);

        var items = rows.Select(r => new CharlaContratistaDto
        {
            Id = r.c.Id,
            ProyectoId = r.c.ProyectoId,
            ProyectoNombre = r.ProjectDescription,
            EmpresaNombre = r.ContributorName,
            Fecha = r.c.Fecha,
            Tema = r.c.Tema,
            Descripcion = r.c.Descripcion,
            EvidenciaUrl = r.c.EvidenciaUrl,
            EvidenciaNombre = r.c.EvidenciaNombre,
            CreatedAt = r.c.CreatedAt,
            Estado = r.c.Estado,
            AprobadoPorNombre = r.c.AprobadoPorId.HasValue && aprobadores.TryGetValue(r.c.AprobadoPorId.Value, out var nombre) ? nombre : null,
            AprobadoEn = r.c.AprobadoEn,
            MotivoRechazo = r.c.MotivoRechazo,
        }).ToList();

        return new CharlaContratistaRevisionResultDto { Items = items, Total = total };
    }

    public async Task<CharlaContratistaDto> AprobarAsync(int id, int userId)
    {
        using var ctx = _factory.CreateDbContext();
        var charla = await ctx.SsCharlaContratista.FirstOrDefaultAsync(c => c.Id == id && c.State)
            ?? throw new AbrilException("Charla de contratista no encontrada.", 404);
        if (charla.Estado != "Enviado")
            throw new AbrilException("Solo se puede aprobar una charla en estado Enviado.", 400);

        charla.Estado = "Aprobado";
        charla.AprobadoPorId = userId;
        charla.AprobadoEn = DateTime.UtcNow;
        charla.MotivoRechazo = null;
        await ctx.SaveChangesAsync();

        return await MapConAprobadorAsync(ctx, charla);
    }

    public async Task<CharlaContratistaDto> RechazarAsync(int id, string motivo, int userId)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new AbrilException("El motivo del rechazo es requerido.", 400);

        using var ctx = _factory.CreateDbContext();
        var charla = await ctx.SsCharlaContratista.FirstOrDefaultAsync(c => c.Id == id && c.State)
            ?? throw new AbrilException("Charla de contratista no encontrada.", 404);
        if (charla.Estado != "Enviado")
            throw new AbrilException("Solo se puede rechazar una charla en estado Enviado.", 400);

        charla.Estado = "Rechazado";
        charla.AprobadoPorId = userId;
        charla.AprobadoEn = DateTime.UtcNow;
        charla.MotivoRechazo = motivo.Trim();
        await ctx.SaveChangesAsync();

        return await MapConAprobadorAsync(ctx, charla);
    }

    private static async Task<CharlaContratistaDto> MapConAprobadorAsync(AppDbContext ctx, SsCharlaContratista charla)
    {
        var proyectoNombre = await ctx.Project.Where(p => p.ProjectId == charla.ProyectoId)
            .Select(p => p.ProjectDescription).FirstOrDefaultAsync() ?? "";
        var aprobadorNombre = charla.AprobadoPorId.HasValue
            ? await ctx.User.Include(u => u.Person).Where(u => u.UserId == charla.AprobadoPorId.Value)
                .Select(u => u.Person.FullName).FirstOrDefaultAsync()
            : null;

        return new CharlaContratistaDto
        {
            Id = charla.Id,
            ProyectoId = charla.ProyectoId,
            ProyectoNombre = proyectoNombre,
            Fecha = charla.Fecha,
            Tema = charla.Tema,
            Descripcion = charla.Descripcion,
            EvidenciaUrl = charla.EvidenciaUrl,
            EvidenciaNombre = charla.EvidenciaNombre,
            CreatedAt = charla.CreatedAt,
            Estado = charla.Estado,
            AprobadoPorNombre = aprobadorNombre,
            AprobadoEn = charla.AprobadoEn,
            MotivoRechazo = charla.MotivoRechazo,
        };
    }
}

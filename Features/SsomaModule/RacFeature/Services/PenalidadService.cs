using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.Rac.Dtos;
using Abril_Backend.Features.Ssoma.Rac.Entities;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Ssoma.Rac.Services;

public class PenalidadService : IPenalidadService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IRacNotificationService _notifService;
    private readonly ILogger<PenalidadService> _logger;

    public PenalidadService(
        IDbContextFactory<AppDbContext> factory,
        IRacNotificationService notifService,
        ILogger<PenalidadService> logger)
    {
        _factory      = factory;
        _notifService = notifService;
        _logger       = logger;
    }

    public async Task<RacPagedResult<PenalidadListItemDto>> GetListAsync(PenalidadListQuery q)
    {
        using var ctx = _factory.CreateDbContext();

        var query = ctx.SsomaRacPenalidades.AsQueryable();

        if (q.EmpresaId.HasValue)
            query = query.Where(p => p.EmpresaId == q.EmpresaId.Value);
        if (q.ProyectoId.HasValue)
            query = query.Where(p => p.ProyectoId == q.ProyectoId.Value);
        if (!string.IsNullOrEmpty(q.Estado))
            query = query.Where(p => p.Estado == q.Estado);

        var total    = await query.CountAsync();
        var page     = q.Page < 1 ? 1 : q.Page;
        var pageSize = q.PageSize <= 0 ? 20 : Math.Min(q.PageSize, 100);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PenalidadListItemDto
            {
                Id               = p.Id,
                Codigo           = p.Codigo,
                RacId            = p.RacId,
                RacCodigo        = p.Rac.Codigo,
                ProyectoNombre   = p.ProyectoId != null
                    ? ctx.Project
                        .Where(pr => pr.ProjectId == p.ProyectoId)
                        .Select(pr => pr.ProjectDescription)
                        .FirstOrDefault()
                    : null,
                EmpresaNombre    = p.EmpresaId != null
                    ? ctx.Contributor
                        .Where(c => c.ContributorId == p.EmpresaId)
                        .Select(c => c.ContributorName)
                        .FirstOrDefault()
                    : null,
                InfraccionNombre = p.Infraccion != null ? p.Infraccion.Nombre : null,
                MontoCalculado   = p.MontoCalculado,
                Estado           = p.Estado,
                CreatedAt        = p.CreatedAt,
                DescargoFecha    = p.DescargoFecha,
                ResueltaEn       = p.ResueltaEn,
                ResolucionTipo   = p.ResolucionTipo,
            })
            .ToListAsync();

        return new RacPagedResult<PenalidadListItemDto>
        {
            Items    = items,
            Total    = total,
            Page     = page,
            PageSize = pageSize
        };
    }

    public async Task<PenalidadDetalleDto?> GetDetalleAsync(int id)
    {
        using var ctx = _factory.CreateDbContext();

        var pen = await ctx.SsomaRacPenalidades
            .Include(p => p.Rac)
            .Include(p => p.Infraccion)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pen == null) return null;

        var proyectoNombre = pen.ProyectoId.HasValue
            ? await ctx.Project
                .Where(p => p.ProjectId == pen.ProyectoId.Value)
                .Select(p => p.ProjectDescription)
                .FirstOrDefaultAsync()
            : null;

        var empresaNombre = pen.EmpresaId.HasValue
            ? await ctx.Contributor
                .Where(c => c.ContributorId == pen.EmpresaId.Value)
                .Select(c => c.ContributorName)
                .FirstOrDefaultAsync()
            : null;

        return new PenalidadDetalleDto
        {
            Id                  = pen.Id,
            Codigo              = pen.Codigo,
            RacId               = pen.RacId,
            RacCodigo           = pen.Rac.Codigo,
            EmpresaId           = pen.EmpresaId,
            EmpresaNombre       = empresaNombre,
            ProyectoId          = pen.ProyectoId,
            ProyectoNombre      = proyectoNombre,
            InfraccionId        = pen.InfraccionId,
            InfraccionNombre    = pen.Infraccion?.Nombre,
            MontoCalculado      = pen.MontoCalculado,
            UitReferencia       = pen.UitReferencia,
            DescripcionOcurrido = pen.DescripcionOcurrido,
            Estado              = pen.Estado,
            DescargoTexto       = pen.DescargoTexto,
            DocumentoUrl        = pen.DocumentoUrl,
            DescargoFecha       = pen.DescargoFecha,
            ResolucionTexto     = pen.ResolucionTexto,
            ResolucionTipo      = pen.ResolucionTipo,
            ResueltaEn          = pen.ResueltaEn,
            PdfResolucionUrl    = pen.PdfResolucionUrl,
            CreatedAt           = pen.CreatedAt,
        };
    }

    public async Task PresentarDescargaAsync(int id, PenalidadDescargaRequest req, int userId)
    {
        using var ctx = _factory.CreateDbContext();

        var pen = await ctx.SsomaRacPenalidades.FindAsync(id)
            ?? throw new AbrilException("Penalidad no encontrada.", 404);

        if (pen.Estado != "EnEvaluacion")
            throw new AbrilException("Solo se puede presentar descargo en estado EnEvaluacion.", 400);

        if (string.IsNullOrWhiteSpace(req.DocumentoUrl))
            throw new AbrilException("El documento de sustento es obligatorio.", 400);

        pen.Estado            = "DescargoPresentado";
        pen.DescargoTexto     = req.DescargoTexto;
        pen.DocumentoUrl      = req.DocumentoUrl;
        pen.DescargoFecha     = DateTime.UtcNow;
        pen.DescargoUsuarioId = userId;
        pen.UpdatedAt         = DateTime.UtcNow;

        await ctx.SaveChangesAsync();
    }

    public async Task<PenalidadDetalleDto> ResolverAsync(int id, PenalidadResolverRequest req, int userId)
    {
        using var ctx = _factory.CreateDbContext();

        var pen = await ctx.SsomaRacPenalidades.FindAsync(id)
            ?? throw new AbrilException("Penalidad no encontrada.", 404);

        if (pen.Estado is "Aplicada" or "Anulada")
            throw new AbrilException("La penalidad ya fue resuelta.", 400);

        if (req.ResolucionTipo is not ("Aplicada" or "Anulada"))
            throw new AbrilException("ResolucionTipo debe ser 'Aplicada' o 'Anulada'.", 400);

        pen.Estado         = req.ResolucionTipo;
        pen.ResolucionTexto = req.ResolucionTexto;
        pen.ResolucionTipo  = req.ResolucionTipo;
        pen.ResueltoPorId   = userId;
        pen.ResueltaEn      = DateTime.UtcNow;
        pen.UpdatedAt       = DateTime.UtcNow;

        await ctx.SaveChangesAsync();

        var racId = pen.RacId;
        _ = Task.Run(async () =>
        {
            try
            {
                var racDto = await GetRacDetalleParaNotifAsync(racId);
                if (racDto != null)
                    await _notifService.NotificarPenalidadAsync(racDto);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error notificando penalidad resuelta (id={Id})", id);
            }
        });

        return await GetDetalleAsync(id)
            ?? throw new AbrilException("Penalidad no encontrada.", 404);
    }

    public async Task<string> GetPdfResolucionAsync(int id)
    {
        using var ctx = _factory.CreateDbContext();

        var url = await ctx.SsomaRacPenalidades
            .Where(p => p.Id == id)
            .Select(p => p.PdfResolucionUrl)
            .FirstOrDefaultAsync();

        if (url == null)
            throw new AbrilException("Penalidad no encontrada.", 404);

        if (string.IsNullOrWhiteSpace(url))
            throw new AbrilException("No hay PDF de resolución disponible.", 404);

        return url;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<RacDetalleDto?> GetRacDetalleParaNotifAsync(int racId)
    {
        using var ctx = _factory.CreateDbContext();

        var rac = await ctx.SsomaRacs
            .Include(r => r.Penalidad)
                .ThenInclude(p => p!.Infraccion)
            .FirstOrDefaultAsync(r => r.Id == racId);

        if (rac == null) return null;

        var proyectoNombre = await ctx.Project
            .Where(p => p.ProjectId == rac.ProyectoId)
            .Select(p => p.ProjectDescription)
            .FirstOrDefaultAsync();

        string? empReportadaNombre = null;
        if (rac.EmpresaReportadaId.HasValue)
        {
            empReportadaNombre = await ctx.Contributor
                .Where(c => c.ContributorId == rac.EmpresaReportadaId.Value)
                .Select(c => c.ContributorName)
                .FirstOrDefaultAsync();
        }

        return new RacDetalleDto
        {
            Id                     = rac.Id,
            Codigo                 = rac.Codigo,
            ProyectoNombre         = proyectoNombre,
            EmpresaReportadaId     = rac.EmpresaReportadaId,
            EmpresaReportadaNombre = empReportadaNombre,
            Penalidad = rac.Penalidad is null ? null : new RacPenalidadResumenDto
            {
                Id               = rac.Penalidad.Id,
                Codigo           = rac.Penalidad.Codigo,
                Estado           = rac.Penalidad.Estado,
                MontoCalculado   = rac.Penalidad.MontoCalculado,
                InfraccionNombre = rac.Penalidad.Infraccion?.Nombre
            }
        };
    }
}

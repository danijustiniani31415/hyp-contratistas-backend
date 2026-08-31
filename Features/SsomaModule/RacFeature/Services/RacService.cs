using System.Text.RegularExpressions;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.Rac.Dtos;
using Abril_Backend.Features.Ssoma.Rac.Entities;
using Abril_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Features.Ssoma.Rac.Services;

public class RacService : IRacService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IRacSharePointService _spService;
    private readonly IRacNotificationService _notifService;
    private readonly ILogger<RacService> _logger;

    public RacService(
        IDbContextFactory<AppDbContext> factory,
        IRacSharePointService spService,
        IRacNotificationService notifService,
        ILogger<RacService> logger)
    {
        _factory      = factory;
        _spService    = spService;
        _notifService = notifService;
        _logger       = logger;
    }

    public async Task<List<RacCategoriaDto>> GetCategoriasAsync()
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsomaRacCategorias
            .Where(c => c.Activo)
            .OrderBy(c => c.Orden)
            .ThenBy(c => c.Nombre)
            .Select(c => new RacCategoriaDto
            {
                Id     = c.Id,
                Nombre = c.Nombre,
                Tipo   = c.Tipo,
                Orden  = c.Orden
            })
            .ToListAsync();
    }

    public async Task<RacPagedResult<RacListItemDto>> GetListAsync(RacListQuery q)
    {
        using var ctx = _factory.CreateDbContext();

        var query = ctx.SsomaRacs.AsQueryable();

        if (q.ProyectoId.HasValue)
            query = query.Where(r => r.ProyectoId == q.ProyectoId.Value);
        if (!string.IsNullOrEmpty(q.Estado))
            query = query.Where(r => r.Estado == q.Estado);
        if (!string.IsNullOrEmpty(q.Severidad))
            query = query.Where(r => r.Severidad == q.Severidad);
        if (!string.IsNullOrEmpty(q.Tipo))
            query = query.Where(r => r.Tipo == q.Tipo);
        if (q.EmpresaReportadaId.HasValue)
            query = query.Where(r => r.EmpresaReportadaId == q.EmpresaReportadaId.Value);
        if (q.EmpresaReportanteId.HasValue)
            query = query.Where(r => r.EmpresaReportanteId == q.EmpresaReportanteId.Value);
        if (q.EmpresaIdContratista.HasValue)
            query = query.Where(r => r.EmpresaReportadaId == q.EmpresaIdContratista.Value
                                   || r.EmpresaReportanteId == q.EmpresaIdContratista.Value);
        if (q.FechaDesde.HasValue)
            query = query.Where(r => r.FechaReporte >= q.FechaDesde.Value);
        if (q.FechaHasta.HasValue)
            query = query.Where(r => r.FechaReporte <= q.FechaHasta.Value);
        if (q.SoloConPenalidad == true)
            query = query.Where(r => r.AplicaPenalidad);

        var total    = await query.CountAsync();
        var page     = q.Page < 1 ? 1 : q.Page;
        var pageSize = q.PageSize <= 0 ? 20 : Math.Min(q.PageSize, 100);

        var items = await query
            .OrderByDescending(r => r.FechaReporte)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RacListItemDto
            {
                Id                    = r.Id,
                Codigo                = r.Codigo,
                ProyectoNombre        = ctx.Project
                    .Where(p => p.ProjectId == r.ProyectoId)
                    .Select(p => p.ProjectDescription)
                    .FirstOrDefault(),
                ProyectoAbreviacion   = ctx.Project
                    .Where(p => p.ProjectId == r.ProyectoId)
                    .Select(p => p.Abbreviation)
                    .FirstOrDefault(),
                Tipo                  = r.Tipo,
                CategoriaNombre       = r.Categoria.Nombre,
                CategoriaAmbito       = r.Categoria.Ambito ?? "",
                Severidad             = r.Severidad,
                Estado                = r.Estado,
                FechaReporte          = r.FechaReporte,
                PlazoLevantamiento    = r.PlazoLevantamiento,
                AplicaPenalidad       = r.AplicaPenalidad,
                EmpresaReportadaNombre = r.EmpresaReportadaId != null
                    ? ctx.Contributor
                        .Where(c => c.ContributorId == r.EmpresaReportadaId)
                        .Select(c => c.ContributorName)
                        .FirstOrDefault()
                    : null,
                EmpresaReportanteNombre = r.EmpresaReportanteId != null
                    ? ctx.Contributor
                        .Where(c => c.ContributorId == r.EmpresaReportanteId)
                        .Select(c => c.ContributorName)
                        .FirstOrDefault()
                    : null,
                ReportanteNombre      = r.ReportanteNombre,
                Descripcion           = r.Descripcion,
            })
            .ToListAsync();

        return new RacPagedResult<RacListItemDto>
        {
            Items    = items,
            Total    = total,
            Page     = page,
            PageSize = pageSize
        };
    }

    public async Task<RacDetalleDto?> GetDetalleAsync(int id)
    {
        using var ctx = _factory.CreateDbContext();

        var rac = await ctx.SsomaRacs
            .Include(r => r.Categoria)
            .Include(r => r.Fotos.OrderBy(f => f.Orden))
            .Include(r => r.Penalidad)
                .ThenInclude(p => p!.Infraccion)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rac == null) return null;

        var proyectoNombre = await ctx.Project
            .Where(p => p.ProjectId == rac.ProyectoId)
            .Select(p => p.ProjectDescription)
            .FirstOrDefaultAsync();

        string? reportanteNombre = rac.ReportanteNombre;
        if (rac.ReportanteId.HasValue && string.IsNullOrEmpty(reportanteNombre))
        {
            reportanteNombre = await ctx.Person
                .Where(p => p.UserId == rac.ReportanteId.Value)
                .Select(p => p.FullName)
                .FirstOrDefaultAsync();
        }

        string? observadoNombre = null;
        if (rac.ObservadoWorkerId.HasValue)
        {
            observadoNombre = await ctx.Worker
                .Where(w => w.Id == rac.ObservadoWorkerId.Value)
                .Select(w => w.Person != null ? w.Person.FullName : null)
                .FirstOrDefaultAsync();
        }

        string? empReportanteNombre = null;
        if (rac.EmpresaReportanteId.HasValue)
        {
            empReportanteNombre = await ctx.Contributor
                .Where(c => c.ContributorId == rac.EmpresaReportanteId.Value)
                .Select(c => c.ContributorName)
                .FirstOrDefaultAsync();
        }

        string? empReportadaNombre = null;
        if (rac.EmpresaReportadaId.HasValue)
        {
            empReportadaNombre = await ctx.Contributor
                .Where(c => c.ContributorId == rac.EmpresaReportadaId.Value)
                .Select(c => c.ContributorName)
                .FirstOrDefaultAsync();
        }

        string? cerradoPorNombre = null;
        string? cerradoPorCargo = null;
        if (rac.CerradoPorId.HasValue)
        {
            var personaCierre = await ctx.Person
                .Where(p => p.UserId == rac.CerradoPorId.Value)
                .Select(p => new { p.PersonId, p.FullName })
                .FirstOrDefaultAsync();
            if (personaCierre is not null)
            {
                cerradoPorNombre = personaCierre.FullName;
                cerradoPorCargo = await ctx.Worker
                    .Where(w => w.PersonId == personaCierre.PersonId)
                    .Select(w => w.PuestoCatalogo == null ? null : w.PuestoCatalogo.Nombre)
                    .FirstOrDefaultAsync();
            }
        }

        return new RacDetalleDto
        {
            Id                      = rac.Id,
            Codigo                  = rac.Codigo,
            ProyectoId              = rac.ProyectoId,
            ProyectoNombre          = proyectoNombre,
            Tipo                    = rac.Tipo,
            CategoriaId             = rac.CategoriaId,
            CategoriaNombre         = rac.Categoria.Nombre,
            CategoriaAmbito         = rac.Categoria.Ambito ?? "",
            Severidad               = rac.Severidad,
            EsAnonimoReportante     = rac.EsAnonimoReportante,
            ReportanteId            = rac.EsAnonimoReportante ? null : rac.ReportanteId,
            ReportanteNombre        = rac.EsAnonimoReportante ? "Reporte anónimo" : reportanteNombre,
            ReportanteCargo         = rac.EsAnonimoReportante ? null : rac.ReportanteCargo,
            EmpresaReportanteId     = rac.EsAnonimoReportante ? null : rac.EmpresaReportanteId,
            EmpresaReportanteNombre = rac.EsAnonimoReportante ? null : empReportanteNombre,
            EsAnonimoObservado      = rac.EsAnonimoObservado,
            ObservadoWorkerId       = rac.ObservadoWorkerId,
            ObservadoNombre         = observadoNombre,
            EmpresaReportadaId      = rac.EmpresaReportadaId,
            EmpresaReportadaNombre  = empReportadaNombre,
            ProyectoPiso            = rac.ProyectoPiso,
            LugarDescripcion        = rac.LugarDescripcion,
            Latitud                 = rac.Latitud,
            Longitud                = rac.Longitud,
            Descripcion             = rac.Descripcion,
            PlanAccion              = rac.PlanAccion,
            FechaReporte            = rac.FechaReporte,
            PlazoLevantamiento      = rac.PlazoLevantamiento,
            Estado                  = rac.Estado,
            FechaCierre             = rac.FechaCierre,
            CierreDescripcion       = rac.CierreDescripcion,
            CerradoPorNombre        = cerradoPorNombre,
            CerradoPorCargo         = cerradoPorCargo,
            AplicaPenalidad         = rac.AplicaPenalidad,
            PdfUrl                  = rac.PdfUrl,
            CreatedAt               = rac.CreatedAt,
            Fotos = rac.Fotos.Select(f => new RacFotoDto
            {
                Id            = f.Id,
                Url           = f.Url,
                Tipo          = f.Tipo,
                NombreArchivo = f.NombreArchivo,
                Orden         = f.Orden
            }).ToList(),
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
    public async Task<RacCreadoDto> CrearAsync(RacCreateRequest req, int userId)
    {
        // ── Validaciones ──────────────────────────────────────────────────────
        if (req.AplicaPenalidad && req.InfraccionId is null)
            throw new AbrilException("El campo InfraccionId es requerido cuando aplica penalidad.", 400);

        using var ctx = _factory.CreateDbContext();

        if (req.AplicaPenalidad && req.EmpresaReportadaId.HasValue)
        {
            var esAbril = await ctx.Contributor
                .Where(c => c.ContributorId == req.EmpresaReportadaId.Value)
                .Select(c => c.EsAbril)
                .FirstOrDefaultAsync();
            if (esAbril)
                throw new AbrilException("La empresa Abril Ingeniería no puede ser objeto de penalidad.", 422);
        }

        // ── Snapshot reportante — siempre desde JWT ───────────────────────────
        // El autor se obtiene del userId (JWT), independientemente de EsAnonimoReportante
        string? reportanteNombre = null;
        string? reportanteCargo  = null;
        if (userId > 0)
        {
            var persona = await ctx.Person
                .Where(p => p.UserId == userId)
                .Select(p => new { p.FullName, p.PersonId })
                .FirstOrDefaultAsync();
            if (persona is not null)
            {
                reportanteNombre = persona.FullName;
                // Mismo criterio que WorkerSearchRepository.GetByUserId: una persona puede
                // arrastrar una ficha RETIRADA de un ingreso anterior, así que el snapshot
                // del cargo tiene que salir de la ficha vigente, no de la primera que caiga.
                var hoy = DateOnly.FromDateTime(DateTime.Today);
                var worker = await ctx.Worker
                    .Where(w => w.PersonId == persona.PersonId)
                    .OrderByDescending(w => ctx.WorkerVinculacion.Any(v =>
                        v.WorkerId == w.Id && (v.FechaFin == null || v.FechaFin >= hoy)))
                    .ThenByDescending(w => w.WorkersEstadoId == WorkersEstadoIds.Activo ? 1 : 0)
                    .ThenByDescending(w => w.Id)
                    .Select(w => new { Puesto = w.PuestoCatalogo == null ? null : w.PuestoCatalogo.Nombre })
                    .FirstOrDefaultAsync();
                if (!string.IsNullOrEmpty(worker?.Puesto))
                    reportanteCargo = worker.Puesto;
            }
        }

        // Fallback: la cuenta autenticada no tiene una Person vinculada (común en cuentas
        // internas/contratistas), así que se usa el observador elegido en el formulario
        // en vez de dejar el reportante y cargo vacíos.
        if (string.IsNullOrEmpty(reportanteNombre) && req.ReportanteId.HasValue)
        {
            var workerReportante = await ctx.Worker
                .Where(w => w.Id == req.ReportanteId.Value)
                .Select(w => new
                {
                    w.ApellidoNombre,
                    Puesto = w.PuestoCatalogo == null ? null : w.PuestoCatalogo.Nombre,
                    Categoria = w.PuestoCatalogo == null || w.PuestoCatalogo.Categoria == null
                        ? null : w.PuestoCatalogo.Categoria.Nombre
                })
                .FirstOrDefaultAsync();
            if (workerReportante is not null)
            {
                reportanteNombre = workerReportante.ApellidoNombre;
                reportanteCargo = !string.IsNullOrEmpty(req.ReportanteCargo)
                    ? req.ReportanteCargo
                    : (workerReportante.Puesto ?? workerReportante.Categoria);
            }
        }

        // ── Transacción ───────────────────────────────────────────────────────
        var strategy  = ctx.Database.CreateExecutionStrategy();
        RacCreadoDto resultado = default!;
        int racId    = 0;
        string racCodigo = "";

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await ctx.Database.BeginTransactionAsync();
            try
            {
                var project = await ctx.Project
                    .Where(p => p.ProjectId == req.ProyectoId)
                    .FirstOrDefaultAsync()
                    ?? throw new AbrilException("Proyecto no encontrado.", 404);

                var abbrev = project.Abbreviation ?? req.ProyectoId.ToString();
                var year   = DateTime.UtcNow.Year;

                var nuevoContadorRac = project.ContadorRac + 1;
                var codigoRac        = $"RAC-{year}-{abbrev}-{nuevoContadorRac:D3}";

                var rac = new SsomaRac
                {
                    Codigo              = codigoRac,
                    ProyectoId          = req.ProyectoId,
                    Tipo                = req.Tipo,
                    CategoriaId         = req.CategoriaId,
                    Severidad           = req.Severidad,
                    EsAnonimoReportante = req.EsAnonimoReportante,
                    ReportanteId        = userId > 0 ? userId : req.ReportanteId,
                    ReportanteNombre    = reportanteNombre,
                    ReportanteCargo     = reportanteCargo,
                    EmpresaReportanteId = req.EmpresaReportanteId,
                    EsAnonimoObservado  = req.EsAnonimoObservado,
                    ObservadoWorkerId   = req.ObservadoWorkerId,
                    EmpresaReportadaId  = req.EmpresaReportadaId,
                    ProyectoPiso        = req.ProyectoPiso,
                    LugarDescripcion    = req.LugarDescripcion,
                    Latitud             = req.Latitud,
                    Longitud            = req.Longitud,
                    Descripcion         = req.Descripcion,
                    PlanAccion          = req.PlanAccion,
                    FechaReporte        = DateTime.SpecifyKind(req.FechaReporte, DateTimeKind.Utc),
                    PlazoLevantamiento  = req.PlazoLevantamiento.HasValue
                                          ? DateTime.SpecifyKind(req.PlazoLevantamiento.Value, DateTimeKind.Utc)
                                          : (DateTime?)null,
                    AplicaPenalidad     = req.AplicaPenalidad,
                    CreatedBy           = userId,
                    CreatedAt           = DateTime.UtcNow
                };

                ctx.SsomaRacs.Add(rac);
                project.ContadorRac = nuevoContadorRac;

                SsomaRacPenalidad? penalidad = null;
                if (req.AplicaPenalidad)
                {
                    var nuevoContadorPen = project.ContadorPenalidad + 1;
                    var codigoPen        = $"PEN-{year}-{abbrev}-{nuevoContadorPen:D3}";

                    penalidad = new SsomaRacPenalidad
                    {
                        Codigo              = codigoPen,
                        Rac                 = rac,
                        EmpresaId           = req.EmpresaReportadaId,
                        ProyectoId          = req.ProyectoId,
                        InfraccionId        = req.InfraccionId,
                        MontoCalculado      = 0m,
                        UitReferencia       = 0m,
                        DescripcionOcurrido = req.DescripcionOcurrido,
                        CreatedBy           = userId,
                        CreatedAt           = DateTime.UtcNow
                    };

                    ctx.SsomaRacPenalidades.Add(penalidad);
                    project.ContadorPenalidad = nuevoContadorPen;
                }

                await ctx.SaveChangesAsync();
                await tx.CommitAsync();

                racId     = rac.Id;
                racCodigo = rac.Codigo;
                resultado = new RacCreadoDto
                {
                    Id              = rac.Id,
                    Codigo          = rac.Codigo,
                    PenalidadId     = penalidad?.Id,
                    PenalidadCodigo = penalidad?.Codigo
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });

        // ── PDF + SharePoint + Email (best-effort) ────────────────────────────
        try
        {
            var detalle = await GetDetalleAsync(racId);
            if (detalle != null)
            {
                var pdfBytes = await RacPdfService.GenerarPdfAsync(detalle, new List<(string Tipo, byte[] Bytes)>());
                using var pdfStream = new MemoryStream(pdfBytes);
                var pdfPath = await _spService.SubirPdfAsync(pdfStream, $"{racCodigo}.pdf", racId);

                using var ctx2 = _factory.CreateDbContext();
                var racRow = await ctx2.SsomaRacs.FindAsync(racId);
                if (racRow != null)
                {
                    racRow.PdfUrl = pdfPath;
                    await ctx2.SaveChangesAsync();
                }

                _ = Task.Run(async () =>
                {
                    try { await _notifService.NotificarRacCreadoAsync(detalle); }
                    catch { /* silencioso */ }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error post-creación RAC {Codigo}: PDF/SP/email no procesado", racCodigo);
        }

        return resultado;
    }
    public async Task<RacDetalleDto> CerrarAsync(int id, RacCerrarRequest req, int userId)
    {
        using var ctx = _factory.CreateDbContext();

        if (string.IsNullOrWhiteSpace(req.FotoCierreUrl))
            throw new AbrilException("Debe adjuntar una foto de evidencia del levantamiento.", 400);

        var rac = await ctx.SsomaRacs.FindAsync(id)
            ?? throw new AbrilException("RAC no encontrado.", 404);

        if (rac.Estado == "Cerrado")
            throw new AbrilException("El RAC ya está cerrado.", 400);

        rac.Estado            = "Cerrado";
        rac.FechaCierre       = DateTime.UtcNow;
        rac.CierreDescripcion = req.CierreDescripcion;
        rac.CerradoPorId      = userId;
        rac.UpdatedAt         = DateTime.UtcNow;

        ctx.SsomaRacFotos.Add(new SsomaRacFoto
        {
            RacId         = rac.Id,
            Url           = req.FotoCierreUrl,
            Tipo          = "Cierre",
            NombreArchivo = "foto-cierre",
            Orden         = 0,
            SubidoPor     = userId,
            CreatedAt     = DateTime.UtcNow
        });

        await ctx.SaveChangesAsync();

        return await GetDetalleAsync(id)
            ?? throw new AbrilException("RAC no encontrado.", 404);
    }

    public async Task<RacFotoUploadResult> SubirFotoAsync(int racId, IFormFile file, string tipo, int userId)
    {
        using var ctx = _factory.CreateDbContext();

        var existe = await ctx.SsomaRacs.AnyAsync(r => r.Id == racId);
        if (!existe)
            throw new AbrilException("RAC no encontrado.", 404);

        var orden = await ctx.SsomaRacFotos.CountAsync(f => f.RacId == racId) + 1;

        using var stream = file.OpenReadStream();
        var path = await _spService.SubirFotoAsync(stream, file.FileName, racId);

        var foto = new SsomaRacFoto
        {
            RacId         = racId,
            Url           = path,
            Tipo          = tipo,
            NombreArchivo = file.FileName,
            Orden         = orden,
            SubidoPor     = userId,
            CreatedAt     = DateTime.UtcNow
        };

        ctx.SsomaRacFotos.Add(foto);
        await ctx.SaveChangesAsync();

        return new RacFotoUploadResult
        {
            Id            = foto.Id,
            Url           = foto.Url,
            Tipo          = foto.Tipo,
            NombreArchivo = foto.NombreArchivo ?? file.FileName
        };
    }
    public async Task<byte[]?> GetFotoBytesAsync(int fotoId)
    {
        using var ctx = _factory.CreateDbContext();
        var foto = await ctx.SsomaRacFotos.FirstOrDefaultAsync(f => f.Id == fotoId)
            ?? throw new AbrilException("Foto no encontrada.", 404);
        return await _spService.DescargarFotoAsync(foto.Url);
    }

    public Task<byte[]> GenerarPdf(RacDetalleDto rac) => RacPdfService.GenerarPdfAsync(rac, new List<(string Tipo, byte[] Bytes)>());

    public async Task<byte[]> GetPdfAsync(int id)
    {
        var rac = await GetDetalleAsync(id)
            ?? throw new AbrilException("RAC no encontrado.", 404);

        var fotoBytes = new List<(string Tipo, byte[] Bytes)>();
        foreach (var foto in rac.Fotos)
        {
            try
            {
                var bytes = await _spService.DescargarFotoAsync(foto.Url);
                if (bytes != null && bytes.Length > 0)
                    fotoBytes.Add((foto.Tipo, bytes));
            }
            catch { /* ignorar fotos que fallen */ }
        }

        return await RacPdfService.GenerarPdfAsync(rac, fotoBytes);
    }
    public async Task<RacDashboardDto> GetDashboardAsync(int? empresaIdContratista = null)
    {
        using var ctx = _factory.CreateDbContext();
        var ahora = DateTime.UtcNow;

        var baseQuery = ctx.SsomaRacs.AsQueryable();
        if (empresaIdContratista.HasValue)
            baseQuery = baseQuery.Where(r => r.EmpresaReportadaId == empresaIdContratista.Value);

        var totalAbiertos     = await baseQuery.CountAsync(r => r.Estado == "Abierto");
        var totalCerrados     = await baseQuery.CountAsync(r => r.Estado == "Cerrado");
        var totalConPenalidad = await baseQuery.CountAsync(r => r.AplicaPenalidad);
        var criticosAbiertos  = await baseQuery.CountAsync(r => r.Estado == "Abierto" && r.Severidad == "CRITICO");
        var altosAbiertos     = await baseQuery.CountAsync(r => r.Estado == "Abierto" && r.Severidad == "ALTO");
        var vencidosAbiertos  = await baseQuery.CountAsync(r => r.Estado == "Abierto" && r.PlazoLevantamiento < ahora);

        // RACs que esta empresa levantó/reportó (independiente de los que le fueron reportados a ella).
        var totalReportados = 0;
        var totalReportadosCerrados = 0;
        if (empresaIdContratista.HasValue)
        {
            var reportadosQuery = ctx.SsomaRacs.Where(r => r.EmpresaReportanteId == empresaIdContratista.Value);
            totalReportados = await reportadosQuery.CountAsync();
            totalReportadosCerrados = await reportadosQuery.CountAsync(r => r.Estado == "Cerrado");
        }

        // ── PorProyecto ───────────────────────────────────────────────────────
        var porProyectoRaw = await baseQuery
            .GroupBy(r => r.ProyectoId)
            .Select(g => new
            {
                ProyectoId = g.Key,
                Total      = g.Count(),
                Abiertos   = g.Count(r => r.Estado == "Abierto"),
                Cerrados   = g.Count(r => r.Estado == "Cerrado")
            })
            .OrderByDescending(x => x.Total)
            .Take(10)
            .ToListAsync();

        var proyIds = porProyectoRaw.Select(x => x.ProyectoId).ToList();
        var proyNombres = await ctx.Project
            .Where(p => proyIds.Contains(p.ProjectId))
            .Select(p => new { p.ProjectId, p.ProjectDescription })
            .ToListAsync();

        var porProyecto = porProyectoRaw.Select(x => new RacPorProyectoDto
        {
            ProyectoId     = x.ProyectoId,
            ProyectoNombre = proyNombres.FirstOrDefault(p => p.ProjectId == x.ProyectoId)?.ProjectDescription ?? "",
            Total          = x.Total,
            Abiertos       = x.Abiertos,
            Cerrados       = x.Cerrados
        }).ToList();

        // ── PorCategoria ──────────────────────────────────────────────────────
        var porCategoriaRaw = await baseQuery
            .GroupBy(r => r.CategoriaId)
            .Select(g => new { CategoriaId = g.Key, Total = g.Count() })
            .OrderByDescending(x => x.Total)
            .Take(10)
            .ToListAsync();

        var catIds = porCategoriaRaw.Select(x => x.CategoriaId).ToList();
        var cats   = await ctx.SsomaRacCategorias
            .Where(c => catIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Nombre, c.Ambito })
            .ToListAsync();

        var porCategoria = porCategoriaRaw.Select(x => new RacPorCategoriaDto
        {
            CategoriaNombre = cats.FirstOrDefault(c => c.Id == x.CategoriaId)?.Nombre ?? "",
            Ambito          = cats.FirstOrDefault(c => c.Id == x.CategoriaId)?.Ambito ?? "",
            Total           = x.Total
        }).ToList();

        // ── Tendencia — últimos 6 meses ───────────────────────────────────────
        var fechaCorte = DateTime.SpecifyKind(new DateTime(ahora.Year, ahora.Month, 1).AddMonths(-5), DateTimeKind.Utc);
        var tendencia  = await baseQuery
            .Where(r => r.FechaReporte >= fechaCorte)
            .GroupBy(r => new { r.FechaReporte.Year, r.FechaReporte.Month })
            .Select(g => new RacTendenciaDto
            {
                Anio        = g.Key.Year,
                Mes         = g.Key.Month,
                Total       = g.Count(),
                Actos       = g.Count(r => r.Tipo == "ACTO"),
                Condiciones = g.Count(r => r.Tipo == "CONDICION")
            })
            .OrderBy(x => x.Anio).ThenBy(x => x.Mes)
            .ToListAsync();

        return new RacDashboardDto
        {
            TotalAbiertos     = totalAbiertos,
            TotalCerrados     = totalCerrados,
            TotalConPenalidad = totalConPenalidad,
            CriticosAbiertos  = criticosAbiertos,
            AltosAbiertos     = altosAbiertos,
            VencidosAbiertos  = vencidosAbiertos,
            TotalReportados         = totalReportados,
            TotalReportadosCerrados = totalReportadosCerrados,
            PorProyecto       = porProyecto,
            PorCategoria      = porCategoria,
            Tendencia         = tendencia
        };
    }
    public async Task<List<RacInfraccionDto>> GetInfraccionesAsync()
    {
        using var ctx = _factory.CreateDbContext();
        var lista = await ctx.SsomaRacInfracciones
            .Where(x => x.Activo == true)
            .OrderBy(x => x.Nombre)
            .ToListAsync();

        return lista.Select(x => new RacInfraccionDto
        {
            Id       = x.Id,
            Nombre   = x.Nombre,
            FactorUit = x.FactorUit,
            MontoFijo = x.MontoFijo
        }).ToList();
    }

    public async Task<List<string>> GetNivelesProyectoAsync(int projectId)
    {
        using var ctx = _factory.CreateDbContext();
        var proj = await ctx.Project
            .Where(p => p.ProjectId == projectId)
            .Select(p => new { p.LevelDescription, p.NumNiveles, p.NumSotanos })
            .FirstOrDefaultAsync();
        if (proj is null) return new List<string>();
        return ParseNiveles(proj.LevelDescription, proj.NumNiveles, proj.NumSotanos);
    }

    private static List<string> ParseNiveles(string? levelDesc, string? numNiveles, string? numSotanos)
    {
        int pisos   = 0;
        int sotanos = 0;
        bool azotea = false;

        if (!string.IsNullOrWhiteSpace(levelDesc))
        {
            var d = levelDesc.ToUpperInvariant();
            azotea = d.Contains("AZOTEA");

            var mP = Regex.Match(d, @"(\d+)\s*PISO");
            if (mP.Success) pisos = int.Parse(mP.Groups[1].Value);

            var mS = Regex.Match(d, @"(\d+)\s*S[OÓ]TANO");
            if (mS.Success) sotanos = int.Parse(mS.Groups[1].Value);

            // Fallback: plain number like "8.0"
            if (pisos == 0)
            {
                var mN = Regex.Match(d, @"^\s*(\d+)");
                if (mN.Success && int.TryParse(mN.Groups[1].Value, out var n)) pisos = n;
            }
        }

        if (pisos   == 0 && int.TryParse(numNiveles, out var nv)) pisos   = nv;
        if (sotanos == 0 && int.TryParse(numSotanos, out var ns)) sotanos = ns;

        var lista = new List<string>();
        for (int i = sotanos; i >= 1; i--) lista.Add($"Sótano {i}");
        for (int i = 1; i <= pisos; i++)   lista.Add($"Piso {i}");
        if (azotea) lista.Add("Azotea");
        return lista;
    }
}

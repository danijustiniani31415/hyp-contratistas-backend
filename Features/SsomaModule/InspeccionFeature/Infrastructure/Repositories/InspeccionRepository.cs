using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.InspeccionFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.InspeccionFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.InspeccionFeature.Application.Services;
using Abril_Backend.Features.SsomaModule.InspeccionFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Interfaces;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.SsomaModule.InspeccionFeature.Infrastructure.Repositories;

public class InspeccionRepository : IInspeccionRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IEmailService _emailService;
    private readonly InspeccionPdfService _pdfService;
    private readonly ILogger<InspeccionRepository> _logger;

    public InspeccionRepository(
        IDbContextFactory<AppDbContext> factory,
        IEmailService emailService,
        InspeccionPdfService pdfService,
        ILogger<InspeccionRepository> logger)
    {
        _factory = factory;
        _emailService = emailService;
        _pdfService = pdfService;
        _logger = logger;
    }

    public async Task<(int? EmpresaId, int? EmpresaInspectoraId)> GetEmpresaIdDeHallazgoAsync(int hallazgoId)
    {
        using var ctx = _factory.CreateDbContext();
        var row = await ctx.SsomaInspeccionHallazgo
            .Where(h => h.Id == hallazgoId)
            .Select(h => new { h.Inspeccion!.EmpresaId, h.Inspeccion!.EmpresaInspectoraId })
            .FirstOrDefaultAsync();
        return (row?.EmpresaId, row?.EmpresaInspectoraId);
    }

    public async Task<List<InspeccionTipoDto>> GetTiposAsync()
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsomaInspeccionTipo
            .Where(t => t.Activo)
            .OrderBy(t => t.Ambito).ThenBy(t => t.Nombre)
            .Select(t => new InspeccionTipoDto
            {
                Id = t.Id,
                Nombre = t.Nombre,
                Ambito = t.Ambito,
                EsColaborativa = t.EsColaborativa
            })
            .ToListAsync();
    }

    public async Task<List<InspeccionChecklistItemDto>> GetChecklistItemsAsync(int tipoId)
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsomaInspeccionChecklistItem
            .Where(i => i.TipoId == tipoId && i.Activo)
            .OrderBy(i => i.Orden)
            .Select(i => new InspeccionChecklistItemDto
            {
                Id = i.Id,
                TipoId = i.TipoId,
                Pregunta = i.Pregunta,
                Categoria = i.Categoria,
                Orden = i.Orden
            })
            .ToListAsync();
    }

    public async Task<int> CrearInspeccionAsync(CrearInspeccionRequest request,
        string? firmaInspectorUrl, string? firmaRepresentanteUrl,
        Dictionary<int, List<string>> fotosHallazgoUrls, List<string> fotosAreaUrls, int? userId = null)
    {
        using var ctx = _factory.CreateDbContext();

        var totalCumple = request.Respuestas.Count(r => r.Resultado == "Cumple");
        var totalNoCumple = request.Respuestas.Count(r => r.Resultado == "NoCumple");
        var totalNa = request.Respuestas.Count(r => r.Resultado == "NA");
        var evaluados = totalCumple + totalNoCumple;
        decimal? tasa = evaluados > 0
            ? Math.Round((decimal)totalCumple / evaluados * 100, 2)
            : null;

        TimeOnly? horaInicio = null, horaFin = null;
        if (!string.IsNullOrEmpty(request.HoraInicio) && TimeOnly.TryParse(request.HoraInicio, out var hi))
            horaInicio = hi;
        if (!string.IsNullOrEmpty(request.HoraFin) && TimeOnly.TryParse(request.HoraFin, out var hf))
            horaFin = hf;

        // El formulario manda el worker del inspector; si un cliente viejo (o el flujo de
        // contratista) no lo envía, se deduce del usuario logueado. Sin esto la inspección solo
        // se podría atribuir por el texto del nombre, que es justo lo que se rompe cuando alguien
        // corrige un nombre en la ficha del trabajador.
        var inspectorWorkerId = request.InspectorWorkerId;
        if (inspectorWorkerId == null && userId != null)
        {
            inspectorWorkerId = await ctx.Person
                .Where(p => p.UserId == userId)
                .Join(ctx.Worker, p => p.PersonId, w => w.PersonId, (p, w) => (int?)w.Id)
                .FirstOrDefaultAsync();
        }

        var inspeccion = new SsomaInspeccion
        {
            ProyectoId = request.ProyectoId,
            TipoId = request.TipoId,
            EmpresaId = request.EmpresaId,
            EmpresaInspectoraId = request.EmpresaInspectoraId,
            EsPlanificada = request.EsPlanificada,
            Fecha = DateTime.SpecifyKind(request.Fecha.Date, DateTimeKind.Utc),
            HoraInicio = horaInicio,
            HoraFin = horaFin,
            Area = request.Area,
            ResponsableArea = request.ResponsableArea,
            InspectorWorkerId = inspectorWorkerId,
            InspectorNombre = request.InspectorNombre,
            InspectorCargo = request.InspectorCargo,
            InspectorEmpresa = request.InspectorEmpresa,
            FirmaInspectorUrl = firmaInspectorUrl,
            RepresentanteNombre = request.RepresentanteNombre,
            RepresentanteCargo = request.RepresentanteCargo,
            FirmaRepresentanteUrl = firmaRepresentanteUrl,
            DescripcionCausas = request.DescripcionCausas,
            Conclusiones = request.Conclusiones,
            CreatedBy = userId,
            TotalItems = request.Respuestas.Count,
            TotalCumple = totalCumple,
            TotalNoCumple = totalNoCumple,
            TotalNa = totalNa,
            TasaCumplimiento = tasa,
            Estado = request.EsColaborativa ? "Abierta" : (request.Hallazgos.Any() ? "En Proceso" : "Cerrada"),
            EsColaborativa = request.EsColaborativa,
            CreatedAt = DateTime.UtcNow
        };

        ctx.SsomaInspeccion.Add(inspeccion);
        await ctx.SaveChangesAsync();

        if (request.EsColaborativa && !string.IsNullOrWhiteSpace(request.InspectorNombre))
        {
            ctx.SsomaInspeccionParticipante.Add(new SsomaInspeccionParticipante
            {
                InspeccionId = inspeccion.Id,
                Nombre = request.InspectorNombre!,
                Cargo = request.InspectorCargo,
                Empresa = request.InspectorEmpresa,
                FechaUnion = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
        }

        foreach (var r in request.Respuestas)
        {
            ctx.SsomaInspeccionRespuesta.Add(new SsomaInspeccionRespuesta
            {
                InspeccionId = inspeccion.Id,
                ItemId = r.ItemId,
                Resultado = r.Resultado,
                Observacion = r.Observacion
            });
        }

        for (int i = 0; i < request.Hallazgos.Count; i++)
        {
            var h = request.Hallazgos[i];
            var hallazgo = new SsomaInspeccionHallazgo
            {
                InspeccionId = inspeccion.Id,
                Descripcion = h.Descripcion,
                Tipo = h.Tipo,
                Area = h.Area,
                ResponsableNombre = h.ResponsableNombre,
                ResponsableCargo = h.ResponsableCargo,
                FechaLimite = h.FechaLimite.HasValue ? DateTime.SpecifyKind(h.FechaLimite.Value, DateTimeKind.Utc) : null,
                AccionCorrectiva = h.AccionCorrectiva,
                Latitud = h.Latitud,
                Longitud = h.Longitud,
                Estado = "Abierto",
                CreatedAt = DateTime.UtcNow
            };
            ctx.SsomaInspeccionHallazgo.Add(hallazgo);
            await ctx.SaveChangesAsync();

            if (fotosHallazgoUrls.TryGetValue(i, out var urls))
            {
                for (int j = 0; j < urls.Count; j++)
                {
                    ctx.SsomaInspeccionHallazgoFoto.Add(new SsomaInspeccionHallazgoFoto
                    {
                        HallazgoId = hallazgo.Id,
                        Url = urls[j],
                        Orden = j,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        for (int j = 0; j < fotosAreaUrls.Count; j++)
        {
            ctx.SsomaInspeccionFotoArea.Add(new SsomaInspeccionFotoArea
            {
                InspeccionId = inspeccion.Id,
                Url = fotosAreaUrls[j],
                Orden = j,
                CreatedAt = DateTime.UtcNow
            });
        }

        await ctx.SaveChangesAsync();
        return inspeccion.Id;
    }

    public async Task CerrarHallazgoAsync(int hallazgoId, CerrarHallazgoRequest request, string? evidenciaUrl)
    {
        using var ctx = _factory.CreateDbContext();
        var hallazgo = await ctx.SsomaInspeccionHallazgo.FindAsync(hallazgoId)
            ?? throw new AbrilException("Hallazgo no encontrado.", 404);
        hallazgo.Estado = "Cerrado";
        hallazgo.AccionCorrectiva = request.AccionCorrectiva;
        hallazgo.EvidenciaCierreUrl = evidenciaUrl;
        hallazgo.FechaCierre = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    /// <summary>Solo editable mientras el hallazgo sigue "Abierto" y su inspección sigue
    /// "Abierta" — un hallazgo ya cerrado, o cuya inspección ya se cerró, es un registro final
    /// y no debe modificarse.</summary>
    public async Task EditarHallazgoAsync(int hallazgoId, EditarHallazgoRequest request)
    {
        using var ctx = _factory.CreateDbContext();
        var hallazgo = await ctx.SsomaInspeccionHallazgo.Include(h => h.Inspeccion)
            .FirstOrDefaultAsync(h => h.Id == hallazgoId)
            ?? throw new AbrilException("Hallazgo no encontrado.", 404);

        if (hallazgo.Estado != "Abierto")
            throw new AbrilException("Solo se pueden editar hallazgos abiertos.", 400);
        if (hallazgo.Inspeccion == null || hallazgo.Inspeccion.Estado != "Abierta")
            throw new AbrilException("La inspección ya está cerrada, no se puede editar el hallazgo.", 400);

        hallazgo.Descripcion = request.Descripcion;
        hallazgo.Tipo = request.Tipo;
        hallazgo.Area = request.Area;
        hallazgo.ResponsableNombre = request.ResponsableNombre;
        hallazgo.ResponsableCargo = request.ResponsableCargo;
        hallazgo.FechaLimite = request.FechaLimite.HasValue ? DateTime.SpecifyKind(request.FechaLimite.Value, DateTimeKind.Utc) : null;
        hallazgo.AccionCorrectiva = request.AccionCorrectiva;
        await ctx.SaveChangesAsync();
    }

    /// <summary>Soft delete — marca Estado = "Eliminado" en vez de borrar la fila, para
    /// conservar el registro ante una auditoría. Mismas condiciones que EditarHallazgoAsync.
    /// Todas las lecturas de hallazgos (detalle, PDF, dashboard, lista) ya excluyen este estado.</summary>
    public async Task EliminarHallazgoAsync(int hallazgoId)
    {
        using var ctx = _factory.CreateDbContext();
        var hallazgo = await ctx.SsomaInspeccionHallazgo.Include(h => h.Inspeccion)
            .FirstOrDefaultAsync(h => h.Id == hallazgoId)
            ?? throw new AbrilException("Hallazgo no encontrado.", 404);

        if (hallazgo.Estado != "Abierto")
            throw new AbrilException("Solo se pueden eliminar hallazgos abiertos.", 400);
        if (hallazgo.Inspeccion == null || hallazgo.Inspeccion.Estado != "Abierta")
            throw new AbrilException("La inspección ya está cerrada, no se puede eliminar el hallazgo.", 400);

        hallazgo.Estado = "Eliminado";
        await ctx.SaveChangesAsync();
    }

    public async Task ActualizarFirmasYFotosAsync(int id, string? firmaInspectorUrl, string? firmaRepresentanteUrl,
        Dictionary<int, List<string>> fotosHallazgoUrls, List<string> fotosAreaUrls)
    {
        using var ctx = _factory.CreateDbContext();

        if (firmaInspectorUrl != null || firmaRepresentanteUrl != null)
        {
            var inspeccion = await ctx.SsomaInspeccion.FindAsync(id)
                ?? throw new AbrilException("Inspección no encontrada.", 404);
            if (firmaInspectorUrl != null) inspeccion.FirmaInspectorUrl = firmaInspectorUrl;
            if (firmaRepresentanteUrl != null) inspeccion.FirmaRepresentanteUrl = firmaRepresentanteUrl;
            await ctx.SaveChangesAsync();
        }

        if (fotosHallazgoUrls.Any())
        {
            var hallazgos = await ctx.SsomaInspeccionHallazgo
                .Where(h => h.InspeccionId == id)
                .OrderBy(h => h.Id)
                .ToListAsync();

            foreach (var (i, urls) in fotosHallazgoUrls)
            {
                if (i >= hallazgos.Count) continue;
                for (int j = 0; j < urls.Count; j++)
                {
                    ctx.SsomaInspeccionHallazgoFoto.Add(new SsomaInspeccionHallazgoFoto
                    {
                        HallazgoId = hallazgos[i].Id,
                        Url = urls[j],
                        Orden = j,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            await ctx.SaveChangesAsync();
        }

        if (fotosAreaUrls.Any())
        {
            for (int j = 0; j < fotosAreaUrls.Count; j++)
            {
                ctx.SsomaInspeccionFotoArea.Add(new SsomaInspeccionFotoArea
                {
                    InspeccionId = id,
                    Url = fotosAreaUrls[j],
                    Orden = j,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await ctx.SaveChangesAsync();
        }
    }

    public async Task<InspeccionDetalleDto?> GetDetalleAsync(int id)
    {
        using var ctx = _factory.CreateDbContext();
        var insp = await ctx.SsomaInspeccion
            .Include(i => i.Proyecto)
            .Include(i => i.Tipo)
            .Include(i => i.Empresa)
            .Include(i => i.Respuestas).ThenInclude(r => r.Item)
            .Include(i => i.Hallazgos).ThenInclude(h => h.Fotos)
            .Include(i => i.FotosArea)
            .Include(i => i.Participantes)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (insp == null) return null;

        return new InspeccionDetalleDto
        {
            Id = insp.Id,
            ProyectoId = insp.ProyectoId,
            ProyectoNombre = insp.Proyecto?.ProjectDescription ?? "",
            TipoId = insp.TipoId,
            TipoNombre = insp.Tipo?.Nombre ?? "",
            TipoAmbito = insp.Tipo?.Ambito ?? "",
            EmpresaId = insp.EmpresaId,
            EmpresaNombre = insp.Empresa?.ContributorNombreComercial,
            EmpresaInspectoraId = insp.EmpresaInspectoraId,
            EsPlanificada = insp.EsPlanificada,
            Fecha = insp.Fecha,
            HoraInicio = insp.HoraInicio?.ToString("HH:mm"),
            HoraFin = insp.HoraFin?.ToString("HH:mm"),
            Area = insp.Area,
            ResponsableArea = insp.ResponsableArea,
            InspectorNombre = insp.InspectorNombre,
            InspectorCargo = insp.InspectorCargo,
            InspectorEmpresa = insp.InspectorEmpresa,
            FirmaInspectorUrl = insp.FirmaInspectorUrl,
            RepresentanteNombre = insp.RepresentanteNombre,
            RepresentanteCargo = insp.RepresentanteCargo,
            FirmaRepresentanteUrl = insp.FirmaRepresentanteUrl,
            DescripcionCausas = insp.DescripcionCausas,
            Conclusiones = insp.Conclusiones,
            TotalItems = insp.TotalItems,
            TotalCumple = insp.TotalCumple,
            TotalNoCumple = insp.TotalNoCumple,
            TotalNa = insp.TotalNa,
            TasaCumplimiento = insp.TasaCumplimiento,
            Estado = insp.Estado,
            EsColaborativa = insp.EsColaborativa,
            CreatedAt = insp.CreatedAt,
            Respuestas = insp.Respuestas
                .OrderBy(r => r.Item?.Orden)
                .Select(r => new InspeccionRespuestaDto
                {
                    ItemId = r.ItemId,
                    Pregunta = r.Item?.Pregunta ?? "",
                    Categoria = r.Item?.Categoria,
                    Orden = r.Item?.Orden ?? 0,
                    Resultado = r.Resultado,
                    Observacion = r.Observacion
                }).ToList(),
            Hallazgos = insp.Hallazgos
                .Where(h => h.Estado != "Eliminado")
                .OrderByDescending(h => h.Tipo)
                .Select(h => new InspeccionHallazgoDto
                {
                    Id = h.Id,
                    Descripcion = h.Descripcion,
                    Tipo = h.Tipo,
                    Area = h.Area,
                    ResponsableNombre = h.ResponsableNombre,
                    ResponsableCargo = h.ResponsableCargo,
                    FechaLimite = h.FechaLimite,
                    Estado = h.Estado,
                    AccionCorrectiva = h.AccionCorrectiva,
                    EvidenciaCierreUrl = h.EvidenciaCierreUrl,
                    FechaCierre = h.FechaCierre,
                    Latitud = h.Latitud,
                    Longitud = h.Longitud,
                    CreadoPorNombre = h.CreadoPorNombre,
                    Fotos = h.Fotos.OrderBy(f => f.Orden)
                        .Select(f => new InspeccionHallazgoFotoDto
                        {
                            Id = f.Id,
                            Url = f.Url,
                            Descripcion = f.Descripcion,
                            Orden = f.Orden
                        }).ToList()
                }).ToList(),
            FotosArea = insp.FotosArea.OrderBy(f => f.Orden)
                .Select(f => new InspeccionHallazgoFotoDto { Id = f.Id, Url = f.Url, Orden = f.Orden })
                .ToList(),
            Participantes = insp.Participantes.OrderBy(p => p.FechaUnion)
                .Select(p => new ParticipanteDto { Id = p.Id, Nombre = p.Nombre, Cargo = p.Cargo, Empresa = p.Empresa, FechaUnion = p.FechaUnion })
                .ToList()
        };
    }

    public async Task<List<InspeccionListItemDto>> GetListAsync(int? proyectoId, int? tipoId,
        string? estado, DateTime? fechaDesde, DateTime? fechaHasta, int page, int pageSize,
        int? empresaIdContratista = null)
    {
        using var ctx = _factory.CreateDbContext();
        var q = ctx.SsomaInspeccion
            .Include(i => i.Proyecto)
            .Include(i => i.Tipo)
            .Include(i => i.Empresa)
            .Include(i => i.Hallazgos)
            .AsQueryable();

        if (proyectoId.HasValue) q = q.Where(i => i.ProyectoId == proyectoId.Value);
        if (tipoId.HasValue) q = q.Where(i => i.TipoId == tipoId.Value);
        if (!string.IsNullOrEmpty(estado)) q = q.Where(i => i.Estado == estado);
        if (fechaDesde.HasValue) q = q.Where(i => i.Fecha >= DateTime.SpecifyKind(fechaDesde.Value.Date, DateTimeKind.Utc));
        if (fechaHasta.HasValue) q = q.Where(i => i.Fecha <= DateTime.SpecifyKind(fechaHasta.Value.Date, DateTimeKind.Utc));
        if (empresaIdContratista.HasValue) q = q.Where(i => i.EmpresaId == empresaIdContratista.Value || i.EmpresaInspectoraId == empresaIdContratista.Value);

        return await q
            .OrderByDescending(i => i.Fecha)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new InspeccionListItemDto
            {
                Id = i.Id,
                ProyectoNombre = i.Proyecto != null ? i.Proyecto.ProjectDescription : "",
                TipoNombre = i.Tipo != null ? i.Tipo.Nombre : "",
                TipoAmbito = i.Tipo != null ? i.Tipo.Ambito : "",
                EmpresaNombre = i.Empresa != null ? i.Empresa.ContributorNombreComercial : null,
                EsPlanificada = i.EsPlanificada,
                Fecha = i.Fecha,
                Area = i.Area,
                InspectorNombre = i.InspectorNombre,
                TotalHallazgos = i.Hallazgos.Count(h => h.Estado != "Eliminado"),
                HallazgosCriticos = i.Hallazgos.Count(h => h.Tipo == "Critico" && h.Estado != "Eliminado"),
                HallazgosAbiertos = i.Hallazgos.Count(h => h.Estado == "Abierto"),
                TasaCumplimiento = i.TasaCumplimiento,
                Estado = i.Estado,
                CreatedAt = i.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<int> GetListCountAsync(int? proyectoId, int? tipoId,
        string? estado, DateTime? fechaDesde, DateTime? fechaHasta, int? empresaIdContratista = null)
    {
        using var ctx = _factory.CreateDbContext();
        var q = ctx.SsomaInspeccion.AsQueryable();
        if (proyectoId.HasValue) q = q.Where(i => i.ProyectoId == proyectoId.Value);
        if (tipoId.HasValue) q = q.Where(i => i.TipoId == tipoId.Value);
        if (!string.IsNullOrEmpty(estado)) q = q.Where(i => i.Estado == estado);
        if (fechaDesde.HasValue) q = q.Where(i => i.Fecha >= DateTime.SpecifyKind(fechaDesde.Value.Date, DateTimeKind.Utc));
        if (fechaHasta.HasValue) q = q.Where(i => i.Fecha <= DateTime.SpecifyKind(fechaHasta.Value.Date, DateTimeKind.Utc));
        if (empresaIdContratista.HasValue) q = q.Where(i => i.EmpresaId == empresaIdContratista.Value || i.EmpresaInspectoraId == empresaIdContratista.Value);
        return await q.CountAsync();
    }

    public async Task<InspeccionDashboardDto> GetDashboardAsync(int? proyectoId, int? anio, int? empresaIdContratista = null)
    {
        using var ctx = _factory.CreateDbContext();
        var anioFiltro = anio ?? DateTime.Now.Year;
        var mesActual = DateTime.Now.Month;

        var q = ctx.SsomaInspeccion.Include(i => i.Tipo).Include(i => i.Hallazgos).AsQueryable();
        if (proyectoId.HasValue) q = q.Where(i => i.ProyectoId == proyectoId.Value);
        if (empresaIdContratista.HasValue) q = q.Where(i => i.EmpresaId == empresaIdContratista.Value || i.EmpresaInspectoraId == empresaIdContratista.Value);

        var all = await q.ToListAsync();
        var delAnio = all.Where(i => i.Fecha.Year == anioFiltro).ToList();
        var delMes = delAnio.Where(i => i.Fecha.Month == mesActual).ToList();
        var todosHallazgos = all.SelectMany(i => i.Hallazgos).Where(h => h.Estado != "Eliminado").ToList();

        var tendencia = Enumerable.Range(1, 12).Select(m =>
        {
            var items = delAnio.Where(i => i.Fecha.Month == m).ToList();
            return new InspeccionTendenciaMensualDto
            {
                Anio = anioFiltro,
                Mes = m,
                MesNombre = new DateTime(anioFiltro, m, 1).ToString("MMM",
                    new System.Globalization.CultureInfo("es-PE")),
                Total = items.Count,
                TasaPromedio = items.Any(i => i.TasaCumplimiento.HasValue)
                    ? Math.Round(items.Where(i => i.TasaCumplimiento.HasValue)
                        .Average(i => i.TasaCumplimiento!.Value), 1)
                    : null
            };
        }).ToList();

        var porTipo = delAnio
            .GroupBy(i => new { i.TipoId, Nombre = i.Tipo?.Nombre ?? "", Ambito = i.Tipo?.Ambito ?? "" })
            .Select(g => new InspeccionPorTipoDto
            {
                TipoNombre = g.Key.Nombre,
                Ambito = g.Key.Ambito,
                Total = g.Count(),
                TasaPromedio = g.Any(i => i.TasaCumplimiento.HasValue)
                    ? Math.Round(g.Where(i => i.TasaCumplimiento.HasValue)
                        .Average(i => i.TasaCumplimiento!.Value), 1)
                    : null
            })
            .OrderByDescending(t => t.Total)
            .Take(10)
            .ToList();

        var hallazgosPorArea = todosHallazgos
            .Where(h => !string.IsNullOrEmpty(h.Area))
            .GroupBy(h => h.Area!)
            .Select(g => new InspeccionHallazgoPorAreaDto
            {
                Area = g.Key,
                Total = g.Count(),
                Criticos = g.Count(h => h.Tipo == "Critico"),
                Abiertos = g.Count(h => h.Estado == "Abierto")
            })
            .OrderByDescending(a => a.Criticos)
            .Take(10)
            .ToList();

        var recurrentes = todosHallazgos
            .GroupBy(h => h.Descripcion.ToLower().Trim())
            .Where(g => g.Count() > 1)
            .Select(g => new InspeccionHallazgoRecurrenteDto
            {
                Descripcion = g.First().Descripcion,
                Ocurrencias = g.Count(),
                UltimoTipo = g.OrderByDescending(h => h.CreatedAt).First().Tipo
            })
            .OrderByDescending(r => r.Ocurrencias)
            .Take(5)
            .ToList();

        return new InspeccionDashboardDto
        {
            TotalInspecciones = all.Count,
            TotalEsteMes = delMes.Count,
            HallazgosAbiertos = todosHallazgos.Count(h => h.Estado == "Abierto"),
            HallazgosCriticosAbiertos = todosHallazgos.Count(h => h.Tipo == "Critico" && h.Estado == "Abierto"),
            TasaCumplimientoPromedio = all.Any(i => i.TasaCumplimiento.HasValue)
                ? Math.Round(all.Where(i => i.TasaCumplimiento.HasValue)
                    .Average(i => i.TasaCumplimiento!.Value), 1)
                : null,
            TasaCumplimientoEsteMes = delMes.Any(i => i.TasaCumplimiento.HasValue)
                ? Math.Round(delMes.Where(i => i.TasaCumplimiento.HasValue)
                    .Average(i => i.TasaCumplimiento!.Value), 1)
                : null,
            TendenciaMensual = tendencia,
            PorTipo = porTipo,
            HallazgosPorArea = hallazgosPorArea,
            HallazgosRecurrentes = recurrentes
        };
    }

    public async Task<List<HallazgoListItemDto>> GetHallazgosAsync(
        string? estado, string? proyecto, string? area, DateTime? fechaLimiteHasta,
        int? empresaIdContratista = null)
    {
        using var ctx = _factory.CreateDbContext();

        var query = ctx.SsomaInspeccionHallazgo
            .Include(h => h.Inspeccion!).ThenInclude(i => i.Proyecto)
            .Include(h => h.Fotos)
            .AsNoTracking()
            .Where(h => h.Estado != "Eliminado")
            .AsQueryable();

        if (!string.IsNullOrEmpty(estado))
            query = query.Where(h => h.Estado == estado);
        if (!string.IsNullOrEmpty(area))
            query = query.Where(h => h.Area != null && h.Area.ToLower().Contains(area.ToLower()));
        if (fechaLimiteHasta.HasValue)
            query = query.Where(h => h.FechaLimite <= fechaLimiteHasta.Value);
        if (!string.IsNullOrEmpty(proyecto))
            query = query.Where(h => h.Inspeccion!.Proyecto != null &&
                h.Inspeccion!.Proyecto!.ProjectDescription.ToLower().Contains(proyecto.ToLower()));
        if (empresaIdContratista.HasValue)
            query = query.Where(h => h.Inspeccion!.EmpresaId == empresaIdContratista.Value
                || h.Inspeccion!.EmpresaInspectoraId == empresaIdContratista.Value);

        var lista = await query
            .Select(h => new HallazgoListItemDto
            {
                Id = h.Id,
                InspeccionId = h.InspeccionId,
                Proyecto = h.Inspeccion!.Proyecto != null ? h.Inspeccion.Proyecto.ProjectDescription : null,
                FechaInspeccion = h.Inspeccion!.Fecha,
                Descripcion = h.Descripcion,
                Tipo = h.Tipo,
                Area = h.Area,
                ResponsableNombre = h.ResponsableNombre,
                ResponsableCargo = h.ResponsableCargo,
                FechaLimite = h.FechaLimite,
                AccionCorrectiva = h.AccionCorrectiva,
                Estado = h.Estado,
                FechaCierre = h.FechaCierre,
                FotosUrls = h.Fotos.OrderBy(f => f.Orden).Select(f => f.Url).ToList()
            })
            .ToListAsync();

        var ahora = DateTime.UtcNow;
        return lista
            .OrderBy(h => h.Estado == "Abierto" && h.FechaLimite < ahora ? 0 :
                          h.Estado == "Abierto" ? 1 :
                          h.Estado == "En proceso" ? 2 : 3)
            .ThenBy(h => h.FechaLimite)
            .ToList();
    }

    public async Task<int> AgregarHallazgoAsync(int inspeccionId, InspeccionHallazgoRequest h, int? creadoPorWorkerId, string? creadoPorNombre)
    {
        using var ctx = _factory.CreateDbContext();
        var insp = await ctx.SsomaInspeccion.FindAsync(inspeccionId)
            ?? throw new AbrilException("Inspección no encontrada.", 404);
        if (!insp.EsColaborativa || insp.Estado != "Abierta")
            throw new AbrilException("Esta inspección no está abierta para agregar hallazgos.", 400);

        var hallazgo = new SsomaInspeccionHallazgo
        {
            InspeccionId = inspeccionId,
            Descripcion = h.Descripcion,
            Tipo = h.Tipo,
            Area = h.Area,
            ResponsableNombre = h.ResponsableNombre,
            ResponsableCargo = h.ResponsableCargo,
            FechaLimite = h.FechaLimite.HasValue ? DateTime.SpecifyKind(h.FechaLimite.Value, DateTimeKind.Utc) : null,
            AccionCorrectiva = h.AccionCorrectiva,
            Latitud = h.Latitud,
            Longitud = h.Longitud,
            CreadoPorWorkerId = creadoPorWorkerId,
            CreadoPorNombre = creadoPorNombre,
            Estado = "Abierto",
            CreatedAt = DateTime.UtcNow
        };
        ctx.SsomaInspeccionHallazgo.Add(hallazgo);
        await ctx.SaveChangesAsync();
        return hallazgo.Id;
    }

    public async Task AgregarFotosHallazgoAsync(int hallazgoId, List<string> urls)
    {
        using var ctx = _factory.CreateDbContext();
        for (int j = 0; j < urls.Count; j++)
        {
            ctx.SsomaInspeccionHallazgoFoto.Add(new SsomaInspeccionHallazgoFoto
            {
                HallazgoId = hallazgoId,
                Url = urls[j],
                Orden = j,
                CreatedAt = DateTime.UtcNow
            });
        }
        await ctx.SaveChangesAsync();
    }

    public async Task UnirseAsync(int inspeccionId, UnirseInspeccionRequest req, int? workerId)
    {
        using var ctx = _factory.CreateDbContext();
        var insp = await ctx.SsomaInspeccion.FindAsync(inspeccionId)
            ?? throw new AbrilException("Inspección no encontrada.", 404);
        if (!insp.EsColaborativa || insp.Estado != "Abierta")
            throw new AbrilException("Esta inspección no está abierta.", 400);

        var existentes = await ctx.SsomaInspeccionParticipante
            .Where(p => p.InspeccionId == inspeccionId)
            .ToListAsync();

        // El creador queda registrado como participante sin WorkerId (se resuelve solo por nombre
        // al crear). Si es la misma persona la que luego "se une" — p.ej. desde su propio detalle
        // al ir a agregar un hallazgo —, hay que reconocerla por nombre para no duplicarla.
        var match = (workerId.HasValue ? existentes.FirstOrDefault(p => p.WorkerId == workerId.Value) : null)
            ?? existentes.FirstOrDefault(p =>
                p.WorkerId == null && p.Nombre.Trim().Equals(req.Nombre.Trim(), StringComparison.OrdinalIgnoreCase));

        if (match != null)
        {
            if (workerId.HasValue && match.WorkerId == null)
            {
                match.WorkerId = workerId;
                await ctx.SaveChangesAsync();
            }
            return;
        }

        ctx.SsomaInspeccionParticipante.Add(new SsomaInspeccionParticipante
        {
            InspeccionId = inspeccionId,
            WorkerId = workerId,
            Nombre = req.Nombre,
            Cargo = req.Cargo,
            Empresa = req.Empresa,
            FechaUnion = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();
    }

    public async Task<List<InspeccionAbiertaListItemDto>> GetAbiertasAsync(int? proyectoId)
    {
        using var ctx = _factory.CreateDbContext();
        var q = ctx.SsomaInspeccion
            .Include(i => i.Proyecto).Include(i => i.Tipo).Include(i => i.Hallazgos).Include(i => i.Participantes)
            .Where(i => i.EsColaborativa && i.Estado == "Abierta")
            .AsQueryable();
        if (proyectoId.HasValue) q = q.Where(i => i.ProyectoId == proyectoId.Value);

        return await q
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new InspeccionAbiertaListItemDto
            {
                Id = i.Id,
                ProyectoNombre = i.Proyecto != null ? i.Proyecto.ProjectDescription : "",
                TipoNombre = i.Tipo != null ? i.Tipo.Nombre : "",
                Fecha = i.Fecha,
                TotalHallazgos = i.Hallazgos.Count(h => h.Estado != "Eliminado"),
                TotalParticipantes = i.Participantes.Count,
                CreatedAt = i.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<int> GetProyectoIdAsync(int inspeccionId)
    {
        using var ctx = _factory.CreateDbContext();
        var insp = await ctx.SsomaInspeccion.FindAsync(inspeccionId)
            ?? throw new AbrilException("Inspección no encontrada.", 404);
        return insp.ProyectoId;
    }

    public async Task CerrarInspeccionColaborativaAsync(int inspeccionId, int? userId)
    {
        using var ctx = _factory.CreateDbContext();
        var insp = await ctx.SsomaInspeccion.Include(i => i.Hallazgos)
            .FirstOrDefaultAsync(i => i.Id == inspeccionId)
            ?? throw new AbrilException("Inspección no encontrada.", 404);
        if (!insp.EsColaborativa) throw new AbrilException("Esta inspección no es colaborativa.", 400);
        if (insp.Estado != "Abierta") throw new AbrilException("La inspección ya está cerrada.", 400);

        insp.Estado = insp.Hallazgos.Any(h => h.Estado != "Eliminado") ? "En Proceso" : "Cerrada";
        await ctx.SaveChangesAsync();

        await EnviarNotificacionCierreColaborativaAsync(ctx, insp, userId);
    }

    public async Task<InspeccionDestinatariosCierreDto> GetDestinatariosCierreColaborativaAsync(int inspeccionId, int? userId)
    {
        using var ctx = _factory.CreateDbContext();
        var insp = await ctx.SsomaInspeccion.FindAsync(inspeccionId)
            ?? throw new AbrilException("Inspección no encontrada.", 404);

        return await ResolverDestinatariosCierreAsync(ctx, insp, userId);
    }

    /// <summary>
    /// Resuelve a quién le llega el correo de cierre de una inspección colaborativa (gerencial/
    /// cruzada): residente del proyecto, coordinador SSOMA del proyecto (ambos ya configurables
    /// en Configuración → Proyectos → Emails SSOMA) y quien ocupe el puesto "Gerente
    /// Inmobiliario" — hoy es uno solo en toda la empresa, se resuelve por nombre de puesto y no
    /// por un campo nuevo por proyecto. Más quien cierra la inspección, en copia. Usado tanto
    /// para el envío real como para la vista previa que confirma el usuario antes de cerrar —
    /// mismo criterio en los dos lados, para que la vista previa nunca mienta.
    /// </summary>
    private async Task<InspeccionDestinatariosCierreDto> ResolverDestinatariosCierreAsync(AppDbContext ctx, SsomaInspeccion insp, int? userId)
    {
        var dto = new InspeccionDestinatariosCierreDto();

        var proyecto = await ctx.Project.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProjectId == insp.ProyectoId);
        if (proyecto == null) return dto;

        if (proyecto.ResidenteWorkersId.HasValue)
        {
            dto.ResidenteEmail = await ctx.Worker.AsNoTracking()
                .Where(w => w.Id == proyecto.ResidenteWorkersId.Value)
                .Select(w => w.EmailCorporativo)
                .FirstOrDefaultAsync();
        }

        dto.CoordSsomaEmail = proyecto.EmailCoordSsoma;

        dto.GerenteInmobiliarioEmail = await ctx.Worker.AsNoTracking()
            .Where(w => w.PuestoCatalogo != null && w.PuestoCatalogo.Nombre.ToUpper() == "GERENTE INMOBILIARIO"
                     && w.Estado == "ACTIVO")
            .Select(w => w.EmailCorporativo)
            .FirstOrDefaultAsync();

        dto.JefeSsomaEmail = await ctx.Worker.AsNoTracking()
            .Where(w => w.PuestoCatalogo != null && w.PuestoCatalogo.Nombre.ToUpper() == "JEFE DE SEGURIDAD Y SALUD EN EL TRABAJO"
                     && w.Estado == "ACTIVO")
            .Select(w => w.EmailCorporativo)
            .FirstOrDefaultAsync();

        // Quienes hicieron la inspección: participantes con WorkerId resuelto a correo
        // corporativo, MÁS el inspector original (insp.InspectorWorkerId) — el creador de la
        // inspección queda registrado como participante SIN WorkerId (se resuelve solo por
        // nombre en InspeccionRepository.UnirseAsync/CrearInspeccionAsync), así que si no se
        // trata aparte el inspector nunca aparece acá aunque sea quien más participó.
        var participantesConWorker = await ctx.SsomaInspeccionParticipante
            .Where(p => p.InspeccionId == insp.Id && p.WorkerId.HasValue)
            .Select(p => new { p.Nombre, WorkerId = p.WorkerId!.Value })
            .ToListAsync();
        if (insp.InspectorWorkerId.HasValue && !participantesConWorker.Any(p => p.WorkerId == insp.InspectorWorkerId.Value))
            participantesConWorker.Add(new { Nombre = insp.InspectorNombre ?? "", WorkerId = insp.InspectorWorkerId.Value });
        var workerIdsParticipantes = participantesConWorker.Select(p => p.WorkerId).Distinct().ToList();
        var emailsPorWorker = await ctx.Worker.AsNoTracking()
            .Where(w => workerIdsParticipantes.Contains(w.Id))
            .Select(w => new { w.Id, w.EmailCorporativo })
            .ToDictionaryAsync(w => w.Id, w => w.EmailCorporativo);
        dto.Participantes = participantesConWorker
            .Where(p => emailsPorWorker.TryGetValue(p.WorkerId, out var email) && !string.IsNullOrWhiteSpace(email))
            .Select(p => new InspeccionDestinatarioDto { Nombre = p.Nombre, Email = emailsPorWorker[p.WorkerId]! })
            .ToList();

        // Prevencionistas (rol 72, ver Roles.Prevencionista): uno por contratista con
        // vinculación activa al proyecto de la inspección. Mismo criterio de resolución que
        // EvPrevencionistaRepository.GetInicioAsync (evaluaciones de desempeño), que ya matchea
        // por correo entre workers y app_user en vez de por person.user_id, porque no todos los
        // prevencionistas de contratista tienen ese campo poblado.
        await ctx.Database.OpenConnectionAsync();
        var conn = ctx.Database.GetDbConnection();
        var prevencionistas = await conn.QueryAsync<(string Nombre, string Email)>(
            @"SELECT DISTINCT p.full_name AS Nombre, au.email AS Email
              FROM workers w
              JOIN person p ON p.person_id = w.person_id
              JOIN app_user au ON LOWER(au.email) = LOWER(w.email_corporativo)
              JOIN user_role ur ON ur.user_id = au.user_id AND ur.role_id = 72 AND ur.active = TRUE AND ur.state = TRUE
              JOIN worker_vinculaciones wv ON wv.worker_id = w.id AND wv.fecha_fin IS NULL
              WHERE w.state AND wv.proyecto_id = @ProyectoId AND au.email IS NOT NULL",
            new { ProyectoId = insp.ProyectoId });
        dto.Prevencionistas = prevencionistas
            .Select(p => new InspeccionDestinatarioDto { Nombre = p.Nombre, Email = p.Email })
            .ToList();

        if (userId.HasValue)
        {
            dto.TuEmail = await ctx.User.AsNoTracking()
                .Where(u => u.UserId == userId.Value)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();
        }

        return dto;
    }

    /// <summary>
    /// Envía el correo real de cierre, con el mismo destinatarios que ya vio el usuario en la
    /// vista previa (<see cref="ResolverDestinatariosCierreAsync"/>).
    /// </summary>
    private async Task EnviarNotificacionCierreColaborativaAsync(AppDbContext ctx, SsomaInspeccion insp, int? userId)
    {
        try
        {
            var destinatarios = await ResolverDestinatariosCierreAsync(ctx, insp, userId);

            var to = new List<string?> { destinatarios.ResidenteEmail, destinatarios.CoordSsomaEmail, destinatarios.GerenteInmobiliarioEmail }
                .Concat(destinatarios.Prevencionistas.Select(p => (string?)p.Email))
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e!)
                .Distinct()
                .ToList();
            if (to.Count == 0) return;

            var cc = new List<string?> { destinatarios.TuEmail, destinatarios.JefeSsomaEmail }
                .Concat(destinatarios.Participantes.Select(p => (string?)p.Email))
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e!)
                .Distinct()
                .ToList();

            var proyecto = await ctx.Project.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProjectId == insp.ProyectoId);
            if (proyecto == null) return;

            var tipoNombre = await ctx.SsomaInspeccionTipo.AsNoTracking()
                .Where(t => t.Id == insp.TipoId)
                .Select(t => t.Nombre)
                .FirstOrDefaultAsync();

            var hallazgosVigentes = insp.Hallazgos.Where(h => h.Estado != "Eliminado").ToList();
            var hallazgosAbiertos = hallazgosVigentes.Count(h => h.Estado != "Cerrado");
            var fechaStr = insp.Fecha.ToString("dd/MM/yyyy");

            var html = $@"<h2>Inspección {(insp.Estado == "Cerrada" ? "cerrada" : "cerrada — con hallazgos pendientes")}</h2>
<p>Se cerró la inspección colaborativa del proyecto <strong>{proyecto.ProjectDescription}</strong>:</p>
<table style='border-collapse:collapse;font-family:Arial,sans-serif;font-size:14px;'>
<tr><td style='padding:6px 12px;font-weight:600;background:#f9fafb'>Proyecto</td><td style='padding:6px 12px'>{proyecto.ProjectDescription}</td></tr>
<tr><td style='padding:6px 12px;font-weight:600;background:#f9fafb'>Tipo de inspección</td><td style='padding:6px 12px'>{tipoNombre ?? "—"}</td></tr>
<tr><td style='padding:6px 12px;font-weight:600;background:#f9fafb'>Área</td><td style='padding:6px 12px'>{insp.Area ?? "—"}</td></tr>
<tr><td style='padding:6px 12px;font-weight:600;background:#f9fafb'>Fecha</td><td style='padding:6px 12px'>{fechaStr}</td></tr>
<tr><td style='padding:6px 12px;font-weight:600;background:#f9fafb'>Estado</td><td style='padding:6px 12px'>{insp.Estado}</td></tr>
<tr><td style='padding:6px 12px;font-weight:600;background:#f9fafb'>Hallazgos totales</td><td style='padding:6px 12px'>{hallazgosVigentes.Count}</td></tr>
<tr><td style='padding:6px 12px;font-weight:600;background:#f9fafb'>Hallazgos pendientes de cierre</td><td style='padding:6px 12px'>{hallazgosAbiertos}</td></tr>
</table>
<p style='font-size:12px;color:#666;margin-top:24px;'>Esta notificación se generó automáticamente por el sistema Abril.</p>";

            List<EmailAttachment>? attachments = null;
            try
            {
                var detalle = await GetDetalleAsync(insp.Id);
                if (detalle != null)
                {
                    var pdfBytes = await _pdfService.GenerarPdfAsync(detalle);
                    attachments = [new EmailAttachment
                    {
                        FileName = $"Inspeccion_{insp.Id}_{fechaStr.Replace("/", "-")}.pdf",
                        ContentType = "application/pdf",
                        Content = pdfBytes,
                    }];
                }
            }
            catch (Exception exPdf)
            {
                // El correo de cierre igual debe salir aunque el PDF falle (ej. una foto
                // no descargable desde SharePoint) — no lo dejamos sin adjunto en silencio,
                // queda en el log para investigar.
                _logger.LogWarning(exPdf, "No se pudo generar el PDF adjunto para el cierre de inspección {InspeccionId}.", insp.Id);
            }

            await _emailService.SendAsync(
                to: to,
                subject: $"[Inspección Colaborativa Cerrada] {proyecto.ProjectDescription} — {fechaStr}",
                body: html,
                isHtml: true,
                cc: cc,
                attachments: attachments);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo enviar la notificación de cierre de inspección colaborativa {InspeccionId}.", insp.Id);
        }
    }

    public async Task ReabrirInspeccionColaborativaAsync(int inspeccionId)
    {
        using var ctx = _factory.CreateDbContext();
        var insp = await ctx.SsomaInspeccion.FindAsync(inspeccionId)
            ?? throw new AbrilException("Inspección no encontrada.", 404);
        if (!insp.EsColaborativa) throw new AbrilException("Esta inspección no es colaborativa.", 400);
        if (insp.Estado == "Abierta") throw new AbrilException("La inspección ya está abierta.", 400);

        insp.Estado = "Abierta";
        await ctx.SaveChangesAsync();
    }

    public async Task LevantarHallazgoAsync(int hallazgoId, LevantarHallazgoDto dto)
    {
        using var ctx = _factory.CreateDbContext();
        var hallazgo = await ctx.SsomaInspeccionHallazgo.FindAsync(hallazgoId)
            ?? throw new AbrilException("Hallazgo no encontrado.", 404);

        hallazgo.Estado = dto.Estado;
        if (dto.Estado == "Cerrado")
            hallazgo.FechaCierre = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(dto.EvidenciaUrl))
            hallazgo.EvidenciaCierreUrl = dto.EvidenciaUrl;

        await ctx.SaveChangesAsync();
    }
}

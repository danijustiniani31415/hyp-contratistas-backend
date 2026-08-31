using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Dossier;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Repositories;

public class DossierRepository : IDossierRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    private static readonly string[] TiposDoc =
        ["Accidente", "EPP", "Estadisticas", "Capacitaciones", "PETAR", "ATS", "Charlas"];

    public DossierRepository(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<DossierSemanaDto>> GetSemanasAsync(int? contributorId, int? proyectoId, int? anio)
    {
        using var ctx = _factory.CreateDbContext();
        var query = ctx.SsDossierSemana.Include(s => s.Documentos).AsQueryable();
        if (contributorId.HasValue) query = query.Where(s => s.ContributorId == contributorId.Value);
        if (proyectoId.HasValue) query = query.Where(s => s.ProyectoId == proyectoId.Value);
        if (anio.HasValue) query = query.Where(s => s.Anio == anio.Value);

        var contributores = await ctx.Contributor.ToDictionaryAsync(c => c.ContributorId, c => c.ContributorName);
        var proyectos = await ctx.Project.ToDictionaryAsync(p => p.ProjectId, p => p.ProjectDescription);

        var semanas = await query
            .OrderByDescending(s => s.Anio)
            .ThenByDescending(s => s.NumeroSemana)
            .Select(s => new
            {
                s.Id, s.ContributorId, s.ProyectoId,
                s.Anio, s.NumeroSemana, s.FechaInicio, s.FechaFin,
                s.Estado, s.ObsRevisor, s.CreatedAt,
                TotalDocs = s.Documentos.Count,
                DocsSubidos = s.Documentos.Count(d => d.Estado == "Subido"),
                DocsNa = s.Documentos.Count(d => d.Estado == "NA"),
                DocsAprobados = s.Documentos.Count(d => d.Estado == "Aprobado"),
            })
            .ToListAsync();

        return semanas.Select(s => new DossierSemanaDto(
            s.Id, s.ContributorId, contributores.GetValueOrDefault(s.ContributorId),
            s.ProyectoId, proyectos.GetValueOrDefault(s.ProyectoId),
            s.Anio, s.NumeroSemana, s.FechaInicio, s.FechaFin,
            s.Estado, s.ObsRevisor, s.CreatedAt,
            s.TotalDocs, s.DocsSubidos, s.DocsNa, s.DocsAprobados)).ToList();
    }

    public async Task<DossierSemanaDetalleDto?> GetDetalleAsync(int id)
    {
        using var ctx = _factory.CreateDbContext();
        var s = await ctx.SsDossierSemana
            .Include(s => s.Documentos)
            .ThenInclude(d => d.Archivos)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (s == null) return null;

        var docs = s.Documentos
            .OrderBy(d => Array.IndexOf(TiposDoc, d.TipoDoc))
            .Select(d => new DossierDocumentoDto(
                d.Id, d.DossierId, d.TipoDoc,
                d.NombreArchivo, d.ArchivoPath,
                d.Estado, d.ObsRevisor, d.CreatedAt, d.UpdatedAt,
                d.Archivos.OrderBy(a => a.CreatedAt)
                    .Select(a => new DossierArchivoDto(a.Id, a.NombreArchivo, a.ArchivoPath, a.CreatedAt))
                    .ToList()))
            .ToList();

        return new DossierSemanaDetalleDto(
            s.Id, s.ContributorId, s.ProyectoId,
            s.Anio, s.NumeroSemana, s.FechaInicio, s.FechaFin,
            s.Estado, s.ObsRevisor, s.CreatedAt,
            s.Documentos.Count,
            s.Documentos.Count(d => d.Estado == "Subido"),
            s.Documentos.Count(d => d.Estado == "NA"),
            s.Documentos.Count(d => d.Estado == "Aprobado"),
            docs);
    }

    public async Task<(int Id, DateTime FechaInicio, DateTime FechaFin)> EnsureSemanaAsync(EnsureSemanaRequest req)
    {
        var fechaInicio = DateTime.SpecifyKind(
            ISOWeek.ToDateTime(req.Anio, req.NumeroSemana, DayOfWeek.Monday),
            DateTimeKind.Utc);
        var fechaFin = fechaInicio.AddDays(6);
        var ahora = DateTime.UtcNow;

        using var ctx = _factory.CreateDbContext();

        await ctx.Database.ExecuteSqlRawAsync(
            @"INSERT INTO ss_dossier_semana
                (contributor_id, proyecto_id, anio, numero_semana, fecha_inicio, fecha_fin, estado, created_at, updated_at)
              VALUES ({0}, {1}, {2}, {3}, {4}, {5}, 'Borrador', {6}, {7})
              ON CONFLICT (contributor_id, proyecto_id, anio, numero_semana) DO NOTHING",
            req.ContributorId, req.ProyectoId, req.Anio, req.NumeroSemana,
            fechaInicio, fechaFin, ahora, ahora);

        var semana = await ctx.SsDossierSemana
            .FirstAsync(s => s.ContributorId == req.ContributorId
                          && s.ProyectoId == req.ProyectoId
                          && s.Anio == req.Anio
                          && s.NumeroSemana == req.NumeroSemana);

        foreach (var tipo in TiposDoc)
        {
            await ctx.Database.ExecuteSqlRawAsync(
                @"INSERT INTO ss_dossier_documento (dossier_id, tipo_doc, estado, created_at, updated_at)
                  VALUES ({0}, {1}, 'Pendiente', {2}, {3})
                  ON CONFLICT (dossier_id, tipo_doc) DO NOTHING",
                semana.Id, tipo, ahora, ahora);
        }

        return (semana.Id, semana.FechaInicio, semana.FechaFin);
    }

    public async Task<(int ContributorId, int ProyectoId, int NumeroSemana, DateTime FechaInicio)?> GetDossierContextoAsync(int dossierId)
    {
        using var ctx = _factory.CreateDbContext();
        var s = await ctx.SsDossierSemana
            .Where(s => s.Id == dossierId)
            .Select(s => new { s.ContributorId, s.ProyectoId, s.NumeroSemana, s.FechaInicio })
            .FirstOrDefaultAsync();
        if (s == null) return null;
        return (s.ContributorId, s.ProyectoId, s.NumeroSemana, s.FechaInicio);
    }

    public async Task SubirDocumentoAsync(int dossierId, string tipoDoc, string nombreArchivo, string archivoPath)
    {
        using var ctx = _factory.CreateDbContext();
        var doc = await ctx.SsDossierDocumento
            .FirstOrDefaultAsync(d => d.DossierId == dossierId && d.TipoDoc == tipoDoc)
            ?? throw new AbrilException("Documento no encontrado.", 404);

        ctx.SsDossierDocumentoArchivo.Add(new SsDossierDocumentoArchivo {
            DocumentoId = doc.Id,
            NombreArchivo = nombreArchivo,
            ArchivoPath = archivoPath,
            CreatedAt = DateTime.UtcNow
        });
        doc.Estado = "Subido";
        doc.ObsRevisor = null;
        doc.ArchivoPath = archivoPath;
        doc.NombreArchivo = nombreArchivo;
        doc.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    public async Task MarcarNaAsync(int docId)
    {
        using var ctx = _factory.CreateDbContext();
        var doc = await ctx.SsDossierDocumento
            .Include(d => d.Archivos)
            .Include(d => d.Dossier)
            .ThenInclude(s => s!.Documentos)
            .FirstOrDefaultAsync(d => d.Id == docId)
            ?? throw new AbrilException("Documento no encontrado.", 404);

        if (doc.Estado != "NA" && (doc.Estado == "Subido" || doc.Archivos.Count > 0))
            throw new AbrilException(
                "No se puede marcar como No Aplica un documento que ya tiene un archivo subido.", 400);

        var ahora = DateTime.UtcNow;
        doc.Estado = doc.Estado == "NA" ? "Pendiente" : "NA";
        doc.NombreArchivo = null;
        doc.ArchivoPath = null;
        doc.UpdatedAt = ahora;

        // Marcar NA puede ser el último cambio que le faltaba a la semana para poder
        // aprobarse (o para no depender más de un documento que quedó observado). El
        // único recálculo vivía en RevisarDocumentoAsync, así que si el documento NA-eado
        // era el último pendiente de revisión, la semana se quedaba congelada en "Enviado"
        // porque ya no quedaba ningún documento "Subido" que disparara el recálculo.
        var semana = doc.Dossier!;
        if (semana.Estado is "Enviado" or "Observado" or "Aprobado")
        {
            RecalcularEstadoSemana(semana);
            semana.UpdatedAt = ahora;
        }

        await ctx.SaveChangesAsync();
    }

    private static void RecalcularEstadoSemana(SsDossierSemana semana)
    {
        var aplicables = semana.Documentos.Where(d => d.Estado != "NA").ToList();
        if (aplicables.Count > 0 && aplicables.All(d => d.Estado == "Aprobado"))
            semana.Estado = "Aprobado";
        else if (aplicables.Any(d => d.Estado == "Observado"))
            semana.Estado = "Observado";
        else
            semana.Estado = "Enviado";
    }

    public async Task EnviarAsync(int dossierId)
    {
        using var ctx = _factory.CreateDbContext();
        var semana = await ctx.SsDossierSemana.FindAsync(dossierId)
            ?? throw new AbrilException("Dossier no encontrado.", 404);
        semana.Estado = "Enviado";
        semana.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    public async Task RevisarAsync(int dossierId, RevisarDossierRequest req)
    {
        using var ctx = _factory.CreateDbContext();
        var semana = await ctx.SsDossierSemana.FindAsync(dossierId)
            ?? throw new AbrilException("Dossier no encontrado.", 404);
        semana.Estado = req.Estado;
        semana.ObsRevisor = req.ObsRevisor;
        semana.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    public async Task RevisarDocumentoAsync(int docId, RevisarDocumentoRequest req)
    {
        using var ctx = _factory.CreateDbContext();
        var doc = await ctx.SsDossierDocumento.FindAsync(docId)
            ?? throw new AbrilException("Documento no encontrado.", 404);
        var semana = await ctx.SsDossierSemana
            .Include(s => s.Documentos)
            .FirstOrDefaultAsync(s => s.Id == doc.DossierId)
            ?? throw new AbrilException("Dossier no encontrado.", 404);

        // Un documento solo se revisa cuando la semana ya está en revisión. Sin este
        // candado se podían aprobar documentos de una semana en Borrador, y el recálculo
        // de abajo la empujaba a "Enviado" sin que el contratista la hubiera enviado:
        // quedaba congelada, porque EnviarAsync exige Borrador/Rechazado y subir un
        // archivo solo revierte a Borrador desde Aprobado/Observado, no desde Enviado.
        if (semana.Estado is not ("Enviado" or "Observado" or "Aprobado"))
            throw new AbrilException(
                "El contratista todavía no envía esta semana para revisión, así que aún no se pueden revisar sus documentos.",
                400);

        var ahora = DateTime.UtcNow;
        doc.Estado = req.Estado;
        doc.ObsRevisor = req.ObsRevisor;
        doc.UpdatedAt = ahora;

        RecalcularEstadoSemana(semana);
        semana.UpdatedAt = ahora;

        await ctx.SaveChangesAsync();
    }

    public async Task MarcarSemanaNoAplicaAsync(int dossierId)
    {
        using var ctx = _factory.CreateDbContext();
        var semana = await ctx.SsDossierSemana.FindAsync(dossierId)
            ?? throw new AbrilException("Dossier no encontrado.", 404);
        semana.Estado = semana.Estado == "NoAplica" ? "Borrador" : "NoAplica";
        semana.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    public async Task<string?> GetArchivoPathAsync(int docId)
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsDossierDocumento
            .Where(d => d.Id == docId)
            .Select(d => d.ArchivoPath)
            .FirstOrDefaultAsync();
    }

    public async Task EliminarArchivoAsync(int archivoId)
    {
        using var ctx = _factory.CreateDbContext();
        var archivo = await ctx.SsDossierDocumentoArchivo
            .Include(a => a.Documento)
            .ThenInclude(d => d.Dossier)
            .FirstOrDefaultAsync(a => a.Id == archivoId)
            ?? throw new AbrilException("Archivo no encontrado.", 404);

        if (archivo.Documento.Dossier!.Estado == "Aprobado")
            throw new AbrilException("No se puede eliminar archivos de un dossier aprobado.", 400);

        ctx.SsDossierDocumentoArchivo.Remove(archivo);

        var quedan = await ctx.SsDossierDocumentoArchivo
            .CountAsync(a => a.DocumentoId == archivo.DocumentoId && a.Id != archivoId);
        if (quedan == 0)
        {
            var ahora = DateTime.UtcNow;
            archivo.Documento.Estado = "Pendiente";
            archivo.Documento.UpdatedAt = ahora;

            // El documento vuelve a quedar pendiente, así que la semana ya no cumple la
            // condición de envío. Si estaba esperando o en revisión hay que devolverla a
            // Borrador: quedarse en "Enviado" con un pendiente la congela, porque
            // EnviarAsync exige Borrador/Rechazado y nadie puede aprobar lo que no existe.
            var semana = archivo.Documento.Dossier!;
            if (semana.Estado is "Enviado" or "Observado")
            {
                semana.Estado = "Borrador";
                semana.UpdatedAt = ahora;
            }
        }
        await ctx.SaveChangesAsync();
    }

    public async Task RevertirABorradorAsync(int dossierId)
    {
        using var ctx = _factory.CreateDbContext();
        var semana = await ctx.SsDossierSemana.FindAsync(dossierId)
            ?? throw new AbrilException("Dossier no encontrado.", 404);
        semana.Estado = "Borrador";
        semana.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    public async Task<string?> GetArchivoPathByIdAsync(int archivoId)
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsDossierDocumentoArchivo
            .Where(a => a.Id == archivoId)
            .Select(a => a.ArchivoPath)
            .FirstOrDefaultAsync();
    }

    public async Task<int?> GetContributorIdDeDocumentoAsync(int docId)
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsDossierDocumento
            .Where(d => d.Id == docId)
            .Select(d => (int?)d.Dossier!.ContributorId)
            .FirstOrDefaultAsync();
    }

    public async Task<int?> GetContributorIdDeArchivoAsync(int archivoId)
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.SsDossierDocumentoArchivo
            .Where(a => a.Id == archivoId)
            .Select(a => (int?)a.Documento!.Dossier!.ContributorId)
            .FirstOrDefaultAsync();
    }
}

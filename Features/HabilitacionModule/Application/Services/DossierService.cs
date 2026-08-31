using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Dossier;
using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Abril_Backend.Features.Habilitacion.Application.Services;

public class DossierService : IDossierService
{
    private readonly IDossierRepository _repo;
    private readonly ISharePointHabService _sharePoint;

    public DossierService(IDossierRepository repo, ISharePointHabService sharePoint)
    {
        _repo = repo;
        _sharePoint = sharePoint;
    }

    public Task<List<DossierSemanaDto>> GetSemanasAsync(int? contributorId, int? proyectoId, int? anio)
        => _repo.GetSemanasAsync(contributorId, proyectoId, anio);

    public async Task<DossierSemanaDetalleDto> GetDetalleAsync(int id)
    {
        var result = await _repo.GetDetalleAsync(id);
        return result ?? throw new AbrilException("Dossier no encontrado.", 404);
    }

    public async Task<object> EnsureSemanaAsync(EnsureSemanaRequest req)
    {
        if (req.NumeroSemana < 1 || req.NumeroSemana > 53)
            throw new AbrilException("Número de semana inválido.", 400);
        if (req.Anio < 2020 || req.Anio > 2100)
            throw new AbrilException("Año inválido.", 400);

        var (id, fechaInicio, fechaFin) = await _repo.EnsureSemanaAsync(req);
        return new { id, fechaInicio, fechaFin };
    }

    public async Task SubirDocumentoAsync(int dossierId, string tipoDoc, IFormFile file)
    {
        if (file is null || file.Length == 0)
            throw new AbrilException("No se recibió ningún archivo.", 400);

        var ctx = await _repo.GetDossierContextoAsync(dossierId)
            ?? throw new AbrilException("Dossier no encontrado.", 404);

        var carpetaPath = $"{ctx.ContributorId}/{ctx.ProyectoId}/Sem{ctx.NumeroSemana}_{ctx.FechaInicio:yyyyMMdd}";
        var fileName = $"{tipoDoc}_{file.FileName}";

        using var stream = file.OpenReadStream();
        var path = await _sharePoint.SubirArchivoEnRutaAsync(stream, fileName, "dossier-semanal", carpetaPath);

        await _repo.SubirDocumentoAsync(dossierId, tipoDoc, file.FileName, path);

        var detalle = await _repo.GetDetalleAsync(dossierId);
        if (detalle?.Estado is "Aprobado" or "Observado")
            await _repo.RevertirABorradorAsync(dossierId);
    }

    public Task MarcarNaAsync(int docId) => _repo.MarcarNaAsync(docId);

    public Task MarcarSemanaNoAplicaAsync(int dossierId) => _repo.MarcarSemanaNoAplicaAsync(dossierId);

    public Task EliminarArchivoAsync(int archivoId) => _repo.EliminarArchivoAsync(archivoId);

    public async Task<string> GetArchivoUrlAsync(int archivoId)
    {
        var path = await _repo.GetArchivoPathByIdAsync(archivoId)
            ?? throw new AbrilException("Archivo no encontrado.", 404);
        var url = await _sharePoint.GetDownloadUrlAsync(path, "dossier-semanal");
        if (string.IsNullOrWhiteSpace(url))
            throw new AbrilException("No se pudo obtener la URL del archivo.", 502);
        return url;
    }

    public async Task EnviarAsync(int dossierId)
    {
        var detalle = await _repo.GetDetalleAsync(dossierId)
            ?? throw new AbrilException("Dossier no encontrado.", 404);
        if (detalle.Estado != "Borrador" && detalle.Estado != "Rechazado")
            throw new AbrilException("Solo se puede enviar un dossier en estado Borrador o Rechazado.", 400);

        var pendientes = detalle.Documentos
            .Where(d => d.Estado == "Pendiente")
            .Select(d => d.TipoDoc)
            .ToList();
        if (pendientes.Count > 0)
            throw new AbrilException(
                $"Debes subir el documento o marcarlo como 'No aplica' antes de enviar: {string.Join(", ", pendientes)}.",
                400);

        await _repo.EnviarAsync(dossierId);
    }

    public async Task RevisarAsync(int dossierId, RevisarDossierRequest req)
    {
        if (req.Estado != "Aprobado" && req.Estado != "Rechazado")
            throw new AbrilException("Estado inválido. Use 'Aprobado' o 'Rechazado'.", 400);

        var detalle = await _repo.GetDetalleAsync(dossierId)
            ?? throw new AbrilException("Dossier no encontrado.", 404);
        if (detalle.Estado != "Enviado")
            throw new AbrilException("Solo se puede revisar un dossier en estado Enviado.", 400);

        await _repo.RevisarAsync(dossierId, req);
    }

    public async Task RevisarDocumentoAsync(int docId, RevisarDocumentoRequest req)
    {
        if (req.Estado != "Aprobado" && req.Estado != "Observado")
            throw new AbrilException("Estado inválido. Use 'Aprobado' u 'Observado'.", 400);

        await _repo.RevisarDocumentoAsync(docId, req);
    }

    public async Task<string> GetDocumentoUrlAsync(int docId)
    {
        var path = await _repo.GetArchivoPathAsync(docId)
            ?? throw new AbrilException("Documento sin archivo adjunto.", 404);
        var url = await _sharePoint.GetDownloadUrlAsync(path, "dossier-semanal");
        if (string.IsNullOrWhiteSpace(url))
            throw new AbrilException("No se pudo obtener la URL del archivo.", 502);
        return url;
    }

    public Task<int?> GetContributorIdDeDocumentoAsync(int docId) => _repo.GetContributorIdDeDocumentoAsync(docId);

    public Task<int?> GetContributorIdDeArchivoAsync(int archivoId) => _repo.GetContributorIdDeArchivoAsync(archivoId);
}

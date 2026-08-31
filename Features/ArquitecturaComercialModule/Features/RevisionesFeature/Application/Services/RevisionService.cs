using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.RevisionesFeature.Application.Dtos;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.RevisionesFeature.Application.Interfaces;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.RevisionesFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.RevisionesFeature.Infrastructure.Models;

namespace Abril_Backend.Features.ArquitecturaComercialModule.Features.RevisionesFeature.Application.Services;

public class RevisionService : IRevisionService
{
    private readonly IRevisionRepository _repository;
    private readonly IRevisionSharePointService _sharePoint;

    public RevisionService(IRevisionRepository repository, IRevisionSharePointService sharePoint)
    {
        _repository = repository;
        _sharePoint = sharePoint;
    }

    public Task<List<RevisionDTO>> GetRevisiones(int? proyectoId, bool soloActivas)
        => _repository.GetRevisiones(proyectoId, soloActivas);

    public async Task<RevisionDTO> CreateRevision(CreateRevisionDTO body)
    {
        if (body.ProyectoId <= 0) throw new AbrilException("Debe seleccionar un proyecto.", 400);
        if (string.IsNullOrWhiteSpace(body.Tipo) || !TipoRevision.EsValido(body.Tipo))
            throw new AbrilException("Debe seleccionar un tipo de revisión válido.", 400);
        if (string.IsNullOrWhiteSpace(body.Lugar))
            throw new AbrilException("Debe indicar el lugar a revisar.", 400);

        var proyectoNombre = await _repository.GetProyectoNombre(body.ProyectoId)
            ?? throw new AbrilException("No se encontró el proyecto.", 404);

        var lugar = body.Lugar.Trim();
        var nombre = $"{body.Tipo}-{proyectoNombre}-{lugar}";

        var entity = await _repository.CreateRevision(body.ProyectoId, body.Tipo, lugar, nombre);
        return new RevisionDTO
        {
            Id = entity.Id,
            ProyectoId = entity.ProyectoId,
            ProyectoNombre = proyectoNombre,
            Tipo = entity.Tipo,
            Lugar = entity.Lugar,
            Nombre = entity.Nombre,
            Activo = entity.Activo
        };
    }

    public Task<bool> DeleteRevision(int id) => _repository.DeleteRevision(id);

    public Task<RevisionObservacionListResponseDTO> GetObservaciones(int? revisionId, int? proyectoId, string? estado, string? partida, DateTime? desde, DateTime? hasta, string? search, int pagina, int porPagina)
        => _repository.GetObservaciones(revisionId, proyectoId, estado, partida, desde, hasta, search, pagina, porPagina);

    public Task<RevisionObservacionListItemDTO?> GetObservacionById(int id) => _repository.GetObservacionById(id);

    public Task<RevisionFiltrosDTO> GetFiltros() => _repository.GetFiltros();

    public Task<RevisionDashboardDTO> GetDashboard(DateTime? desde, DateTime? hasta, int? proyectoId)
        => _repository.GetDashboard(desde, hasta, proyectoId);

    public Task<RevisionObservacionStatsDTO> GetStats(DateTime? desde, DateTime? hasta, int? proyectoId)
        => _repository.GetStats(desde, hasta, proyectoId);

    public async Task<RevisionObservacionListItemDTO> CreateObservacion(CreateRevisionObservacionDTO body, Stream? fotoStream, string? fotoFileName)
    {
        if (body.RevisionId <= 0) throw new AbrilException("Debe seleccionar una revisión.", 400);
        if (string.IsNullOrWhiteSpace(body.Descripcion)) throw new AbrilException("Debe ingresar una descripción de la observación.", 400);

        var revision = await _repository.GetRevisionEntityById(body.RevisionId)
            ?? throw new AbrilException("No se encontró la revisión.", 404);

        var entity = await _repository.CreateObservacion(body);

        if (fotoStream != null && !string.IsNullOrWhiteSpace(fotoFileName))
        {
            var contentType = GetContentType(fotoFileName);
            var url = await _sharePoint.SubirFotoObservacionAsync(fotoStream, fotoFileName, contentType, revision.ProyectoId, entity.Id);
            await _repository.AgregarFoto(entity.Id, "Observacion", url, 0);
        }

        return (await _repository.GetObservacionById(entity.Id))!;
    }

    public async Task<RevisionObservacionListItemDTO?> LevantarObservacion(int id, Stream? fotoStream, string? fotoFileName, LevantarRevisionObservacionDTO body)
    {
        var actual = await _repository.GetObservacionById(id);
        if (actual == null) throw new AbrilException("No se encontró la observación.", 404);
        if (body.LevantaPorWorkerId is null or <= 0)
            throw new AbrilException("Debe indicar quién levanta la observación.", 400);

        if (fotoStream != null && !string.IsNullOrWhiteSpace(fotoFileName))
        {
            var contentType = GetContentType(fotoFileName);
            var orden = actual.Fotos.Count(f => f.Tipo == "Levantamiento");
            var url = await _sharePoint.SubirFotoLevantamientoAsync(fotoStream, fotoFileName, contentType, actual.ProyectoId, id, orden);
            await _repository.AgregarFoto(id, "Levantamiento", url, orden);
        }

        return await _repository.LevantarObservacion(id, body.LevantaPorWorkerId);
    }

    public Task<RevisionObservacionListItemDTO?> UpdateObservacion(int id, UpdateRevisionObservacionDTO body)
        => _repository.UpdateObservacion(id, body);

    /// <summary>Sube la foto de "Observacion" cuando se reportó sin ella — distinto de
    /// ReemplazarFoto, que requiere una foto ya existente.</summary>
    public async Task<string> AgregarFotoObservacion(int revisionObservacionId, Stream fotoStream, string fotoFileName)
    {
        var observacion = await _repository.GetObservacionById(revisionObservacionId);
        if (observacion == null) throw new AbrilException("No se encontró la observación.", 404);
        if (observacion.Fotos.Any(f => f.Tipo == "Observacion"))
            throw new AbrilException("La observación ya tiene una foto. Use reemplazar en su lugar.", 400);

        var contentType = GetContentType(fotoFileName);
        var url = await _sharePoint.SubirFotoObservacionAsync(fotoStream, fotoFileName, contentType, observacion.ProyectoId, revisionObservacionId);
        var foto = await _repository.AgregarFoto(revisionObservacionId, "Observacion", url, 0);
        return foto.Url;
    }

    public async Task<string> ReemplazarFoto(int fotoId, Stream fotoStream, string fotoFileName)
    {
        var foto = await _repository.GetFotoById(fotoId);
        if (foto?.RevisionObservacion?.Revision == null) throw new AbrilException("No se encontró la foto.", 404);

        var contentType = GetContentType(fotoFileName);
        var proyectoId = foto.RevisionObservacion.Revision.ProyectoId;
        var url = foto.Tipo == "Levantamiento"
            ? await _sharePoint.SubirFotoLevantamientoAsync(fotoStream, fotoFileName, contentType, proyectoId, foto.RevisionObservacionId, foto.Orden)
            : await _sharePoint.SubirFotoObservacionAsync(fotoStream, fotoFileName, contentType, proyectoId, foto.RevisionObservacionId);

        await _repository.ActualizarFoto(fotoId, url);
        return url;
    }

    public async Task<(byte[] Bytes, string ContentType)?> GetFotoContenido(int fotoId)
    {
        var foto = await _repository.GetFotoById(fotoId);
        if (foto == null) return null;
        return await _sharePoint.DescargarFotoAsync(foto.Url);
    }

    private static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "image/jpeg"
        };
    }
}

using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Application.Dtos;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Application.Interfaces;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Application.Services;

public class ObservacionService : IObservacionService
{
    private readonly IObservacionRepository _repository;
    private readonly IObservacionSharePointService _sharePoint;

    public ObservacionService(IObservacionRepository repository, IObservacionSharePointService sharePoint)
    {
        _repository = repository;
        _sharePoint = sharePoint;
    }

    public Task<ObservacionListResponseDTO> GetObservaciones(int? proyectoId, string? estado, string? partida, DateTime? desde, DateTime? hasta, string? search, int pagina, int porPagina)
        => _repository.GetObservaciones(proyectoId, estado, partida, desde, hasta, search, pagina, porPagina);

    public Task<ObservacionListItemDTO?> GetObservacionById(int id) => _repository.GetObservacionById(id);

    public Task<ObservacionFiltrosDTO> GetFiltros() => _repository.GetFiltros();

    public Task<ObservacionDashboardDTO> GetDashboard(DateTime? desde, DateTime? hasta, int? proyectoId)
        => _repository.GetDashboard(desde, hasta, proyectoId);

    public Task<ObservacionStatsDTO> GetStats(DateTime? desde, DateTime? hasta, int? proyectoId)
        => _repository.GetStats(desde, hasta, proyectoId);

    public async Task<ObservacionListItemDTO> CreateObservacion(CreateObservacionDTO body, Stream? fotoStream, string? fotoFileName)
    {
        if (body.ProyectoId <= 0) throw new AbrilException("Debe seleccionar un proyecto.", 400);
        if (string.IsNullOrWhiteSpace(body.Descripcion)) throw new AbrilException("Debe ingresar una descripción de la observación.", 400);

        var abbreviation = await _repository.GetProyectoAbbreviation(body.ProyectoId);
        var anio = body.Fecha.Year;
        var correlativo = await _repository.GetProximoCorrelativo(abbreviation, anio);
        var codigo = $"{abbreviation}-{correlativo}-{anio}";

        var entity = await _repository.CreateObservacion(body, codigo);

        if (fotoStream != null && !string.IsNullOrWhiteSpace(fotoFileName))
        {
            var contentType = GetContentType(fotoFileName);
            var url = await _sharePoint.SubirFotoObservacionAsync(fotoStream, fotoFileName, contentType, body.ProyectoId, entity.Id);
            await _repository.AgregarFoto(entity.Id, "Observacion", url, 0);
        }

        return (await _repository.GetObservacionById(entity.Id))!;
    }

    public async Task<ObservacionListItemDTO?> LevantarObservacion(int id, Stream? fotoStream, string? fotoFileName, LevantarObservacionDTO body)
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

    public Task<ObservacionListItemDTO?> UpdateObservacion(int id, UpdateObservacionDTO body)
        => _repository.UpdateObservacion(id, body);

    /// <summary>Sube la foto de "Observacion" cuando la observación se creó sin ella —
    /// caso distinto de <see cref="ReemplazarFoto"/>, que requiere una foto ya existente.</summary>
    public async Task<string> AgregarFotoObservacion(int observacionId, Stream fotoStream, string fotoFileName)
    {
        var observacion = await _repository.GetObservacionById(observacionId);
        if (observacion == null) throw new AbrilException("No se encontró la observación.", 404);
        if (observacion.Fotos.Any(f => f.Tipo == "Observacion"))
            throw new AbrilException("La observación ya tiene una foto. Use reemplazar en su lugar.", 400);

        var contentType = GetContentType(fotoFileName);
        var url = await _sharePoint.SubirFotoObservacionAsync(fotoStream, fotoFileName, contentType, observacion.ProyectoId, observacionId);
        var foto = await _repository.AgregarFoto(observacionId, "Observacion", url, 0);
        return foto.Url;
    }

    /// <summary>Reemplaza el archivo de una foto ya subida (observación o levantamiento) — el
    /// caso "me equivoqué de foto" que antes no tenía solución sin recrear la observación.</summary>
    public async Task<string> ReemplazarFoto(int fotoId, Stream fotoStream, string fotoFileName)
    {
        var foto = await _repository.GetFotoById(fotoId);
        if (foto?.Observacion == null) throw new AbrilException("No se encontró la foto.", 404);

        var contentType = GetContentType(fotoFileName);
        var url = foto.Tipo == "Levantamiento"
            ? await _sharePoint.SubirFotoLevantamientoAsync(fotoStream, fotoFileName, contentType, foto.Observacion.ProyectoId, foto.ObservacionId, foto.Orden)
            : await _sharePoint.SubirFotoObservacionAsync(fotoStream, fotoFileName, contentType, foto.Observacion.ProyectoId, foto.ObservacionId);

        await _repository.ActualizarFoto(fotoId, url);
        return url;
    }

    /// <summary>Bytes de una foto para servirla desde nuestro propio dominio (ver comentario en
    /// el controller sobre por qué las miniaturas no cargaban en celulares sin sesión de SharePoint).</summary>
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

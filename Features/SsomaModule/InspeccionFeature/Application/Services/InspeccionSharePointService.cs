using Abril_Backend.Features.Habilitacion.Application.Interfaces;

namespace Abril_Backend.Features.SsomaModule.InspeccionFeature.Application.Services;

public class InspeccionSharePointService : IInspeccionSharePointService
{
    private readonly ISharePointHabService _sp;

    public InspeccionSharePointService(ISharePointHabService sp) => _sp = sp;

    public Task<string> SubirFotoHallazgoAsync(Stream stream, string filename, int inspeccionId, int hallazgoId)
        => _sp.SubirArchivoEnRutaAsync(stream, filename, "inspeccion-fotos", $"Inspecciones/{inspeccionId}/hallazgos/{hallazgoId}");

    public Task<string> SubirFotoAreaAsync(Stream stream, string filename, int inspeccionId, int orden)
        => _sp.SubirArchivoEnRutaAsync(stream, filename, "inspeccion-fotos", $"Inspecciones/{inspeccionId}/area");

    public Task<string> SubirFirmaInspectorAsync(Stream stream, string filename, int inspeccionId)
        => _sp.SubirArchivoEnRutaAsync(stream, filename, "inspeccion-firmas", $"Inspecciones/{inspeccionId}/firmas");

    public Task<string> SubirFirmaRepresentanteAsync(Stream stream, string filename, int inspeccionId)
        => _sp.SubirArchivoEnRutaAsync(stream, filename, "inspeccion-firmas", $"Inspecciones/{inspeccionId}/firmas");

    public Task<byte[]?> DescargarAsync(string url, string libraryContexto)
        => _sp.DescargarContenidoAsync(url, libraryContexto);
}

using Abril_Backend.Features.Habilitacion.Application.Interfaces;

namespace Abril_Backend.Features.SsomaModule.OptFeature.Application.Services;

public class OptSharePointService : IOptSharePointService
{
    private readonly ISharePointHabService _sp;

    public OptSharePointService(ISharePointHabService sp) => _sp = sp;

    public Task<string> SubirFirmaObservadorAsync(Stream stream, string filename, int optId)
        => _sp.SubirArchivoEnRutaAsync(stream, filename, "opt-firmas", $"OPT/{optId}/firmas");

    public Task<string> SubirFirmaTrabajadorAsync(Stream stream, string filename, int optId, int trabajadorId)
        => _sp.SubirArchivoEnRutaAsync(stream, filename, "opt-firmas", $"OPT/{optId}/firmas");

    public Task<string> SubirFotoAreaAsync(Stream stream, string filename, int optId, int orden)
        => _sp.SubirArchivoEnRutaAsync(stream, filename, "opt-fotos", $"OPT/{optId}/area");
}

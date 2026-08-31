using Abril_Backend.Features.Habilitacion.Application.Interfaces;

namespace Abril_Backend.Features.Ssoma.Rac.Services;

public class RacSharePointService : IRacSharePointService
{
    private readonly ISharePointHabService _sp;
    private readonly IHttpClientFactory _httpClientFactory;

    public RacSharePointService(ISharePointHabService sp, IHttpClientFactory httpClientFactory)
    {
        _sp = sp;
        _httpClientFactory = httpClientFactory;
    }

    public Task<string> SubirPdfAsync(Stream stream, string fileName, int racId)
        => _sp.SubirArchivoEnRutaAsync(stream, fileName, "rac-pdf", $"RAC/{racId}");

    public Task<string> SubirFotoAsync(Stream stream, string fileName, int racId)
        => _sp.SubirArchivoEnRutaAsync(stream, fileName, "rac-fotos", $"RAC/{racId}/fotos");

    public Task<string> SubirFirmaAsync(Stream stream, string fileName, int racId)
        => _sp.SubirArchivoEnRutaAsync(stream, fileName, "rac-firmas", $"RAC/{racId}/firmas");

    public Task<string> SubirPenalidadPdfAsync(Stream stream, string fileName, int penalidadId)
        => _sp.SubirArchivoEnRutaAsync(stream, fileName, "penalidad-pdf", $"Penalidades/{penalidadId}");

    public async Task<byte[]?> DescargarFotoAsync(string url)
    {
        var downloadUrl = await _sp.GetDownloadUrlAsync(url, "rac-fotos");
        if (string.IsNullOrWhiteSpace(downloadUrl)) return null;
        var http = _httpClientFactory.CreateClient();
        return await http.GetByteArrayAsync(downloadUrl);
    }
}

using Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Application.Interfaces;
using Abril_Backend.Shared.Services.SharePoint.Interfaces;
using Abril_Backend.Shared.Services.SharePoint.Options;

namespace Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Application.Services;

public class ObservacionSharePointService : IObservacionSharePointService
{
    private readonly IGraphSharePointService _sharePoint;
    private readonly SharePointSiteRef _site;

    private const string Library = "BObservacionesArqComercial";

    public ObservacionSharePointService(IGraphSharePointService sharePoint, IConfiguration configuration)
    {
        _sharePoint = sharePoint;
        _site = SharePointSiteRef.FromConfig(configuration, "ObservacionesArqCom");
    }

    public async Task<string> SubirFotoObservacionAsync(Stream stream, string fileName, string contentType, int proyectoId, int observacionId)
    {
        var result = await _sharePoint.UploadToSharePointLibraryAsync(
            site: _site,
            libraryName: Library,
            folderPath: $"Observaciones/{proyectoId}/{observacionId}",
            fileName: fileName,
            fileStream: stream,
            contentType: contentType);

        return result?.WebUrl ?? throw new InvalidOperationException("SharePoint no devolvió una URL para la foto de observación.");
    }

    public async Task<string> SubirFotoLevantamientoAsync(Stream stream, string fileName, string contentType, int proyectoId, int observacionId, int orden)
    {
        var result = await _sharePoint.UploadToSharePointLibraryAsync(
            site: _site,
            libraryName: Library,
            folderPath: $"Observaciones/{proyectoId}/{observacionId}/levantamiento",
            fileName: fileName,
            fileStream: stream,
            contentType: contentType,
            autoRenameOnLock: true);

        return result?.WebUrl ?? throw new InvalidOperationException("SharePoint no devolvió una URL para la foto de levantamiento.");
    }

    public async Task<(byte[] Bytes, string ContentType)> DescargarFotoAsync(string url)
    {
        var bytes = await _sharePoint.DownloadFromSharePointAsync(_site, url);
        var ext = Path.GetExtension(new Uri(url).AbsolutePath).ToLowerInvariant();
        var contentType = ext switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "image/jpeg"
        };
        return (bytes, contentType);
    }
}

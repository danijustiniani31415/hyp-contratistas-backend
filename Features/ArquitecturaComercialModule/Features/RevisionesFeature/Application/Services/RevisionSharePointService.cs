using Abril_Backend.Features.ArquitecturaComercialModule.Features.RevisionesFeature.Application.Interfaces;
using Abril_Backend.Shared.Services.SharePoint.Interfaces;
using Abril_Backend.Shared.Services.SharePoint.Options;

namespace Abril_Backend.Features.ArquitecturaComercialModule.Features.RevisionesFeature.Application.Services;

public class RevisionSharePointService : IRevisionSharePointService
{
    private readonly IGraphSharePointService _sharePoint;
    private readonly SharePointSiteRef _site;

    // Mismo sitio SharePoint que Observaciones (gestionobservaciones), librería distinta —
    // confirmado en la biblioteca real "BibliotecaRevision.xlsx" de la app legacy (Power Apps).
    private const string Library = "BRevisionesArqComercial";

    public RevisionSharePointService(IGraphSharePointService sharePoint, IConfiguration configuration)
    {
        _sharePoint = sharePoint;
        _site = SharePointSiteRef.FromConfig(configuration, "ObservacionesArqCom");
    }

    public async Task<string> SubirFotoObservacionAsync(Stream stream, string fileName, string contentType, int proyectoId, int revisionObservacionId)
    {
        var result = await _sharePoint.UploadToSharePointLibraryAsync(
            site: _site,
            libraryName: Library,
            folderPath: $"Revisiones/{proyectoId}/{revisionObservacionId}",
            fileName: fileName,
            fileStream: stream,
            contentType: contentType);

        return result?.WebUrl ?? throw new InvalidOperationException("SharePoint no devolvió una URL para la foto de observación.");
    }

    public async Task<string> SubirFotoLevantamientoAsync(Stream stream, string fileName, string contentType, int proyectoId, int revisionObservacionId, int orden)
    {
        var result = await _sharePoint.UploadToSharePointLibraryAsync(
            site: _site,
            libraryName: Library,
            folderPath: $"Revisiones/{proyectoId}/{revisionObservacionId}/levantamiento",
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

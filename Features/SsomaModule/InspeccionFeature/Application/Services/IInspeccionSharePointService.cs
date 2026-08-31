namespace Abril_Backend.Features.SsomaModule.InspeccionFeature.Application.Services;

public interface IInspeccionSharePointService
{
    Task<string> SubirFotoHallazgoAsync(Stream stream, string filename, int inspeccionId, int hallazgoId);
    Task<string> SubirFotoAreaAsync(Stream stream, string filename, int inspeccionId, int orden);
    Task<string> SubirFirmaInspectorAsync(Stream stream, string filename, int inspeccionId);
    Task<string> SubirFirmaRepresentanteAsync(Stream stream, string filename, int inspeccionId);
    Task<byte[]?> DescargarAsync(string url, string libraryContexto);
}

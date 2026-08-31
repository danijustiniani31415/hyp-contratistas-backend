namespace Abril_Backend.Features.SsomaModule.OptFeature.Application.Services;

public interface IOptSharePointService
{
    Task<string> SubirFirmaObservadorAsync(Stream stream, string filename, int optId);
    Task<string> SubirFirmaTrabajadorAsync(Stream stream, string filename, int optId, int trabajadorId);
    Task<string> SubirFotoAreaAsync(Stream stream, string filename, int optId, int orden);
}

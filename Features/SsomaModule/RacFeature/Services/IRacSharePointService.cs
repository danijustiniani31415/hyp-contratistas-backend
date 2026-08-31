namespace Abril_Backend.Features.Ssoma.Rac.Services;

public interface IRacSharePointService
{
    Task<string> SubirPdfAsync(Stream pdfStream, string filename, int racId);
    Task<string> SubirFotoAsync(Stream stream, string filename, int racId);
    Task<string> SubirFirmaAsync(Stream stream, string filename, int racId);
    Task<string> SubirPenalidadPdfAsync(Stream stream, string filename, int penalidadId);
    Task<byte[]?> DescargarFotoAsync(string url);
}

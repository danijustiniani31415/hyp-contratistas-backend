using Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Infrastructure.Interfaces;

public interface IAmonestacionRepository
{
    Task<AmonestacionInitDto> GetInitAsync();
    Task<int> CrearAsync(AmonestacionDetalleDto detalle, List<(string Base64, string NombreArchivo, string Url)> fotos);
    Task<(List<AmonestacionListItemDto> Items, int Total)> GetListAsync(AmonestacionListQuery q);
    Task<AmonestacionDetalleDto?> GetDetalleAsync(int id);
    Task<AmonestacionDashboardDto> GetDashboardAsync(int? empresaIdContratista = null);
    Task<WorkerPuntajeDto?> GetPuntajeWorkerAsync(int workerId);
    Task<string> GenerarCodigoAsync(int proyectoId);
    Task GuardarPdfUrlAsync(int id, string url);
    Task<List<(byte[] Bytes, string Nombre)>> GetFotosBytesAsync(int id);
    Task LimpiarBase64FotosAsync(int id);
    Task ConfirmarEstadoAsync(int id);
    Task CerrarAsync(int id, string documentoFirmadoUrl);
    Task<(int WorkerId, string CambiosResumen)> EditarAsync(int id, AmonestacionEditRequest req, int userId);
}

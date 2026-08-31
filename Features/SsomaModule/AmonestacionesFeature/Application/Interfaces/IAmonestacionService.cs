using Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Application.Interfaces;

public interface IAmonestacionService
{
    Task<AmonestacionInitDto> GetInitAsync();
    Task<AmonestacionCreadaDto> CrearAsync(AmonestacionCreateRequest req, int userId);
    Task<AmonestacionPagedResult<AmonestacionListItemDto>> GetListAsync(AmonestacionListQuery q);
    Task<AmonestacionDetalleDto?> GetDetalleAsync(int id);
    Task<AmonestacionDashboardDto> GetDashboardAsync(int? empresaIdContratista = null);
    Task<WorkerPuntajeDto?> GetPuntajeWorkerAsync(int workerId);
    Task<(byte[]? Bytes, string? RedirectUrl)> GetPdfAsync(int id);
    Task<AmonestacionCreadaDto> ConfirmarAsync(int id, int userId);
    Task CerrarAsync(int id, AmonestacionCerrarRequest req);

    /// <summary>Corrige una amonestación ya creada (acceso restringido a nivel de controller). Reevalúa la inhabilitación del trabajador tras el cambio.</summary>
    Task EditarAsync(int id, AmonestacionEditRequest req, int userId);
}

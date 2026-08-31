using Abril_Backend.Features.SsomaModule.OptFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.OptFeature.Application.Interfaces;

public interface IOptRepository
{
    Task<List<OptListItemDto>> GetListAsync(int? proyectoId, int? petId, string? tipoObservacion,
        DateTime? fechaDesde, DateTime? fechaHasta, int? trabajadorId, int page, int pageSize,
        int? empresaIdContratista = null, int? empresaObservadorId = null, int? empresaTrabajadorId = null);
    Task<int> GetListCountAsync(int? proyectoId, int? petId, string? tipoObservacion,
        DateTime? fechaDesde, DateTime? fechaHasta, int? trabajadorId, int? empresaIdContratista = null,
        int? empresaObservadorId = null, int? empresaTrabajadorId = null);
    Task<OptDetalleDto?> GetDetalleAsync(int id);
    Task<int> CrearOptAsync(CrearOptRequest request, string? firmaObservadorUrl,
        Dictionary<int, string> firmasTrabajadorUrls, List<string> fotosAreaUrls, int userId = 0);
    Task<OptDashboardDto> GetDashboardAsync(int? proyectoId, int? anio, int? empresaIdContratista = null);
    Task<List<OptPetDto>> GetPetsAsync();
    Task<List<OptCriterioVerificacionDto>> GetCriteriosVerificacionAsync();
    Task UpdateFirmasAsync(int optId, string? firmaObservadorUrl, Dictionary<int, string> firmasTrabajadorUrls, List<string> fotosAreaUrls);
}

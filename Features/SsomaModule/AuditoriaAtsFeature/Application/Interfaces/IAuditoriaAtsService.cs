using Abril_Backend.Features.SsomaModule.AuditoriaAtsFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.AuditoriaAtsFeature.Application.Interfaces;

public interface IAuditoriaAtsService
{
    Task<List<AuditoriaAtsPreguntaDto>> GetPreguntasAsync();
    Task<object> GetListAsync(int? auditadoWorkerId, int? auditorWorkerId, int? proyectoId,
        DateOnly? fechaDesde, DateOnly? fechaHasta, string? estado, int page, int pageSize,
        int? empresaIdContratista = null);
    Task<AuditoriaAtsDetalleDto> GetDetalleAsync(int id);
    Task<int> CrearAsync(CrearAuditoriaAtsRequest request);
}

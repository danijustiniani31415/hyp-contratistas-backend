using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.SctrGestion;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces
{
    public interface ISctrGestionService
    {
        Task<List<SctrGestionDto>> GetByCasoSocialId(Guid casoSocialId);
        Task<int> Create(Guid casoSocialId, SctrGestionCreateDto dto, int? userId);
        Task Update(int id, SctrGestionUpdateDto dto);
        Task Delete(int id);
    }
}

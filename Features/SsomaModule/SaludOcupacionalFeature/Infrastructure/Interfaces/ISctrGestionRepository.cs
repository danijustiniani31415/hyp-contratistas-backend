using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.SctrGestion;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces
{
    public interface ISctrGestionRepository
    {
        Task<List<SctrGestionDto>> GetByCasoSocialId(Guid casoSocialId);
        Task<int> Create(Guid casoSocialId, SctrGestionCreateDto dto, int registradoPorId);
        Task Update(int id, SctrGestionUpdateDto dto);
        Task Delete(int id);
    }
}

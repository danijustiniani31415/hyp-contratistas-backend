using Abril_Backend.Features.PlaneamientoBimFeature.Application.Dtos;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Interfaces
{
    public interface IPlaneamientoBimRestriccionRepository
    {
        Task<List<RestriccionDto>> GetPaged(int projectId, bool? soloActivos);
        Task<RestriccionDto> Create(int projectId, RestriccionCreateDto dto, int userId);
        Task<RestriccionDto> Update(int restriccionId, RestriccionUpdateDto dto);
        Task<RestriccionDto> Cerrar(int restriccionId);
    }
}

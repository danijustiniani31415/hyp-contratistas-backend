using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.CasoSocial;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces
{
    public interface ISeguimientoRepository
    {
        Task<Guid> Create(Guid casoId, SeguimientoCreateDto dto);
        Task Delete(Guid id);
    }
}

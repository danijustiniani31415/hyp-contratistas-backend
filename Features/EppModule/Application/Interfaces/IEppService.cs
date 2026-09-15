using Abril_Backend.Features.EppModule.Application.Dtos;

namespace Abril_Backend.Features.EppModule.Application.Interfaces
{
    public interface IEppService
    {
        Task<EntregaEppDetailDto> Crear(EntregaEppCreateDto dto, long entregadoPorId);
        Task<EntregaEppListResponseDto> List(int? personaId, int page, int pageSize);
        Task<EntregaEppDetailDto> GetById(long id);
    }
}

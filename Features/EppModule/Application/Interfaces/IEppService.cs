using Abril_Backend.Features.EppModule.Application.Dtos;

namespace Abril_Backend.Features.EppModule.Application.Interfaces
{
    public interface IEppService
    {
        Task<EntregaEppDetailDto> Crear(EntregaEppCreateDto dto, long entregadoPorId);
        Task<EntregaEppListResponseDto> List(string? search, int? personaId, HashSet<int>? proyectosPermitidos, int page, int pageSize);
        Task<EntregaEppDetailDto> GetById(long id);
    }
}

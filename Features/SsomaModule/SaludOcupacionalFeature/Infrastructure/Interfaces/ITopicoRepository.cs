using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Topico;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces
{
    public interface ITopicoRepository
    {
        Task<PagedResult<TopicoListItemDto>> ListPaged(TopicoFilterDto filter);
        Task<TopicoDetalleDto> GetById(int id);
        Task<List<TopicoListItemDto>> GetHoy();
        Task<List<TopicoTipoAtencionDto>> GetTiposAtencion();
        Task<int> Create(TopicoCreateDto dto, int registradoPorId);
        Task SetDescansoGenerado(int topicoId, int descansoId);
        Task Update(int id, TopicoUpdateDto dto);
        Task Delete(int id);
        Task Cerrar(int id, int? userId);
        Task<List<TopicoEvolucionDto>> GetEvoluciones(int topicoId);
        Task<int> CreateEvolucion(int topicoId, TopicoEvolucionCreateDto dto, int registradoPorId);
        Task DeleteEvolucion(int evolucionId);
    }
}

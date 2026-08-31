using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.CasoSocial;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces
{
    public interface ICasoSocialService
    {
        Task<PagedResult<CasoSocialListItemDto>> ListPaged(CasoSocialFilterDto filter);
        Task<CasoSocialDetalleDto> GetById(Guid id);
        Task<Guid> Create(CasoSocialCreateDto dto, int? userId);
        Task Update(Guid id, CasoSocialUpdateDto dto, int? userId);
        Task Cerrar(Guid id, CasoSocialCerrarDto dto, int? userId);
        Task Delete(Guid id);
        Task<Guid> CreateSeguimiento(Guid casoId, SeguimientoCreateDto dto);
        Task DeleteSeguimiento(Guid seguimientoId);
    }
}

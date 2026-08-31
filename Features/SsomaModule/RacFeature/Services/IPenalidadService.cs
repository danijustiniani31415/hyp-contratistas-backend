using Abril_Backend.Features.Ssoma.Rac.Dtos;

namespace Abril_Backend.Features.Ssoma.Rac.Services;

public interface IPenalidadService
{
    Task<RacPagedResult<PenalidadListItemDto>> GetListAsync(PenalidadListQuery q);
    Task<PenalidadDetalleDto?> GetDetalleAsync(int id);
    Task PresentarDescargaAsync(int id, PenalidadDescargaRequest req, int userId);
    Task<PenalidadDetalleDto> ResolverAsync(int id, PenalidadResolverRequest req, int userId);
    Task<string> GetPdfResolucionAsync(int id);
}

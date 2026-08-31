using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.SsomaModule.HorasHombreFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.HorasHombreFeature.Application.Interfaces
{
    public interface IHorasHombreService
    {
        Task<PagedResult<HorasHombreDiaDto>> GetTablaAsync(HorasHombreFilterDto filter);
        Task<HorasHombreDashboardDto> GetDashboardAsync(int? proyectoId, int? mes, int? anio);
    }
}

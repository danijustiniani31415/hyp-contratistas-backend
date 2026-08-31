using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.SsomaModule.HorasHombreFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.HorasHombreFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.HorasHombreFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.SsomaModule.HorasHombreFeature.Application.Services
{
    public class HorasHombreService : IHorasHombreService
    {
        private readonly IHorasHombreRepository _repo;

        public HorasHombreService(IHorasHombreRepository repo)
        {
            _repo = repo;
        }

        public Task<PagedResult<HorasHombreDiaDto>> GetTablaAsync(HorasHombreFilterDto filter)
            => _repo.GetTablaAsync(filter);

        public Task<HorasHombreDashboardDto> GetDashboardAsync(int? proyectoId, int? mes, int? anio)
            => _repo.GetDashboardAsync(proyectoId, mes, anio);
    }
}

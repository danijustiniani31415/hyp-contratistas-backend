using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Convalidacion;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces
{
    public interface IConvalidacionRepository
    {
        Task<PagedResponseDto<ConvalidacionListDto>> List(ConvalidacionFilterDto filter);
        Task<ConvalidacionDetalleDto?> GetDetalleAsync(int id);
        Task<int> Create(ConvalidacionCreateDto dto, int? userId, string? ip, string? userAgent);
        Task Update(int id, ConvalidacionUpdateDto dto, int? userId, string? ip, string? userAgent);
        Task<MedicoFirmaEstadoDto?> GetMedicoFirmaEstadoAsync(int medicoId);
        Task RegistrarIntentoFirmaAsync(int medicoId, bool exito);
    }
}

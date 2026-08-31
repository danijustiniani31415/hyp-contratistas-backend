using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Convalidacion;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces
{
    public interface IConvalidacionService
    {
        Task<PagedResponseDto<ConvalidacionListDto>> List(ConvalidacionFilterDto filter);
        Task<int> Create(ConvalidacionCreateDto dto, int? userId, string? ip, string? userAgent);
        Task Update(int id, ConvalidacionUpdateDto dto, int? userId, string? ip, string? userAgent);
        Task<byte[]> GenerarPdfAsync(int id);
    }
}

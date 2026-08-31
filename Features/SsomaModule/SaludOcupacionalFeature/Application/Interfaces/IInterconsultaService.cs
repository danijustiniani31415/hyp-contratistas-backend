using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Interconsulta;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces
{
    public interface IInterconsultaService
    {
        Task<PagedResult<InterconsultaListDto>> List(InterconsultaFilterDto filter);
        Task<InterconsultaEnviarCorreoResultDto> EnviarRecordatorios(List<int> ids);
        Task<InterconsultaDetalleDto> GetById(int id);
        Task<int> Create(InterconsultaCreateDto dto, int? userId);
        Task Update(int id, InterconsultaUpdateDto dto, int? userId);
        Task UpdateResultado(int id, InterconsultaResultadoPatchDto dto, int? userId);
        Task UpdateDerivacion(int id, InterconsultaDerivacionPatchDto dto, int? userId);
    }
}

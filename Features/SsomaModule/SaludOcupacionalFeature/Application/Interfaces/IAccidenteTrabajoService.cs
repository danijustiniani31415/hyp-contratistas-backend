using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.AccidenteTrabajo;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces
{
    public interface IAccidenteTrabajoService
    {
        Task<PagedResult<AccidenteTrabajoListItemDto>> ListPaged(AccidenteFilterDto filter);
        Task<AccidenteTrabajoDetalleDto> GetById(int id);
        Task<int> Create(AccidenteTrabajoCreateDto dto, int? userId);
        Task Update(int id, AccidenteTrabajoUpdateDto dto);
        Task Cerrar(int id, AccidenteCerrarDto dto, int? userId);
        Task Delete(int id);
        Task<int> CreateSeguimiento(int accidenteId, AccidenteSeguimientoCreateDto dto, int? userId);
        Task DeleteSeguimiento(int seguimientoId);
        Task MarcarReinduccionAsync(int accidenteId, int? userId);
    }
}

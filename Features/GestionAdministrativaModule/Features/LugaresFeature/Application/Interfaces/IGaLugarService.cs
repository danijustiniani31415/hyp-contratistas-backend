using Abril_Backend.Features.GestionAdministrativa.Lugares.Application.Dtos;

namespace Abril_Backend.Features.GestionAdministrativa.Lugares.Application.Interfaces
{
    public interface IGaLugarService
    {
        Task<List<GaLugarConfigItemDto>> GetAll();
        Task CreateBatch(GaLugarCreateBatchDto dto);
        Task<bool> ToggleActivo(int id);
        Task<ToggleProyectoResultDto> ToggleProyecto(int projectId);
        Task Edit(int id, GaLugarEditDto dto);
    }
}

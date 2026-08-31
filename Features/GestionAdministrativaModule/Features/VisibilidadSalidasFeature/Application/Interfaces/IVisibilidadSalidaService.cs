using Abril_Backend.Features.GestionAdministrativa.Shared.Dtos;
using Abril_Backend.Features.GestionAdministrativa.VisibilidadSalidas.Application.Dtos;

namespace Abril_Backend.Features.GestionAdministrativa.VisibilidadSalidas.Application.Interfaces
{
    public interface IVisibilidadSalidaService
    {
        Task<VisibilidadInicialDto> GetInitialDataAsync();
        Task<List<GaAreaNodeDto>> GetAreaTreeAsync();
        Task<List<VisibilidadAsignacionDto>> GetWorkerAsignacionesAsync(int workerId);
        Task UpdateWorkerAsignacionesAsync(int workerId, List<VisibilidadAsignacionDto> asignaciones);
    }
}

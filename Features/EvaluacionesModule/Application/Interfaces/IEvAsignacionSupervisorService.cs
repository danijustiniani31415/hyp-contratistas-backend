using Abril_Backend.Features.Evaluaciones.Application.Dtos;

namespace Abril_Backend.Features.Evaluaciones.Application.Interfaces
{
    public interface IEvAsignacionSupervisorService
    {
        Task<List<SupervisorConAsignacionesDto>> GetSupervisoresConAsignacionesAsync();
        Task ActualizarAsignacionesAsync(int supervisorWorkerId, List<int> projectIds, int updatedByUserId);
        Task<List<ProyectoAsignadoDto>> GetProyectosActivosAsync();
    }
}

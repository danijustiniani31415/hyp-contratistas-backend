using Abril_Backend.Features.Evaluaciones.Application.Dtos;
using Abril_Backend.Features.Evaluaciones.Application.Interfaces;

namespace Abril_Backend.Features.Evaluaciones.Application.Services
{
    public class EvAsignacionSupervisorService : IEvAsignacionSupervisorService
    {
        private readonly IEvAsignacionSupervisorRepository _repo;

        public EvAsignacionSupervisorService(IEvAsignacionSupervisorRepository repo)
        {
            _repo = repo;
        }

        public Task<List<SupervisorConAsignacionesDto>> GetSupervisoresConAsignacionesAsync() =>
            _repo.GetSupervisoresConAsignacionesAsync();

        public Task ActualizarAsignacionesAsync(int supervisorWorkerId, List<int> projectIds, int updatedByUserId) =>
            _repo.ActualizarAsignacionesAsync(supervisorWorkerId, projectIds, updatedByUserId);

        public Task<List<ProyectoAsignadoDto>> GetProyectosActivosAsync() =>
            _repo.GetProyectosActivosAsync();
    }
}

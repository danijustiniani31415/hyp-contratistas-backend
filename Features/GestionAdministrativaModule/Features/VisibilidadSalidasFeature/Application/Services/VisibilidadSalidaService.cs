using Abril_Backend.Features.GestionAdministrativa.Shared.Dtos;
using Abril_Backend.Features.GestionAdministrativa.VisibilidadSalidas.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.VisibilidadSalidas.Application.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.VisibilidadSalidas.Infrastructure.Interfaces;

namespace Abril_Backend.Features.GestionAdministrativa.VisibilidadSalidas.Application.Services
{
    public class VisibilidadSalidaService : IVisibilidadSalidaService
    {
        private readonly IVisibilidadSalidaRepository _repo;

        public VisibilidadSalidaService(IVisibilidadSalidaRepository repo)
        {
            _repo = repo;
        }

        public Task<VisibilidadInicialDto> GetInitialDataAsync()
            => _repo.GetInitialDataAsync();

        public Task<List<GaAreaNodeDto>> GetAreaTreeAsync()
            => _repo.GetAreaTreeAsync();

        public Task<List<VisibilidadAsignacionDto>> GetWorkerAsignacionesAsync(int workerId)
            => _repo.GetWorkerAsignacionesAsync(workerId);

        public Task UpdateWorkerAsignacionesAsync(int workerId, List<VisibilidadAsignacionDto> asignaciones)
            => _repo.UpdateWorkerAsignacionesAsync(workerId, asignaciones);
    }
}

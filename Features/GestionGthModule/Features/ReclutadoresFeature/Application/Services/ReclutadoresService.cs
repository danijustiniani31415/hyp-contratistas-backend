using Abril_Backend.Features.GestionGthModule.Features.ReclutadoresFeature.Application.Dtos;
using Abril_Backend.Features.GestionGthModule.Features.ReclutadoresFeature.Application.Interfaces;
using Abril_Backend.Features.GestionGthModule.Features.ReclutadoresFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutadoresFeature.Application.Services
{
    /// <inheritdoc cref="IReclutadoresService"/>
    public class ReclutadoresService : IReclutadoresService
    {
        private readonly IReclutadoresRepository _repo;

        public ReclutadoresService(IReclutadoresRepository repo)
        {
            _repo = repo;
        }

        public Task<List<ReclutadorDto>> GetReclutadores() => _repo.GetReclutadoresAsync();

        public Task<ReclutadorToggleResultDto> Toggle(int workerId, bool activo, int? userId) =>
            _repo.ToggleAsync(workerId, activo, userId);
    }
}

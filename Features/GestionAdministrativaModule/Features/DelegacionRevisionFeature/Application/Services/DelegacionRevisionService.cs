using Abril_Backend.Features.GestionAdministrativa.DelegacionRevision.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.DelegacionRevision.Application.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.DelegacionRevision.Infrastructure.Interfaces;

namespace Abril_Backend.Features.GestionAdministrativa.DelegacionRevision.Application.Services
{
    public class DelegacionRevisionService : IDelegacionRevisionService
    {
        private readonly IDelegacionRevisionRepository _repo;

        public DelegacionRevisionService(IDelegacionRevisionRepository repo)
        {
            _repo = repo;
        }

        public Task<DelegacionInicialDto> GetInitialDataAsync(int userId)
            => _repo.GetInitialDataAsync(userId);

        public Task UpdateAsync(int userId, int areaScopeId, int? projectId, List<DelegacionAsignacionDto> revisores)
            => _repo.UpdateAsync(userId, areaScopeId, projectId, revisores);
    }
}

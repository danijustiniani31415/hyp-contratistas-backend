using Abril_Backend.Features.GestionAdministrativa.DelegacionRevision.Application.Dtos;

namespace Abril_Backend.Features.GestionAdministrativa.DelegacionRevision.Application.Interfaces
{
    public interface IDelegacionRevisionService
    {
        Task<DelegacionInicialDto> GetInitialDataAsync(int userId);
        Task UpdateAsync(int userId, int areaScopeId, int? projectId, List<DelegacionAsignacionDto> revisores);
    }
}

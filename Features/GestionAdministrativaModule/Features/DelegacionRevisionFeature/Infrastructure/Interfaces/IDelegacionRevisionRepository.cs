using Abril_Backend.Features.GestionAdministrativa.DelegacionRevision.Application.Dtos;

namespace Abril_Backend.Features.GestionAdministrativa.DelegacionRevision.Infrastructure.Interfaces
{
    public interface IDelegacionRevisionRepository
    {
        /// <param name="userId">Usuario autenticado (app_user).</param>
        Task<DelegacionInicialDto> GetInitialDataAsync(int userId);

        /// <summary>
        /// Reemplaza los revisores de una asignación (área o área+proyecto). El usuario debe ser
        /// revisor vivo de esa asignación; los designados deben pertenecer al área/proyecto; el
        /// usuario no puede quitarse a sí mismo (solo desactivarse).
        /// </summary>
        Task UpdateAsync(int userId, int areaScopeId, int? projectId, List<DelegacionAsignacionDto> revisores);
    }
}

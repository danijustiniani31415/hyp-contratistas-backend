using Abril_Backend.Features.GestionAdministrativa.AreaRevisores.Application.Dtos;

namespace Abril_Backend.Features.GestionAdministrativa.AreaRevisores.Infrastructure.Interfaces
{
    public interface IAreaRevisorRepository
    {
        /// <param name="userId">Usuario autenticado (app_user).</param>
        /// <param name="verTodas">true = ve todas las áreas y puede editarlas (roles ADMINISTRADOR DE SOLICITUD DE SALIDAS y USUARIO DE GTH).</param>
        Task<AreaRevisorInicialDto> GetInitialDataAsync(int userId, bool verTodas);

        /// <param name="projectId">null = revisores a nivel de área; con valor = revisores de ese proyecto dentro del área.</param>
        Task UpdateAreaRevisoresAsync(int areaScopeId, int? projectId, List<AreaRevisorAsignacionDto> revisores);

        /// <summary>Marca/desmarca "filtrar por proyecto" para el área (upsert en ga_salidas_area_config).</summary>
        Task SetFiltroProyectoAsync(int areaScopeId, bool filtraPorProyecto);
    }
}

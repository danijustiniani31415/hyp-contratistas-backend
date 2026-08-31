using Abril_Backend.Features.Evaluaciones.Application.Dtos;
using Abril_Backend.Features.Evaluaciones.Infrastructure.Models;

namespace Abril_Backend.Features.Evaluaciones.Application.Interfaces
{
    public interface IEvPrevencionistaRepository
    {
        Task<EvPrevencionistaInicioDto> GetInicioAsync(int evaluadorUserId, int evaluadorContributorId, List<int> proyectoIds);
        Task<int?> ResolverEvaluadorSsUsuarioIdAsync(int userId, int contributorId);

        /// <summary>
        /// Proyecto(s) actual(es) del supervisor de campo logueado: sale de la vinculación
        /// vigente (worker_vinculaciones, fecha_fin IS NULL) del worker/person ligado a su
        /// ss_contratista_usuario — solo el trabajador/persona sabe en qué obra está realmente
        /// hoy, así que esto reemplaza al claim estático "proyectoIds" del JWT (que salía de
        /// ss_contratista_usuario_proyecto y quedaba desactualizado apenas la persona rotaba
        /// de obra). Si el usuario no tiene worker_id (cuenta admin sin persona física detrás),
        /// cae de vuelta a la asignación estática histórica.
        /// </summary>
        Task<List<int>> ResolverProyectoIdsActualesAsync(int userId, int contractorId);
        Task<EvEvaluacionPrevencionista> CreateAsync(
            EvEvaluacionPrevencionista eval, List<EvEvaluacionPrevencionistaDetalle> detalles);
        Task<bool> ExisteAsync(int periodoId, int evaluadoUserId, int proyectoId, int evaluadorSsContratistaUsuarioId);
        Task<EvPrevencionistaMiPerfilDto> GetMiPerfilAsync(int evaluadoUserId, int? periodoId);

        /// <summary>
        /// Categoría del puesto actual del usuario (workers.puesto_id -> puesto.categoria_id).
        /// Reemplaza el antiguo gate por user_role (70/72) en "Mi perfil".
        /// </summary>
        Task<int?> ObtenerCategoriaPuestoAsync(int userId);

        /// <summary>
        /// true si el puesto actual del usuario es Jefe SSOMA (PuestoIds.JefeSsoma) — para
        /// la pantalla "solo Jefe SSOMA" (Dashboard consolidado).
        /// </summary>
        Task<bool> EsJefeSsomaAsync(int userId);
        Task<EvPrevencionistaDashboardDto> GetDashboardAsync(int? periodoId, int? proyectoId);

        /// <summary>
        /// Supervisores de campo de contratista (ss_contratista_usuario activos) con al
        /// menos un proyecto asignado — pool para el recordatorio consolidado. El alcance
        /// de proyecto normalmente sale del JWT (claim proyectoIds); acá se reconstruye
        /// desde ss_contratista_usuario_proyecto porque el cron no tiene ese token.
        /// </summary>
        Task<List<EvPrevencionistaCandidatoDto>> GetEvaluadoresCandidatosAsync();
    }
}

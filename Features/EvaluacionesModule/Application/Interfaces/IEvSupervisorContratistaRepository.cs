using Abril_Backend.Features.Evaluaciones.Application.Dtos;
using Abril_Backend.Features.Evaluaciones.Infrastructure.Models;

namespace Abril_Backend.Features.Evaluaciones.Application.Interfaces
{
    public interface IEvSupervisorContratistaRepository
    {
        Task<EvSupervisorContratistaInicioDto> GetInicioAsync(int evaluadorUserId);

        /// <summary>
        /// Categoría del puesto actual del usuario (workers.puesto_id -> puesto.categoria_id).
        /// Reemplaza el antiguo gate por user_role (70/72) en el controller.
        /// </summary>
        Task<int?> ObtenerCategoriaPuestoAsync(int userId);

        /// <summary>
        /// true si el puesto actual del usuario es Jefe SSOMA (PuestoIds.JefeSsoma).
        /// </summary>
        Task<bool> EsJefeSsomaAsync(int userId);
        Task<EvEvaluacionSupervisorContratista> CreateAsync(
            EvEvaluacionSupervisorContratista eval, List<EvEvaluacionSupervisorContratistaDetalle> detalles);
        Task<bool> ExisteAsync(int periodoId, int supervisorWorkerId, int evaluadorUserId);

        /// <summary>Para el guard de edición: dueño (EvaluadorUserId) y período de la evaluación.</summary>
        Task<EvEvaluacionSupervisorContratista?> ObtenerPorIdAsync(int id);

        /// <summary>Edita nota/comentario/detalles de una evaluación ya registrada — solo mientras
        /// su período siga activo (lo valida el controller antes de llamar acá).</summary>
        Task<EvEvaluacionSupervisorContratista> ActualizarAsync(
            int id, string? comentario, List<EvEvaluacionSupervisorContratistaDetalle> detalles);
        Task<bool> ExisteNoAplicaAsync(int periodoId, int evaluadorUserId);
        Task RegistrarNoAplicaAsync(
            int periodoId, int evaluadorUserId, string motivo,
            int? proyectoId = null, int? supervisorWorkerId = null);
        Task<EvSupervisorContratistaVerInicioDto> GetVerInicioAsync(int? periodoId, int? proyectoId);
        Task<EvSupervisorContratistaDashboardDto> GetDashboardAsync(int? periodoId, int? proyectoId);

        /// <summary>
        /// worker_id del propio supervisor/prevencionista logueado como contratista
        /// (ss_contratista_usuario.worker_id) — es la llave con la que se le busca en
        /// ev_evaluacion_supervisor_contratista.supervisor_worker_id, tenga o no cuenta
        /// logueada quien lo evaluó no importa acá: importa la suya propia.
        /// </summary>
        Task<int?> ResolverPropioWorkerIdAsync(int userId, int contributorId);
        Task<EvSupervisorContratistaMiPerfilDto> GetMiPerfilAsync(int supervisorWorkerId, int? periodoId);

        /// <summary>
        /// Coordinador SSOMA / Prevencionista activos con vinculación vigente — pool
        /// para el recordatorio consolidado (para quienes esta evaluación es función
        /// habitual; el Jefe SSOMA queda afuera porque para él es opcional).
        /// </summary>
        Task<List<EvaluadorDto>> GetEvaluadoresCandidatosAsync();
    }
}

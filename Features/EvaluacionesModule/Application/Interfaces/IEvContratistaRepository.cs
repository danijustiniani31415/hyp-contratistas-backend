using Abril_Backend.Features.Evaluaciones.Application.Dtos;
using Abril_Backend.Features.Evaluaciones.Infrastructure.Models;

namespace Abril_Backend.Features.Evaluaciones.Application.Interfaces
{
    public interface IEvContratistaRepository
    {
        Task<EvContratistaInicioDto> GetInicioAsync(int userId);
        Task<EvContratistaVerInicioDto> GetVerInicioAsync(int? periodoId, int? proyectoId);
        Task<EvContratistaDashboardDto> GetDashboardAsync(int? periodoId, int? proyectoId);
        Task<EvEvaluacionContratista> CreateAsync(EvEvaluacionContratista eval, List<EvEvaluacionContratistaDetalle> detalles);
        Task<bool> ExisteAsync(int periodoId, int proyectoId, int contributorId, string areaNombre, int evaluadorUserId);
        Task<bool> ExisteNoAplicaAsync(int periodoId, int evaluadorUserId);
        Task RegistrarNoAplicaAsync(
            int periodoId, int evaluadorUserId, string areaNombre, string motivo,
            int? proyectoId = null, int? contributorId = null);

        /// <summary>
        /// Todos los trabajadores activos cuya subárea corresponde a un área evaluadora de
        /// contratistas (SSOMA, Oficina Técnica, Producción, Calidad, Residencia) y que tienen
        /// al menos un proyecto asignado. Usado por el recordatorio automático mensual.
        /// </summary>
        Task<List<EvaluadorDto>> GetEvaluadoresCandidatosAsync();

        /// <summary>
        /// Agrupa por empresa (contributor) sus evaluaciones (por proyecto/área) y las de sus
        /// supervisores en el período dado — para el envío de resultados a los gerentes.
        /// </summary>
        Task<List<EmpresaResultadoEnvioDto>> GetResultadosParaEnvioAsync(int periodoId);
    }
}

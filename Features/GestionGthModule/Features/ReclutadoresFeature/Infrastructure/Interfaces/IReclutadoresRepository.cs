using Abril_Backend.Features.GestionGthModule.Features.ReclutadoresFeature.Application.Dtos;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutadoresFeature.Infrastructure.Interfaces
{
    /// <summary>Acceso a datos de la pantalla "Reclutadores".</summary>
    public interface IReclutadoresRepository
    {
        /// <summary>
        /// Todas las filas de la pantalla: los trabajadores vigentes del área de GTH más
        /// cualquier otro que ya tenga fila en la tabla filtro. Ordenadas por nombre.
        /// </summary>
        Task<List<ReclutadorDto>> GetReclutadoresAsync();

        /// <summary>
        /// Prende o apaga a un trabajador como reclutador. Crea la fila en
        /// <c>gth_responsable_proceso</c> la primera vez y solo actualiza el <c>active</c> las
        /// siguientes: nunca toca <c>workers</c> ni borra filas.
        /// </summary>
        Task<ReclutadorToggleResultDto> ToggleAsync(int workerId, bool activo, int? userId);
    }
}

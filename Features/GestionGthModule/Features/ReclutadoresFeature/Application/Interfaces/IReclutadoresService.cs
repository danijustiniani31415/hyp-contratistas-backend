using Abril_Backend.Features.GestionGthModule.Features.ReclutadoresFeature.Application.Dtos;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutadoresFeature.Application.Interfaces
{
    /// <summary>
    /// Reclutadores: qué trabajadores del área de GTH pueden llevar un proceso de selección.
    /// Es la única fuente del desplegable "Responsable del proceso" del detalle de Reclutamiento.
    /// </summary>
    public interface IReclutadoresService
    {
        /// <summary>Carga de la pantalla: la lista completa, en una sola petición.</summary>
        Task<List<ReclutadorDto>> GetReclutadores();

        /// <summary>Prende o apaga a un trabajador como reclutador.</summary>
        Task<ReclutadorToggleResultDto> Toggle(int workerId, bool activo, int? userId);
    }
}

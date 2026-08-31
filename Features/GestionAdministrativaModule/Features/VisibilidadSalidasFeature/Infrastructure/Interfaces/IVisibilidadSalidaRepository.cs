using Abril_Backend.Features.GestionAdministrativa.Shared.Dtos;
using Abril_Backend.Features.GestionAdministrativa.VisibilidadSalidas.Application.Dtos;

namespace Abril_Backend.Features.GestionAdministrativa.VisibilidadSalidas.Infrastructure.Interfaces
{
    public interface IVisibilidadSalidaRepository
    {
        /// <summary>Carga inicial: trabajadores (tabla) + árbol de áreas (filtro), en una sola conexión.</summary>
        Task<VisibilidadInicialDto> GetInitialDataAsync();

        /// <summary>Árbol de áreas (lista plana de nodos vivos).</summary>
        Task<List<GaAreaNodeDto>> GetAreaTreeAsync();

        /// <summary>Asignaciones vivas de un trabajador.</summary>
        Task<List<VisibilidadAsignacionDto>> GetWorkerAsignacionesAsync(int workerId);

        /// <summary>Reemplaza el conjunto de asignaciones de un trabajador.</summary>
        Task UpdateWorkerAsignacionesAsync(int workerId, List<VisibilidadAsignacionDto> asignaciones);
    }
}

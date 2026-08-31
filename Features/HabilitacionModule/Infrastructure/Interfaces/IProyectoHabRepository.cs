using Abril_Backend.Features.Habilitacion.Application.Dtos.Proyectos;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces
{
    public interface IProyectoHabRepository
    {
        Task<List<ProyectoSimpleDto>> GetActivosAsync();

        /// <summary>
        /// Proyectos seleccionables al programar una inducción. Usa su propio filtro
        /// (HABILITACION_INDUCCION) en vez del de Habilitación general, para que un proyecto pueda
        /// ser inducible sin aparecer en el filtro de la lista de trabajadores, en EMOs programados
        /// ni en la asignación de proyectos por empresa.
        /// </summary>
        Task<List<ProyectoSimpleDto>> GetActivosInduccionAsync();
    }
}

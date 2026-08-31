using Abril_Backend.Features.Evaluaciones.Infrastructure.Models;

namespace Abril_Backend.Features.Evaluaciones.Application.Interfaces
{
    public interface IEvPeriodoRepository
    {
        /// <summary>Período con la ventana de evaluación abierta ahora mismo (día 25 -> día 4). Solo para gatear el ENVÍO de evaluaciones/recordatorios.</summary>
        Task<EvPeriodo?> GetActivoAsync();

        /// <summary>Último período registrado (por año/mes), esté o no la ventana de evaluación abierta. Úsese para VISUALIZAR resúmenes/dashboards, que no deben depender de si hoy se puede evaluar.</summary>
        Task<EvPeriodo?> GetUltimoAsync();

        Task<List<EvPeriodo>> GetAllAsync();
        Task<EvPeriodo?> GetByIdAsync(int id);
        Task<EvPeriodo> CreateAsync(EvPeriodo periodo);
        Task UpdateAsync(EvPeriodo periodo);

        /// <summary>
        /// Desactiva períodos vencidos y crea/activa automáticamente el período
        /// vigente (ventana día 25 del mes -> día 4 del mes siguiente) si corresponde.
        /// Debe llamarse al inicio de cualquier proceso que dependa del período activo.
        /// </summary>
        Task SincronizarVigenciaAsync();
    }
}

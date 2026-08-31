using Abril_Backend.Features.UnidadDeProyectosModule.Features.CronogramaActividades.Application.Dtos;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.CronogramaActividades.Application.Interfaces
{
    /// <summary>
    /// Lógica de programación del cronograma: cálculo de días hábiles,
    /// detección de dependencias circulares, cascada Fin a Inicio (FS) y
    /// recálculo de fechas de nodos padre.
    /// </summary>
    public interface ICronogramaSchedulingService
    {
        DateTime AddBusinessDays(DateTime start, int days, List<DateTime> feriados);
        DateTime NextBusinessDay(DateTime date, List<DateTime> feriados);

        /// <summary>
        /// True si fijar <paramref name="nuevasPredecesoras"/> como predecesoras de
        /// <paramref name="activityId"/> crearía una dependencia circular.
        /// </summary>
        Task<bool> DetectCycleAsync(int proyectoId, int activityId, List<int> nuevasPredecesoras);

        /// <summary>Calcula la cascada FS sin persistir; devuelve qué actividades se moverían.</summary>
        Task<CascadaResultDto> RecalcularCascadaAsync(int proyectoId);

        /// <summary>Ejecuta la cascada FS, persiste los cambios y recalcula fechas de padres.</summary>
        Task<CascadaResultDto> AplicarCascadaAsync(int proyectoId);

        /// <summary>Recalcula y persiste inicio=MIN(hijos)/fin=MAX(hijos) para todo nodo padre. Devuelve los padres que cambiaron.</summary>
        Task<List<ActividadDto>> RecalcularFechasPadresAsync(int proyectoId);
    }
}

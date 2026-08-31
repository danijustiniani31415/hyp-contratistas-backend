namespace Abril_Backend.Features.Evaluaciones.Application.Interfaces
{
    public interface IEvRecordatorioService
    {
        Task<object> ProcesarRecordatoriosAsync();
        Task<object> ProcesarDescargoAsync();

        /// <summary>
        /// Punto de entrada único para el cron diario: corre recordatorios (residentes +
        /// contratistas) y descargo en una sola llamada, para no depender de varios cronjobs.
        /// </summary>
        Task<object> ProcesarDiarioAsync();
    }
}

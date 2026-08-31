using Abril_Backend.Features.Evaluaciones.Application.Dtos;

namespace Abril_Backend.Features.Evaluaciones.Application.Interfaces
{
    public interface IEvJefeSsomaRepository
    {
        Task<EvJefeSsomaInicioDto> GetInicioAsync(int evaluadorUserId);
        Task<bool> YaEvaluoAsync(int periodoId, int evaluadorUserId);

        /// <summary>
        /// Categoría del puesto actual del usuario (workers.puesto_id -> puesto.categoria_id),
        /// o null si no tiene worker propio o su puesto no tiene categoría. Reemplaza el
        /// antiguo chequeo por user_role (70/72): decide "es Coordinador SSOMA/Prevencionista"
        /// a partir del puesto real que ya mantiene Habilitación, sin depender de que alguien
        /// le asigne además un rol de sistema aparte.
        /// </summary>
        Task<int?> ObtenerCategoriaPuestoAsync(int userId);

        /// <summary>
        /// true si el puesto actual del usuario es Jefe SSOMA (PuestoIds.JefeSsoma) — usado
        /// para las pantallas "solo Jefe SSOMA" (Pendientes/Resultados), no para saber quién
        /// evalúa (eso es Coordinador SSOMA/Prevencionista, ver ObtenerCategoriaPuestoAsync).
        /// </summary>
        Task<bool> EsJefeSsomaPuestoAsync(int userId);

        /// <summary>
        /// Inserta la evaluación (sin autor) y la marca de cumplimiento (sin nota) en la
        /// misma transacción. Deliberadamente no hay forma de unir ambas filas después.
        /// </summary>
        Task RegistrarAsync(
            int periodoId, int evaluadorUserId, string? comentario,
            List<(int? plantillaId, string criterio, int puntaje)> detalles, decimal nota);

        Task<EvJefeSsomaCumplimientoDto> GetCumplimientoAsync(int periodoId);
        Task<EvJefeSsomaResultadosDto> GetResultadosAsync(int? periodoId);
    }
}

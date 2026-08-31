using Abril_Backend.Features.Evaluaciones.Application.Dtos;

namespace Abril_Backend.Features.Evaluaciones.Application.Interfaces
{
    public interface IEvGestionSsomaRepository
    {
        Task<EvGestionSsomaInicioDto> GetInicioAsync(int evaluadorUserId);

        /// <summary>
        /// Categoría del puesto actual del usuario (workers.puesto_id -> puesto.categoria_id),
        /// o null si no aplica. Coordinador SSOMA=41, Prevencionista=35 (CategoriaIds). Se usa
        /// en el controller para reemplazar el antiguo gate por user_role (70/72).
        /// </summary>
        Task<int?> ObtenerCategoriaPuestoAsync(int userId);

        /// <summary>
        /// true si el puesto actual del usuario es Jefe SSOMA (PuestoIds.JefeSsoma) — no
        /// depende de ningún user_role.
        /// </summary>
        Task<bool> EsJefeSsomaAsync(int userId);

        /// <summary>
        /// Determina, a partir del rol de quien llama, si la evaluación es D1/D2/D3
        /// (identificada, requiere evaluadoUserIdSolicitado) o D4 (anónima, el
        /// evaluado se resuelve solo — evaluadoUserIdSolicitado se ignora).
        /// Valida elegibilidad (rol del evaluado, mismo proyecto cuando corresponde).
        /// </summary>
        Task<EvGestionSsomaContextoDto> ResolverContextoEvaluacionAsync(int evaluadorUserId, int? evaluadoUserIdSolicitado);

        /// <summary>
        /// D1/D2/D3 — evaluación identificada (Jefe SSOMA o Coordinador SSOMA
        /// evaluando hacia abajo). Valida elegibilidad del evaluado antes de insertar.
        /// </summary>
        Task RegistrarAsync(
            int periodoId, int evaluadorUserId, string evaluadorRol,
            int evaluadoUserId, string evaluadoRol, int? proyectoId,
            string? fortalezas, string? oportunidadesMejora,
            List<(int? plantillaId, string criterio, int puntaje)> detalles, decimal nota);

        /// <summary>
        /// D4 — evaluación anónima (Prevencionista hacia su Coordinador SSOMA).
        /// Inserta la evaluación (sin autor) y la marca de cumplimiento (sin nota)
        /// en la misma transacción. Deliberadamente no hay forma de unir ambas filas después.
        /// </summary>
        Task RegistrarAnonimoAsync(
            int periodoId, int evaluadorUserId, int evaluadoUserId, int? proyectoId,
            string? fortalezas, string? oportunidadesMejora,
            List<(int? plantillaId, string criterio, int puntaje)> detalles, decimal nota);

        Task<bool> ExisteAsync(int periodoId, int evaluadorUserId, int evaluadoUserId);
        Task<bool> YaEvaluoAnonimoAsync(int periodoId, int evaluadorUserId);

        Task<EvGestionSsomaCumplimientoDto> GetCumplimientoAsync(int periodoId);
        Task<EvGestionSsomaResultadosDto> GetResultadosAsync(int? periodoId);
    }
}

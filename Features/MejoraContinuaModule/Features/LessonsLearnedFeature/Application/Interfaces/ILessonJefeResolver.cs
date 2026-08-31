namespace Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Interfaces
{
    /// <summary>
    /// Resuelve la jefatura para el flujo de aprobación de lecciones aprendidas.
    /// Reusa la idea de <c>ApproverResolver.ResolveApproverEmailAsync</c> (walk-up
    /// por el árbol <c>area_scope</c> a partir del <c>worker.area_scope_id</c>),
    /// pero SOLO considera hasta la categoría "Jefe" (los autores siempre son de
    /// categoría inferior a Jefe), y devuelve identidades (user_id) en lugar de correos.
    /// </summary>
    public interface ILessonJefeResolver
    {
        /// <summary>
        /// user_id del Jefe del autor (el "Jefe" más cercano caminando hacia arriba
        /// en el árbol de áreas). null si no se puede resolver.
        /// </summary>
        Task<int?> ResolveJefeUserIdAsync(int autorUserId);

        /// <summary>
        /// user_ids de los trabajadores cuyo Jefe-más-cercano es <paramref name="jefeUserId"/>.
        /// Usado por el filtro "Pendientes de mi revisión".
        /// </summary>
        Task<List<int>> GetSubordinateUserIdsAsync(int jefeUserId);

        /// <summary>
        /// Regla de alcance del REVISOR según su categoría:
        ///   • Residente → SOLO puede revisar lecciones de los proyectos que tiene
        ///     asignados en user_project (worker). Si <paramref name="projectId"/> no
        ///     es uno de ellos → false.
        ///   • Cualquier otra categoría → true (sin restricción de proyecto).
        /// </summary>
        Task<bool> CanReviewProjectAsync(int reviewerUserId, int? projectId);

        /// <summary>
        /// Si el revisor es Residente, devuelve los project_id que tiene asignados en
        /// user_project (para acotar el listado "Pendientes de mi revisión"). Devuelve
        /// null si NO es Residente (sin restricción de proyecto).
        /// </summary>
        Task<List<int>?> GetResidenteProjectScopeAsync(int reviewerUserId);
    }
}

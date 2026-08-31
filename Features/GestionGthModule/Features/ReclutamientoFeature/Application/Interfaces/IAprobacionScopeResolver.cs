namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Interfaces
{
    /// <summary>
    /// Alcance de un usuario dentro de «Aprobaciones»: con qué poder decide y qué solicitudes ve.
    /// </summary>
    /// <param name="Nivel">
    /// <see cref="Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.AprobacionNivel"/>:
    /// GERENTE_GENERAL, GERENTE_AREA, GTH o NINGUNO.
    /// </param>
    /// <param name="VeTodo">
    /// true para el Gerente General y para GTH: no se filtra por área. El GG porque decide todas
    /// las vacantes nuevas de la empresa y GTH porque decide todos los reemplazos. Cuando es true,
    /// <paramref name="AreaScopeIds"/> viene vacío (no hay nada que filtrar).
    /// </param>
    /// <param name="AreaScopeIds">
    /// Nodos de <c>area_scope</c> cuyas solicitudes puede ver y decidir: el área del gerente y todo
    /// su subárbol. Vacío cuando <paramref name="VeTodo"/> es true o cuando el nivel es NINGUNO
    /// (en ese último caso, vacío significa literalmente "no ve nada").
    /// </param>
    /// <param name="AreaNombre">
    /// Nombre del área del gerente, para poder explicarle en pantalla de dónde sale su alcance.
    /// Null si es Gerente General o si no tiene área asignada.
    /// </param>
    public record AprobacionScope(string Nivel, bool VeTodo, HashSet<int> AreaScopeIds, string? AreaNombre)
    {
        /// <summary>Alcance de quien no es gerente de nada: entra a la pantalla y la ve vacía.</summary>
        public static AprobacionScope Ninguno() =>
            new(AprobacionNivel.Ninguno, false, new HashSet<int>(), null);

        /// <summary>true si el usuario puede aprobar o rechazar algo (cualquiera de los dos niveles).</summary>
        public bool PuedeDecidir => Nivel != AprobacionNivel.Ninguno;

        /// <summary>
        /// ¿La solicitud de este <c>area_scope</c> entra en el alcance del usuario? Una solicitud
        /// sin área (<c>area_scope_id</c> null, cuando no se pudo resolver al registrarla) solo la
        /// ve el Gerente General: no hay forma de decir a qué gerencia pertenece.
        /// </summary>
        public bool Alcanza(int? areaScopeId) =>
            VeTodo || (areaScopeId.HasValue && AreaScopeIds.Contains(areaScopeId.Value));
    }

    /// <summary>
    /// Resuelve el alcance de un usuario en «Aprobaciones» a partir de la categoría de su ficha de
    /// trabajador. El acceso a la pantalla lo da el rol (<c>role_feature</c>); esto decide qué ve
    /// y qué puede decidir dentro de ella.
    /// </summary>
    public interface IAprobacionScopeResolver
    {
        Task<AprobacionScope> ResolveAsync(int? userId);
    }
}

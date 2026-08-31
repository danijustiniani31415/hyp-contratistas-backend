namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Interfaces
{
    /// <summary>
    /// Alcance de un usuario dentro de «Solicitud de Personal»: qué requerimientos ve y si puede
    /// moverlos. Es el equivalente de <see cref="AprobacionScope"/> para la pantalla del
    /// solicitante, pero con otras reglas: allá se decide con la firma de cada quien, acá se
    /// trabaja el requerimiento del ÁREA.
    /// </summary>
    /// <param name="UserId">
    /// Quien pregunta. Sus propios requerimientos los ve siempre, aunque después lo hayan cambiado
    /// de área o su ficha se haya quedado sin ninguna.
    /// </param>
    /// <param name="VeTodo">
    /// true para el Gerente General y para GTH: no se filtra por área. Cuando es true,
    /// <paramref name="AreaScopeIds"/> viene vacío (no hay nada que filtrar).
    /// </param>
    /// <param name="AreaScopeIds">
    /// Nodos de <c>area_scope</c> cuyos requerimientos ve: el área de su ficha y todo el subárbol
    /// que cuelga de ella. Por eso alguien de una gerencia alcanza lo que pidieron sus áreas hijas,
    /// y no al revés. Vacío cuando <paramref name="VeTodo"/> es true o cuando la ficha no tiene
    /// área (entonces solo ve lo suyo).
    /// </param>
    /// <param name="PuedeGestionar">
    /// true solo para JEFE, GERENTE y GERENTE GENERAL: son las categorías que registran solicitudes
    /// y avanzan el proceso (decidir la long list, decidir al finalista, reenviar la aprobación).
    /// El resto del área entra a la pantalla y hace seguimiento, pero no mueve nada.
    /// </param>
    public record SolicitudPersonalScope(
        int UserId, bool VeTodo, HashSet<int> AreaScopeIds, bool PuedeGestionar)
    {
        /// <summary>
        /// Alcance de quien no tiene ficha de trabajador vigente: solo lo que él mismo registró, y
        /// sin poder moverlo. Sin ficha no hay área ni categoría de la cual deducir nada.
        /// </summary>
        public static SolicitudPersonalScope SoloLoSuyo(int userId) =>
            new(userId, false, new HashSet<int>(), false);
    }

    /// <summary>
    /// Resuelve el alcance de un usuario en «Solicitud de Personal» a partir de su ficha de
    /// trabajador (área y categoría del puesto). El acceso a la pantalla lo da el rol
    /// (<c>role_feature</c>); esto decide qué requerimientos ve dentro de ella y cuáles puede
    /// mover.
    /// </summary>
    public interface ISolicitudPersonalScopeResolver
    {
        Task<SolicitudPersonalScope> ResolveAsync(int userId);
    }
}

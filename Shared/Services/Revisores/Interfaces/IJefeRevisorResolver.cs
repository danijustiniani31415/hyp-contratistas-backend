namespace Abril_Backend.Shared.Services.Revisores.Interfaces
{
    /// <summary>
    /// Resuelve el jefe/revisor de un trabajador:
    ///   1) Su jefe personalizado — el primer revisor vivo (state) y activo (active) del
    ///      trabajador en <c>workers_revisores</c>, por orden_prioridad ascendente, cuyo
    ///      worker tenga correo corporativo @abril.pe. Se asigna con el checkbox
    ///      "Jefe personalizado" del formulario de trabajadores (Gestión de Ingresos) y
    ///      se sobrepone al revisor del área.
    ///   2) Los revisores del área del trabajador en <c>area_revisores</c>
    ///      (/configuracion/revisores-areas): se parte
    ///      de su nodo workers.area_scope_id y se sube por el árbol hasta el primer
    ///      nodo con un revisor vivo + activo con correo válido (por prioridad).
    ///   3) Fallback: el área de GTH — nodo <c>area_scope</c> del área
    ///      "Gestión del Talento Humano" con <c>email</c> configurado.
    ///
    /// En los tres pasos rige la misma regla: <b>nadie puede ser su propio jefe</b>. Un candidato
    /// que es el propio trabajador se descarta y la búsqueda sigue — con el siguiente revisor del
    /// mismo nodo si lo hay y, si no, subiendo al <c>area_scope</c> padre (normalmente la gerencia
    /// de la que cuelga su área). Esto es lo normal en los jefes de área: el jefe de SSOMA es el
    /// revisor de SSOMA, así que su propio jefe es el gerente del que depende esa área. La
    /// comparación es por PERSONA, no por ficha: un reingreso deja varias filas en
    /// <c>workers</c> para la misma persona y el revisor puede estar configurado en cualquiera.
    ///
    /// Servicio compartido: es la ÚNICA fuente de "quién es el jefe de este trabajador".
    /// Lo usan Gestión Administrativa (a quién se le manda a aprobar una solicitud de
    /// salida) y SSOMA · Salud Ocupacional (correos de EMO e interconsultas), y
    /// Evaluaciones (a qué jefe se le hace CC del recordatorio). Reemplazó a tres
    /// algoritmos previos: el cruce por nombre contra <c>cat_jefatura</c>, el campo 1:1
    /// <c>workers.worker_salida_jefe_id</c> y el recorrido del árbol de áreas por
    /// categoría de trabajador (ApproverResolver / JefeResolver).
    /// </summary>
    public interface IJefeRevisorResolver
    {
        /// <summary>Jefe/revisor de un trabajador, o null si no se resuelve ninguno.</summary>
        Task<JefeRevisorResolution?> ResolveAsync(int workerId);

        /// <summary>
        /// Versión por lotes: resuelve el jefe de varios trabajadores con un número FIJO de
        /// consultas (no depende de la cantidad de ids), para listas y envíos masivos.
        /// Los trabajadores sin jefe resuelto simplemente no aparecen en el diccionario.
        /// </summary>
        Task<Dictionary<int, JefeRevisorResolution>> ResolveManyAsync(IReadOnlyCollection<int> workerIds);

        /// <summary>
        /// Previsualización por ÁREA, sin trabajador: para cada nodo <c>area_scope</c> pedido devuelve
        /// los revisores que le tocarían a un trabajador ubicado ahí, aplicando los pasos 2 y 3 (revisores
        /// del área subiendo por el árbol, y fallback GTH). No aplica el paso 1 (<c>workers_revisores</c>)
        /// porque no hay trabajador: un trabajador con revisor propio configurado usa ese y no el del área.
        ///
        /// Devuelve los candidatos EN ORDEN de resolución en vez de solo el ganador, justamente porque
        /// no hay trabajador: quien consume esto sí lo conoce y tiene que poder descartarlo para que
        /// nadie salga como su propio jefe (el formulario de trabajadores muestra el primer candidato
        /// que no sea el trabajador que se está editando). El árbol se pide una sola vez y sirve para
        /// cualquier trabajador, sin volver al servidor al cambiar de área.
        ///
        /// La usa el formulario de trabajadores para mostrar, al elegir el área, quién quedaría como su
        /// revisor. Un número FIJO de consultas sea para 1 o para todos los nodos del árbol.
        /// </summary>
        Task<Dictionary<int, AreaScopeRevisorPreview>> ResolveByAreaScopeManyAsync(
            IReadOnlyCollection<int> areaScopeIds);
    }

    /// <summary>
    /// Revisores que le tocarían a un trabajador de un nodo del árbol de áreas, en orden de
    /// resolución. Se separa el caso sin proyecto del caso por proyecto porque hay nodos marcados
    /// como "filtrar por proyecto" (ga_salidas_area_config): ahí el revisor depende del proyecto del
    /// trabajador, así que se precalcula una lista por proyecto configurado y el consumidor elige
    /// según el proyecto del formulario.
    /// </summary>
    /// <param name="Area">
    /// Candidatos a nivel de área en orden (nodo más cercano primero, luego sus ancestros) con el
    /// fallback GTH al final. Vacía si no hay ninguno.
    /// </param>
    /// <param name="PorProyecto">projectId -> candidatos en orden, solo para los proyectos con revisor propio en la rama.</param>
    public record AreaScopeRevisorPreview(
        IReadOnlyList<JefeRevisorResolution> Area,
        Dictionary<int, IReadOnlyList<JefeRevisorResolution>> PorProyecto);

    /// <summary>
    /// Jefe/revisor resuelto: un trabajador (WorkerId) o un área (AreaScopeId, el
    /// fallback de GTH) — exactamente uno de los dos — con el correo a usar y el nombre
    /// para mostrar (nombre completo del revisor, o el del área en el fallback).
    /// </summary>
    /// <param name="PersonId">
    /// Persona del revisor (<c>workers.person_id</c>), null en el fallback de área. Es con lo que se
    /// aplica "nadie puede ser su propio jefe": la misma persona puede tener varias fichas en
    /// <c>workers</c> y comparar solo por ficha dejaría pasar el caso.
    /// </param>
    public record JefeRevisorResolution(
        int? WorkerId, int? AreaScopeId, string Email, string? Nombre = null, int? PersonId = null);
}

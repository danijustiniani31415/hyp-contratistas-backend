namespace Abril_Backend.Shared.Services.AreaScope.Interfaces
{
    /// <summary>
    /// Equivalencia legacy de un nodo del árbol <c>area_scope</c>: los campos de texto plano
    /// <c>workers.area</c> / <c>workers.subarea</c> / <c>workers.jefatura</c> que siguen
    /// existiendo en la tabla y que consumen listados y reportes antiguos.
    /// Todos pueden ser null cuando el nodo no tiene equivalente (una gerencia, por ejemplo).
    /// </summary>
    public record AreaLegacyDatos(string? Area, string? Subarea, string? Jefatura);

    /// <summary>
    /// Traduce un nodo del árbol <c>area_scope</c> a los campos legacy area/subarea/jefatura.
    ///
    /// Es la dirección INVERSA de <see cref="Services.AreaScopeMatcher"/>: ese derivaba el nodo a
    /// partir del texto que capturaba el formulario, y este deriva el texto a partir del nodo, que
    /// es lo que el formulario de trabajadores captura hoy (un solo dato, el nodo del árbol, en vez
    /// de dos textos que había que hacer coincidir).
    ///
    /// La resolución sube por el árbol desde el nodo elegido hasta la raíz y se queda con el primer
    /// nodo que tenga equivalente:
    ///   1) el mapa curado <see cref="Services.AreaScopeMatcher.ScopeToSubarea"/> (verificado
    ///      contra los trabajadores de producción que ya tenían ambos campos poblados), y
    ///   2) si no está ahí, el nombre del nodo cruzado contra <c>cat_subarea.subarea</c>
    ///      (normalizado), que cubre los nodos agregados después por UI.
    /// El área y la jefatura salen siempre de la fila de <c>cat_subarea</c> encontrada, así que
    /// nunca se inventan combinaciones que no estén en el catálogo.
    /// </summary>
    public interface IAreaScopeLegacyResolver
    {
        /// <summary>Equivalencia del nodo indicado, o null si el nodo no existe / no tiene equivalente.</summary>
        Task<AreaLegacyDatos?> ResolveAsync(int? areaScopeId);

        /// <summary>
        /// Equivalencia de todos los nodos vivos del árbol, en una sola pasada. Para el endpoint
        /// que alimenta los desplegables (así el formulario puede mostrar a qué área/subárea
        /// legacy va a caer cada nodo sin una petición por nodo).
        /// </summary>
        Task<Dictionary<int, AreaLegacyDatos>> ResolveTodosAsync();
    }
}

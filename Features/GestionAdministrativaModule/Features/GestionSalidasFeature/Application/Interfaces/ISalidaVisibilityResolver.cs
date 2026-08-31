namespace Abril_Backend.Features.GestionAdministrativa.GestionSalidas.Application.Interfaces
{
    /// <summary>
    /// Resuelve el alcance de visibilidad de un usuario en la gestión de salidas:
    /// qué nodos <c>area_scope</c> puede ver (además de las solicitudes donde él es el
    /// aprobador). Al conjunto siempre se le suman los nodos (con su subárbol) donde el
    /// usuario está designado como revisor de área (<c>area_revisores</c>); sobre eso,
    /// primero mira el override manual (ga_salida_visibilidad_area) y, si el usuario no
    /// tiene ninguna asignación, cae al algoritmo de jerarquía.
    /// </summary>
    public interface ISalidaVisibilityResolver
    {
        Task<SalidaVisibility> ResolveAsync(int userId);
    }

    /// <summary>
    /// Resultado de la resolución de visibilidad.
    ///   • <see cref="SeesAll"/> = true  → ve TODAS las solicitudes (sin restricción por área).
    ///   • <see cref="AreaScopeIds"/>     → conjunto de nodos cuyos trabajadores puede ver.
    ///   • <see cref="EsCategoriaTesorero"/> → alguno de sus workers tiene un puesto de categoría
    ///     Tesorero. Es la MITAD de la condición para entrar como tesorero: la otra mitad es el rol,
    ///     que sale del token y lo aporta el controller. Se devuelve desde acá porque este resolver
    ///     ya trae la categoría de los workers del usuario y sería un roundtrip extra pedirla aparte.
    /// </summary>
    public record SalidaVisibility(bool SeesAll, HashSet<int> AreaScopeIds, bool EsCategoriaTesorero = false);
}

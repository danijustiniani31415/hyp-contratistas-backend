namespace Abril_Backend.Shared.Constants
{
    /// <summary>
    /// IDs del catálogo <c>puesto</c> sobre los que hay lógica de negocio que no se puede
    /// resolver por categoría (ver <see cref="CategoriaIds"/>) porque el puesto no tiene una
    /// categoría propia — comparte una genérica con puestos de otras áreas.
    /// </summary>
    public static class PuestoIds
    {
        /// <summary>
        /// Jefe de Seguridad y Salud en el Trabajo ("Jefe SSOMA"). A diferencia de
        /// Coordinador SSOMA (categoria_id 41) y Prevencionista (categoria_id 35), este
        /// puesto no tiene categoría propia: su categoría es la genérica "JEFE" (17), que
        /// también usan jefaturas de cualquier otra área (Costos, RRHH, etc.), así que no
        /// sirve para identificarlo. Solo existe un puesto con este nombre en el catálogo,
        /// así que se referencia directo por su id — tan estable como una categoría, y
        /// evita depender de un rol de sistema (antes role_id 9) que en la práctica estaba
        /// asignado a ~50 cuentas de todas las áreas y no reflejaba quién es realmente el
        /// Jefe SSOMA.
        /// </summary>
        public const int JefeSsoma = 189;
    }
}

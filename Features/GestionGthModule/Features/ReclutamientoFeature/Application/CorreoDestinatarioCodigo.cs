namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application
{
    /// <summary>
    /// Códigos estables de los destinatarios <b>dinámicos</b> de los correos de Reclutamiento
    /// (espejo de <c>gth_correo_destinatario.codigo</c>). Son filas de configuración que no
    /// guardan un correo: se resuelve al momento de enviar, así que siguen al dato maestro si
    /// la persona cambia. Los correos adicionales escritos a mano tienen <c>codigo = null</c>.
    /// </summary>
    public static class CorreoDestinatarioCodigo
    {
        /// <summary>
        /// Gerente General: correo corporativo del trabajador ACTIVO cuya categoría es
        /// "GERENTE GENERAL" (distinta de la genérica "GERENTE" de los gerentes de área).
        /// </summary>
        public const string GerenteGeneral = "GERENTE_GENERAL";

        /// <summary>
        /// Gerente del área del solicitante: trabajador de categoría "GERENTE" que cuelga del
        /// área del solicitante o de alguno de sus padres en el árbol de <c>area_scope</c>.
        /// Necesita el <c>areaScopeId</c> del solicitante, así que solo se puede expandir en los
        /// correos que lo tienen (hoy, el de aprobación).
        /// </summary>
        public const string GerenteArea = "GERENTE_AREA";

        /// <summary>
        /// Área de Gestión del Talento Humano: <c>area_scope.email</c> del nodo de GTH, para que
        /// siga a lo que se configure en Configuración → Áreas.
        /// </summary>
        public const string GthArea = "GTH_AREA";

        /// <summary>
        /// Área de Tecnología de la Información: <c>area_scope.email</c> del nodo de TI, misma
        /// idea que <see cref="GthArea"/>. El buzón de TI no se cablea en el código: si cambia, se
        /// cambia en Configuración → Áreas y todos los correos que lo usan lo siguen.
        /// </summary>
        public const string TiArea = "TI_AREA";

        /// <summary>
        /// Destinatarios que necesitan el área del solicitante para resolverse. Un correo que no
        /// tiene esa información los omite en vez de mandar algo incompleto.
        /// </summary>
        public static bool RequiereAreaSolicitante(string? codigo) =>
            string.Equals(codigo, GerenteArea, StringComparison.OrdinalIgnoreCase);
    }
}

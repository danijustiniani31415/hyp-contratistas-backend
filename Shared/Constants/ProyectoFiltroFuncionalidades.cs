namespace Abril_Backend.Shared.Constants
{
    /// <summary>
    /// IDs fijos del catálogo <c>proyecto_filtro_funcionalidad</c> (espejo del seed en
    /// <c>Migrations_Manual/proyecto_filtro_por_funcionalidad.sql</c>). Mismo criterio que
    /// <see cref="Roles"/>: se usa el ID (estable) y no el nombre/código editable.
    ///
    /// Uso en queries de listado de proyectos:
    /// <c>&amp;&amp; !ctx.ProyectoFiltro.Any(f => f.ProjectId == p.ProjectId
    ///     &amp;&amp; f.FuncionalidadId == ProyectoFiltroFuncionalidades.X &amp;&amp; !f.Active)</c>
    /// (en SQL crudo/Dapper se puede unir por <c>codigo</c>, que también es estable).
    /// </summary>
    public static class ProyectoFiltroFuncionalidades
    {
        public const int AcActividades       = 1;  // AC_ACTIVIDADES — Arquitectura Comercial: Actividades/Gantt/Dashboard
        public const int AcObservaciones     = 2;  // AC_OBSERVACIONES
        public const int AcRevisiones        = 3;  // AC_REVISIONES
        public const int Habilitacion        = 4;  // HABILITACION
        public const int SharedFilters       = 5;  // SHARED_FILTERS — EMOs, Indicadores Proactivos, Charlas, etc.
        public const int UdpCronograma       = 6;  // UDP_CRONOGRAMA
        public const int UdpDashboard        = 7;  // UDP_DASHBOARD
        public const int VecinosGestion      = 8;  // VECINOS_GESTION
        public const int VecinosCroquis      = 9;  // VECINOS_CROQUIS
        public const int LeccionesAprendidas = 10; // LECCIONES_APRENDIDAS
        public const int Residentes          = 11; // RESIDENTES — proyectos, incidencias y monitoreo
        public const int SsomaAmonestaciones = 12; // SSOMA_AMONESTACIONES
        public const int SsomaAccidentes     = 13; // SSOMA_ACCIDENTES — Flash Report

        /// <summary>
        /// HABILITACION_INDUCCION — solo el desplegable de proyecto del modal "Programar Inducción".
        /// Es un filtro aparte de <see cref="Habilitacion"/> (que cubre el resto de Habilitación:
        /// filtro de la lista de trabajadores, EMOs programados y proyectos por empresa) porque hay
        /// proyectos que deben ser inducibles sin aparecer en esos otros desplegables — el caso que
        /// lo motivó es Oficina Central (project_id 36).
        /// </summary>
        public const int HabilitacionInduccion = 14; // HABILITACION_INDUCCION
        public const int ControlLicencias      = 15; // CONTROL_LICENCIAS — Vecinos: Control de Licencias
    }
}

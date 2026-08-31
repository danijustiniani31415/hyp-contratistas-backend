namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application
{
    /// <summary>
    /// Nivel con el que un usuario entra a la pantalla «Aprobaciones». Sale de la CATEGORÍA de su
    /// ficha de trabajador (<c>workers.puesto_id → puesto.categoria_id</c>), no de su rol: el rol solo abre la
    /// pantalla; la categoría define qué solicitudes ve y con qué poder decide.
    ///
    /// No es un catálogo de base de datos: son los dos actores del flujo, fijos por diseño, y el
    /// tercer valor es la ausencia de ambos. Lo que sí vive en BD son las categorías a las que
    /// mapean (ver <see cref="Abril_Backend.Shared.Constants.CategoriaIds"/>).
    /// </summary>
    public static class AprobacionNivel
    {
        /// <summary>
        /// Gerencia General (<c>categoria_id</c> = GERENTE GENERAL). Ve TODAS las solicitudes y
        /// decide las vacantes de ruta <see cref="RutaAprobacion.GerenciaGeneral"/> — las nuevas
        /// que no son un ingreso directo. Su firma sola las mueve y dispara el correo a GTH.
        ///
        /// Los FFT ya no son suyos: un ingreso directo no lo firma nadie (ruta
        /// <see cref="RutaAprobacion.Ninguna"/>). Los que quedaron esperando su firma desde antes
        /// de ese cambio los sigue decidiendo él.
        /// </summary>
        public const string GerenteGeneral = "GERENTE_GENERAL";

        /// <summary>
        /// Gerente de área (<c>categoria_id</c> = GERENTE). Ve las solicitudes de su
        /// <c>area_scope</c> hacia abajo y decide las vacantes de ruta
        /// <see cref="RutaAprobacion.AreaYGth"/> — los reemplazos. Firma <b>primero</b>: su visto
        /// bueno es lo que le abre el turno a GTH, y su rechazo cierra la vacante sin que GTH
        /// llegue a verla.
        ///
        /// De los requerimientos nuevos solo se entera: los decide Gerencia General y a él le
        /// llega un correo informativo (<see cref="CorreoTipoReclutamiento.AvisoGerenteArea"/>)
        /// que no lo mete en «Aprobaciones».
        /// </summary>
        public const string GerenteArea = "GERENTE_AREA";

        /// <summary>
        /// Gestión del Talento Humano: cualquier trabajador ACTIVO cuyo <c>area_scope_id</c> sea el
        /// nodo de GTH (<see cref="Abril_Backend.Shared.Constants.AreaScopeIds.GestionDelTalentoHumano"/>).
        /// Es el único nivel que NO sale de la categoría del puesto sino del área: GTH no aprueba
        /// como jefatura sino como el área dueña del proceso, y quien esté ahí adentro sirve.
        ///
        /// Decide las vacantes de ruta <see cref="RutaAprobacion.AreaYGth"/> — los reemplazos —
        /// <b>después</b> del gerente del área: una vacante de reemplazo avanza recién con las DOS
        /// firmas y GTH pone la segunda. Hasta que el área no aprueba, la vacante no aparece en su
        /// bandeja ni le llega correo (ver <see cref="RutaAprobacion.LeTocaAhora"/>). Ve los
        /// reemplazos de toda la empresa, no solo los de su área.
        /// </summary>
        public const string Gth = "GTH";

        /// <summary>
        /// Cualquier otro caso. Tiene acceso a la pantalla por su rol, pero no hay solicitudes
        /// bajo su alcance: la ve vacía y no puede decidir nada.
        /// </summary>
        public const string Ninguno = "NINGUNO";
    }
}

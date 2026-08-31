namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Bitácora de estados de un requerimiento (tabla <c>gth_requerimiento_estado_historial</c>):
    /// una fila por cada vez que la vacante cambió de fase, con quién la movió y cuándo.
    ///
    /// Existe porque <c>gth_requerimiento</c> solo guarda el estado ACTUAL y el último
    /// <c>updated_user_id</c>/<c>updated_date_time</c>: al pasar de PUBLICACION a LONG_LIST se
    /// perdía quién la había publicado y a qué hora, y con eso la única pregunta que le importa a
    /// GTH — cuánto se demora cada paso del proceso — no tenía respuesta.
    ///
    /// Las filas las escribe <c>RequerimientoEstadoHistorialInterceptor</c> en el mismo
    /// <c>SaveChanges</c> que mueve la vacante, así que ningún cambio de estado puede quedarse
    /// fuera: no hay que acordarse de registrar nada al agregar una fase nueva al pipeline.
    ///
    /// La fila es inmutable —es un hecho pasado—: por eso no tiene <c>updated_*</c>. Corregirla
    /// sería reescribir la auditoría.
    ///
    /// El tiempo que la vacante estuvo en cada estado es la diferencia entre esta fila y la
    /// siguiente del mismo requerimiento (<c>LEAD(cambio_date_time)</c>); en la última fila —el
    /// estado actual— el estado sigue corriendo.
    /// </summary>
    public class GthRequerimientoEstadoHistorial
    {
        public int GthRequerimientoEstadoHistorialId { get; set; }

        public int GthRequerimientoId { get; set; }

        /// <summary>
        /// El requerimiento al que pertenece la fila. La escribe el interceptor por navegación y no
        /// por id: cuando la vacante se está creando en este mismo <c>SaveChanges</c> todavía no
        /// tiene id, y EF solo lo puede propagar por acá.
        /// </summary>
        public GthRequerimiento? Requerimiento { get; set; }

        /// <summary>
        /// FK a <c>gth_estado_requerimiento</c>: la fase de la que SALIÓ. Null solo en la primera
        /// fila de cada requerimiento (el alta: no venía de ninguna parte).
        /// </summary>
        public int? EstadoAnteriorId { get; set; }

        /// <summary>FK a <c>gth_estado_requerimiento</c>: la fase a la que ENTRÓ con este cambio.</summary>
        public int GthEstadoRequerimientoId { get; set; }

        /// <summary>
        /// Momento del cambio, en UTC. Es la hora del cambio, no la de grabado de la fila: en las
        /// filas <see cref="Reconstruido"/> son distintas.
        /// </summary>
        public DateTimeOffset CambioDateTime { get; set; }

        /// <summary>
        /// Usuario (<c>app_user</c>) que movió la vacante a este estado. Null cuando el cambio no
        /// salió de una sesión —la reconstrucción histórica y los procesos automáticos— y por eso
        /// no es FK, igual que el resto de <c>*_user_id</c> del proyecto.
        /// </summary>
        public int? CambioUserId { get; set; }

        /// <summary>
        /// true = fila deducida de <c>created_*</c>/<c>updated_*</c> por la migración que creó esta
        /// tabla, no capturada en el momento del cambio. Los requerimientos anteriores a la bitácora
        /// no tienen traza real, y sin esta marca sus tiempos se leerían como si la tuvieran: un
        /// requerimiento que pasó por seis fases aparece con dos filas y duraciones inventadas.
        /// Todo cálculo de demoras debería filtrarlas o tratarlas aparte.
        /// </summary>
        public bool Reconstruido { get; set; }

        /// <summary>Cuándo se grabó la fila (en las filas en vivo coincide con <see cref="CambioDateTime"/>).</summary>
        public DateTimeOffset CreatedDateTime { get; set; }

        public bool Active { get; set; } = true;
        public bool State { get; set; } = true;
    }
}

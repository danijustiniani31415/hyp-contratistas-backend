namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Decisión sobre una vacante concreta de la solicitud (tabla <c>gth_aprobacion_gg_detalle</c>):
    /// una fila por requerimiento de la <see cref="GthAprobacionGg"/>.
    ///
    /// Guarda las DOS decisiones de esa vacante, una por nivel: la del gerente del área (visto
    /// bueno) y la del Gerente General (la que manda). Son columnas separadas a propósito — el
    /// gerente del área puede rechazar una vacante que el GG termine aprobando, y las dos posturas
    /// tienen que quedar registradas.
    ///
    /// Es el registro de auditoría de lo decidido, aparte del estado que el requerimiento vaya
    /// tomando después (una vacante aprobada sigue avanzando por el pipeline, así que su estado
    /// deja de reflejar esta decisión).
    /// </summary>
    public class GthAprobacionGgDetalle
    {
        public int GthAprobacionGgDetalleId { get; set; }

        public int GthAprobacionGgId { get; set; }
        public int GthRequerimientoId { get; set; }

        /// <summary>
        /// Decisión de Gerencia General: true = aprobada; false = rechazada; null = todavía no
        /// decide. Es la que mueve el requerimiento y la que se le informa a GTH.
        /// </summary>
        public bool? AprobadoGerenteGeneral { get; set; }

        public DateTimeOffset? GerenteGeneralDecididoDateTime { get; set; }

        /// <summary>
        /// Visto bueno del gerente del área: true = aprobada; false = rechazada; null = no opinó.
        /// No condiciona el avance de la vacante.
        /// </summary>
        public bool? AprobadoGerenteArea { get; set; }

        public DateTimeOffset? GerenteAreaDecididoDateTime { get; set; }

        /// <summary>
        /// Decisión de GTH sobre esta vacante: true = aprobada; false = rechazada; null = no
        /// decidió. Solo aplica a las vacantes de ruta <c>AREA_GTH</c> (reemplazos no-FFT), donde va
        /// de la mano con <see cref="AprobadoGerenteArea"/>: la vacante avanza con las dos en true y
        /// se rechaza con que una sola diga false.
        /// </summary>
        public bool? AprobadoGth { get; set; }

        public DateTimeOffset? GthDecididoDateTime { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; } = true;
        public bool State { get; set; } = true;
    }
}

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Aprobación de una solicitud de personal (tabla <c>gth_aprobacion_gg</c>): 1:1 con
    /// <see cref="GthSolicitud"/> entre las vigentes.
    ///
    /// Es el primer paso del flujo: al registrar la solicitud sale UN SOLO correo con todas las
    /// vacantes al Gerente General y al gerente del área del solicitante, con un enlace a la
    /// pantalla «Aprobaciones». Ahí cada uno decide vacante por vacante.
    ///
    /// Hay DOS casillas independientes sobre la misma solicitud (por eso el sufijo "gg" del nombre
    /// de la tabla ya queda corto; se conserva por compatibilidad):
    ///   • <b>Gerente del área</b> (<c>EstadoGerenteArea*</c> / <c>GerenteArea*</c>): visto bueno
    ///     del gerente cuyo <c>area_scope</c> contiene al solicitante. Es redundante por diseño —
    ///     NO mueve el requerimiento ni dispara el correo a GTH.
    ///   • <b>Gerencia General</b> (<c>EstadoGerenteGeneral*</c> / <c>GerenteGeneral*</c>): la
    ///     obligatoria. Su decisión mueve las vacantes (VALIDACION_GTH / RECHAZADO_GG) y envía el
    ///     correo a GTH con lo aprobado.
    ///
    /// Sin orden impuesto entre las dos: el correo les llega a la vez y el gerente del área puede
    /// registrar su visto bueno incluso después de que el GG cerró la solicitud.
    /// </summary>
    public class GthAprobacionGg
    {
        public int GthAprobacionGgId { get; set; }

        /// <summary>FK a la solicitud dueña de la aprobación (1:1 entre las vigentes).</summary>
        public int GthSolicitudId { get; set; }

        /// <summary>
        /// Identificador aleatorio de la aprobación. Nació como token de acceso a la página
        /// pública de decisión (sin login); esa página ya no existe — hoy los gerentes entran a
        /// «Aprobaciones» con su sesión. Se sigue generando porque la columna es NOT NULL y
        /// tiene índice único entre las vigentes, pero NO otorga acceso a nada.
        /// </summary>
        public string Token { get; set; } = null!;

        // ── Casilla del Gerente General (la obligatoria) ─────────────────────
        /// <summary>FK a <see cref="GthAprobacionGgEstado"/>: PENDIENTE / APROBADA / APROBADA_PARCIAL / RECHAZADA.</summary>
        public int EstadoGerenteGeneralId { get; set; }

        public DateTimeOffset? GerenteGeneralDecididoDateTime { get; set; }

        /// <summary>
        /// Usuario que registró la decisión de Gerencia General desde «Aprobaciones». Va aparte de
        /// <c>updated_user_id</c> para que un update posterior no la pise. Null en las aprobaciones
        /// anteriores a la pantalla (se decidían por enlace, sin sesión).
        /// </summary>
        public int? GerenteGeneralDecididoUserId { get; set; }

        /// <summary>Comentario opcional que el GG escribe al confirmar su decisión.</summary>
        public string? GerenteGeneralComentario { get; set; }

        // ── Casilla del gerente del área (visto bueno, no bloquea) ───────────
        /// <summary>
        /// FK a <see cref="GthAprobacionGgEstado"/> (mismo catálogo que el GG). Nace en PENDIENTE y
        /// se queda ahí si el gerente del área nunca opina: su decisión no condiciona nada.
        /// </summary>
        public int EstadoGerenteAreaId { get; set; }

        public DateTimeOffset? GerenteAreaDecididoDateTime { get; set; }

        /// <summary>Usuario (gerente del área) que registró el visto bueno.</summary>
        public int? GerenteAreaDecididoUserId { get; set; }

        /// <summary>Comentario opcional del gerente del área.</summary>
        public string? GerenteAreaComentario { get; set; }

        // ── Casilla de GTH (la tercera, solo para reemplazos) ────────────────
        /// <summary>
        /// FK a <see cref="GthAprobacionGgEstado"/> (mismo catálogo que las otras dos). Solo cuenta
        /// para las vacantes de ruta <c>AREA_GTH</c> — los reemplazos que no son FFT—, donde la
        /// vacante avanza recién con la firma de GTH Y la del gerente del área. En una solicitud sin
        /// reemplazos se queda en PENDIENTE para siempre y no significa nada.
        /// </summary>
        public int EstadoGthId { get; set; }

        public DateTimeOffset? GthDecididoDateTime { get; set; }

        /// <summary>Usuario del área de GTH que registró la decisión.</summary>
        public int? GthDecididoUserId { get; set; }

        /// <summary>Comentario opcional de GTH.</summary>
        public string? GthComentario { get; set; }

        // ── Correo enviado a AMBOS gerentes (uno solo, con n destinatarios) ──
        /// <summary>Destinatarios principales (Para) a los que se envió el correo, separados por "; ".</summary>
        public string? CorreoEnvio { get; set; }

        /// <summary>Destinatarios en copia (CC) a los que se envió el correo, separados por "; ".</summary>
        public string? CorreoCopia { get; set; }

        public DateTimeOffset? EnviadoDateTime { get; set; }

        /// <summary>Último reenvío del correo ("Reenviar a Gerencia General"). El token no cambia.</summary>
        public DateTimeOffset? ReenviadoDateTime { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; } = true;
        public bool State { get; set; } = true;
    }
}

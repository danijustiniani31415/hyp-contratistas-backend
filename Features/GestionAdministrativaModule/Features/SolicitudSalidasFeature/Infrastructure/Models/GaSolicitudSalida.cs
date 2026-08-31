namespace Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Infrastructure.Models
{
    /// <summary>
    /// Solicitud de salida — cabecera. Los detalles del/los trayecto(s) viven en GaSolicitudTrayecto.
    /// </summary>
    public class GaSolicitudSalida
    {
        public int Id { get; set; }
        public int WorkerId { get; set; }
        public DateOnly FechaSalida { get; set; }
        /// <summary>FK a <c>ga_estado_aprobacion</c>. Ver <see cref="EstadosSalida.Aprobacion"/>.</summary>
        public int EstadoAprobacionId { get; set; } = EstadosSalida.Aprobacion.Pendiente;
        /// <summary>FK a <c>ga_estado_rendicion</c>. Ver <see cref="EstadosSalida.Rendicion"/>.</summary>
        public int EstadoRendicionId { get; set; } = EstadosSalida.Rendicion.NoRendido;
        public int? RegistradoPorId { get; set; }
        /// <summary>
        /// FK a <c>workers.id</c> del trabajador que APROBÓ/RECHAZÓ realmente la solicitud
        /// (se llena al momento de la decisión). Excluyente con <see cref="AprobadorAreaScopeId"/>:
        /// el CHECK <c>chk_ga_solicitud_salida_aprobador_unico</c> impide ambos llenos.
        /// En solicitudes antiguas guardaba al revisor al que se envió el correo; ese dato
        /// ahora vive en <see cref="EnviadoACorreo"/>.
        /// </summary>
        public int? AprobadorWorkerId { get; set; }
        /// <summary>
        /// FK a <c>area_scope.area_scope_id</c> cuando la decisión la tomó un área (correo
        /// grupal, ej. GTH vía gthnm@abril.pe) y no un trabajador puntual. Excluyente con
        /// <see cref="AprobadorWorkerId"/>.
        /// </summary>
        public int? AprobadorAreaScopeId { get; set; }
        /// <summary>Correo al que se envió la solicitud para su aprobación (revisor resuelto
        /// por workers_revisores, o el correo del área GTH como fallback).</summary>
        public string? EnviadoACorreo { get; set; }
        public DateTimeOffset? FechaDecision { get; set; }
        public string? MotivoRechazo { get; set; }
        /// <summary>
        /// FK a <c>app_user.user_id</c> del usuario que canceló la solicitud (el propio solicitante).
        /// Solo se llena cuando <see cref="EstadoAprobacionId"/> pasa a
        /// <see cref="EstadosSalida.Aprobacion.Cancelado"/>. Auditoría — no bloquea ningún flujo.
        /// </summary>
        public int? CanceladaPorId { get; set; }
        /// <summary>Momento (UTC) en que el solicitante canceló la solicitud.</summary>
        public DateTimeOffset? CanceladaAt { get; set; }
        public int? RendicionId { get; set; }

        // ── Reembolso ────────────────────────────────────────────────────────
        // Eje aparte de la aprobación de la salida y de la rendición: arranca recién cuando la
        // salida está Rendida y tiene adjunto el Consolidado del S10 (ga_consolidado_s10).

        /// <summary>FK a <c>ga_estado_reembolso</c>. Ver <see cref="EstadosSalida.Reembolso"/>.</summary>
        public int EstadoReembolsoId { get; set; } = EstadosSalida.Reembolso.Pendiente;

        /// <summary>
        /// Observación que escribe el jefe al RECHAZAR el reembolso: es lo que el trabajador tiene
        /// que subsanar. Se conserva al volver a Pendiente para que se vea qué se observó.
        /// </summary>
        public string? ObservacionReembolso { get; set; }

        /// <summary>FK a <c>app_user.user_id</c> del jefe que aprobó/rechazó el reembolso.</summary>
        public int? ReembolsoDecididoPorId { get; set; }
        public DateTimeOffset? ReembolsoDecididoAt { get; set; }

        /// <summary>
        /// Momento en que el trabajador avisó al jefe/revisor que ya adjuntó el Consolidado del
        /// S10. Solo informativo: no bloquea la revisión (el jefe puede aprobar sin aviso).
        /// </summary>
        public DateTimeOffset? RevisorNotificadoAt { get; set; }
        public int? RevisorNotificadoPorId { get; set; }

        /// <summary>FK a <c>app_user.user_id</c> del jefe que firmó la planilla de esta salida.</summary>
        public int? FirmadoPorId { get; set; }
        public DateTimeOffset? FirmadoAt { get; set; }

        /// <summary>FK a <c>app_user.user_id</c> del tesorero que marcó el reembolso como pagado.</summary>
        public int? PagadoPorId { get; set; }
        public DateTimeOffset? PagadoAt { get; set; }
        /// <summary>Hora real en la que la persona salió, registrada por recepción. Dato extra — no bloquea ningún flujo.</summary>
        public TimeOnly? HoraSalidaReal { get; set; }
        public int? HoraSalidaRealRegistradaPorId { get; set; }
        public DateTimeOffset? HoraSalidaRealRegistradaAt { get; set; }
        /// <summary>Hora real en la que la persona retornó, registrada por recepción. Dato extra — no bloquea ningún flujo.</summary>
        public TimeOnly? HoraRetornoReal { get; set; }
        public int? HoraRetornoRealRegistradaPorId { get; set; }
        public DateTimeOffset? HoraRetornoRealRegistradaAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}

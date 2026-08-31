namespace Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Application.Dtos
{
    public class SolicitudSalidaListItemDto
    {
        public int Id { get; set; }
        public DateOnly FechaSalida { get; set; }

        // ── Datos agregados del/los trayecto(s) para vista de tabla ─────
        /// <summary>Hora de salida del primer trayecto.</summary>
        public TimeOnly HoraSalida { get; set; }
        /// <summary>Hora de retorno del último trayecto.</summary>
        public TimeOnly? HoraRetorno { get; set; }
        /// <summary>Motivo del primer trayecto.</summary>
        public string Motivo { get; set; } = string.Empty;
        /// <summary>Origen del primer trayecto.</summary>
        public string? LugarOrigen { get; set; }
        /// <summary>Destino del último trayecto.</summary>
        public string? LugarDestino { get; set; }
        /// <summary>Cantidad total de trayectos (≥ 1).</summary>
        public int TrayectosCount { get; set; }

        public string EstadoAprobacion { get; set; } = string.Empty;
        public string EstadoRendicion { get; set; } = "No rendido";
        public DateTimeOffset CreatedAt { get; set; }
        /// <summary>True si todos los trayectos están cubiertos (captura por trayecto, o catálogo TI) — habilita la rendición.</summary>
        public bool PuedeRendirse { get; set; }

        // ── Consolidado del S10 (solo salidas rendidas) ──────────────────
        /// <summary>webUrl del PDF Consolidado del S10 vigente, o null si aún no se adjuntó.</summary>
        public string? ConsolidadoS10Url { get; set; }
        /// <summary>Nombre del archivo del consolidado vigente. Null si no hay.</summary>
        public string? ConsolidadoS10Filename { get; set; }
        /// <summary>"Rendicion" (cubre toda la planilla) | "Solicitud" (solo esta salida) | null si no hay.</summary>
        public string? ConsolidadoS10Ambito { get; set; }

        // ── Reembolso ────────────────────────────────────────────────────
        /// <summary>"Pendiente" | "Aprobado" | "Rechazado" | "Firmado" | "Pagado".</summary>
        public string EstadoReembolso { get; set; } = "Pendiente";

        /// <summary>Observación del jefe al rechazar el reembolso: es lo que hay que subsanar.</summary>
        public string? ObservacionReembolso { get; set; }

        /// <summary>
        /// True cuando el trabajador ya puede avisarle al revisor: la salida está rendida, tiene el
        /// Consolidado del S10 adjunto y el reembolso sigue abierto (Pendiente o Rechazado).
        /// </summary>
        public bool PuedeNotificarRevisor { get; set; }

        /// <summary>Última vez que se le avisó al revisor. Null si nunca se le avisó.</summary>
        public DateTimeOffset? RevisorNotificadoAt { get; set; }
    }
}

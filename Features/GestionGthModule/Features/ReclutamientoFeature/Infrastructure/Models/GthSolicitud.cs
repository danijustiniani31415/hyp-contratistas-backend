namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Solicitud de personal registrada por una jefatura/gerencia (tabla <c>gth_solicitud</c>).
    /// Agrupa 1..10 vacantes; cada vacante genera un <see cref="GthRequerimiento"/> independiente.
    /// La justificación general y el sustento (adjunto opcional en SharePoint) son de la solicitud
    /// completa, no por vacante. El área del solicitante se guarda como snapshot (derivada del
    /// worker del usuario autenticado, no editable).
    /// </summary>
    public class GthSolicitud
    {
        public int GthSolicitudId { get; set; }

        /// <summary>FK a <c>area_scope</c> del solicitante (árbol de áreas). Null si no se pudo resolver.</summary>
        public int? AreaScopeId { get; set; }

        /// <summary>Nombre del área del solicitante al momento de registrar (snapshot legible).</summary>
        public string? AreaNombre { get; set; }

        /// <summary>Usuario (app_user) que registró la solicitud.</summary>
        public int? SolicitanteUserId { get; set; }

        /// <summary>Worker del solicitante (si el usuario está vinculado a un trabajador).</summary>
        public int? SolicitanteWorkerId { get; set; }

        public string? Justificacion { get; set; }

        // ── Sustento (adjunto opcional en SharePoint) ────────────────────────
        public string? SustentoNombre { get; set; }
        public string? SustentoUrl { get; set; }
        public string? SustentoItemId { get; set; }
        public string? SustentoDriveId { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; } = true;
        public bool State { get; set; } = true;

        public List<GthRequerimiento> Requerimientos { get; set; } = new();
    }
}

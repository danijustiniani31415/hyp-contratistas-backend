namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Infrastructure.Models
{
    /// <summary>
    /// Responsable de ejecutar un acuerdo. Es un worker de la organización; no necesita haber
    /// asistido a la reunión (un acuerdo puede recaer en alguien ausente). WorkerId es la fuente
    /// de verdad; ReunionParticipanteId se conserva solo para registros creados antes de este
    /// cambio y para exponer el nombre/cargo capturado en esa reunión si aplica.
    /// </summary>
    public class ReunionAcuerdoResponsable
    {
        public int ReunionAcuerdoResponsableId { get; set; }
        public int ReunionAcuerdoId { get; set; }

        public int? ReunionParticipanteId { get; set; }
        public int? WorkerId { get; set; }

        /// <summary>PENDIENTE | ACEPTADO | RECHAZADO. Solo relevante si el acuerdo tiene RequiereAceptacion.</summary>
        public string EstadoAceptacion { get; set; } = "PENDIENTE";
        public string? MotivoRechazo { get; set; }
        public DateTime? FechaRespuesta { get; set; }

        /// <summary>Cuando el acuerdo tiene varios responsables, marca a quien queda a cargo de que
        /// se cumpla (evita que la responsabilidad se diluya entre todos). Debe haber a lo más uno
        /// en true por acuerdo.</summary>
        public bool EsPrincipal { get; set; }

        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}

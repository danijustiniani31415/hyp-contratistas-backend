namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Infrastructure.Models
{
    /// <summary>
    /// Participante convocado a una reunión. Las iniciales identifican al participante
    /// como responsable dentro de los acuerdos (columna EJECUCIÓN del formato SIG-FO-17).
    /// </summary>
    public class ReunionParticipante
    {
        public int ReunionParticipanteId { get; set; }
        public int ReunionId { get; set; }

        /// <summary>
        /// Trabajador de Abril si se eligió del desplegable; null si es un invitado externo
        /// capturado a mano. Permite filtrar/notificar por área/puesto y vincular acuerdos
        /// con responsables que no necesariamente asistieron a esta reunión puntual.
        /// </summary>
        public int? WorkerId { get; set; }

        public string Nombre { get; set; } = null!;
        public string? Cargo { get; set; }
        public string? Iniciales { get; set; }
        public bool Asistio { get; set; }
        public int Orden { get; set; }

        /// <summary>Si true, puede editar el acta igual que su creador (Update/Reprogramar/CambiarEstado/
        /// Eliminar y CRUD de acuerdos). Pensado para cuando el creador se enferma o no puede asistir.
        /// Solo tiene efecto si el participante tiene WorkerId (no aplica a invitados externos).</summary>
        public bool EsCoautor { get; set; }

        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}

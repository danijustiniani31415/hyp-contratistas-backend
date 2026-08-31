using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.SsomaModule.InduccionProgramacionFeature.Infrastructure.Models
{
    /// <summary>
    /// Una fecha concreta de inducción para un proyecto: "el {Fecha} le toca inducción al
    /// proyecto {ProyectoId}". Se genera automáticamente por la rotación (ver
    /// <see cref="InduccionProgramacionService.GenerarProgramacionAsync"/>), saltando fines de
    /// semana y feriados, y puede editarse a mano (reasignar, cancelar o reprogramar) sin
    /// afectar el avance de la cola para las demás fechas.
    /// </summary>
    [Table("ss_induccion_programacion")]
    public class SsInduccionProgramacion
    {
        public int Id { get; set; }
        public DateOnly Fecha { get; set; }
        public int ProyectoId { get; set; }

        /// <summary>Persona que va a dar esta inducción puntual. Se copia del turno de rotación
        /// al generarse, pero es editable independientemente (un día concreto puede cambiar de
        /// responsable sin tocar la rotación general).</summary>
        public int? ResponsableWorkerId { get; set; }

        /// <summary>Programada | Cancelada | Realizada.</summary>
        public string Estado { get; set; } = "Programada";

        /// <summary>true si la fecha/proyecto de esta fila fue tocada a mano (reasignación,
        /// cancelación o reprogramación) en vez de venir de la generación automática.</summary>
        public bool EsManual { get; set; }
        public string? MotivoCambio { get; set; }

        public bool AvisoEnviado { get; set; }
        public DateTime? FechaAvisoEnviado { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(ProyectoId))]
        public Project? Proyecto { get; set; }

        [ForeignKey(nameof(ResponsableWorkerId))]
        public Worker? ResponsableWorker { get; set; }
    }
}

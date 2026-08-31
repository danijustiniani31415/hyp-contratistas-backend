using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.SsomaModule.InduccionProgramacionFeature.Infrastructure.Models
{
    /// <summary>
    /// Un turno dentro de la cola de rotación de inducciones. El <see cref="Orden"/> define la
    /// posición en la cola circular; <see cref="InduccionProgramacionService"/> la recorre para
    /// asignar cada fecha hábil (L/M/V) al siguiente turno activo. Cualquiera de SSOMA puede
    /// reordenar/activar/desactivar en cualquier momento — no hay restricción de rol más allá de
    /// pertenecer a SSOMA.
    ///
    /// Un mismo proyecto puede tener MÁS DE UN turno (ej. "Oficina Central" cubierto por dos
    /// personas distintas que se alternan) — por eso ya no hay unicidad por ProyectoId, cada fila
    /// es un turno independiente identificado por (ProyectoId, ResponsableWorkerId).
    /// </summary>
    [Table("ss_induccion_rotacion_proyecto")]
    public class SsInduccionRotacionProyecto
    {
        public int Id { get; set; }
        public int ProyectoId { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; } = true;

        /// <summary>
        /// Persona asignada a este turno (Coordinador SSOMA o Prevencionista del proyecto,
        /// elegido a mano). Opcional — un turno puede quedar sin responsable puntual asignado.
        /// </summary>
        public int? ResponsableWorkerId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(ProyectoId))]
        public Project? Proyecto { get; set; }

        [ForeignKey(nameof(ResponsableWorkerId))]
        public Worker? ResponsableWorker { get; set; }
    }
}

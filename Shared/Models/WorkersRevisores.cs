using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Shared.Models
{
    /// <summary>
    /// Jefe/revisor personalizado de un trabajador: sobrescribe al revisor que le tocaría
    /// por su área (<c>area_revisores</c>). Se resuelve por prioridad (1 = mayor) y solo
    /// cuentan las filas vivas (state) y activas (active) cuyo revisor tenga correo
    /// corporativo @abril.pe; sin ninguna, manda el revisor del área y, en última
    /// instancia, el área de GTH. Lo lee <c>IJefeRevisorResolver</c> (salidas, correos de
    /// EMO/interconsultas, recordatorios de evaluaciones) y lo escribe el checkbox
    /// "Jefe personalizado" del formulario de trabajadores
    /// (<c>IJefePersonalizadoService</c>), que hoy guarda como máximo un revisor por
    /// trabajador. El modelo admite n por trabajador porque así quedó la configuración
    /// histórica de "Revisores de Trabajadores", pantalla ya retirada.
    /// </summary>
    [Table("workers_revisores")]
    public class WorkersRevisores
    {
        [Column("workers_revisores_id")]
        public int WorkersRevisoresId { get; set; }

        /// <summary>Trabajador (workers.id) al que se le asigna el jefe.</summary>
        [Column("solicitante_id")]
        public int SolicitanteId { get; set; }

        /// <summary>Trabajador (workers.id) que actúa como jefe/revisor del solicitante.</summary>
        [Column("revisor_id")]
        public int RevisorId { get; set; }

        /// <summary>1 = primero en ser considerado; a mayor número, menor prioridad.</summary>
        [Column("orden_prioridad")]
        public int OrdenPrioridad { get; set; } = 1;

        /// <summary>Si false, el revisor no se considera (ej. ausencia temporal del jefe).</summary>
        [Column("active")]
        public bool Active { get; set; } = true;

        /// <summary>Soft delete: false = eliminado (se conserva para auditoría).</summary>
        [Column("state")]
        public bool State { get; set; } = true;

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}

using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Infrastructure.Models;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("ss_trabajador_restringido")]
    public class SsTrabajadorRestringido
    {
        public int Id { get; set; }
        public string? Dni { get; set; }
        public int? WorkerId { get; set; }
        public string? ApellidoNombre { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string? ProyectoOrigen { get; set; }
        public string? RestringidoPor { get; set; }
        public DateOnly? FechaRestriccion { get; set; }
        /// <summary>SANCION (retiro definitivo por amonestación) | DESCANSO_MEDICO (bloqueo temporal de acceso, no es sanción) | MANUAL.
        /// Distingue el origen para que pantallas de sanciones (Amonestaciones) puedan excluir los bloqueos médicos,
        /// mientras Control de Acceso sigue bloqueando el ingreso sin importar el tipo.</summary>
        public string Tipo { get; set; } = "MANUAL";
        public bool Activo { get; set; } = true;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(WorkerId))]
        public Worker? Worker { get; set; }
    }
}

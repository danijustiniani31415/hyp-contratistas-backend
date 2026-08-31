using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Infrastructure.Models;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    /// <summary>
    /// Agrupa un mismo problema de salud de un trabajador: el descanso original, cada "más
    /// descanso" (antes "prórroga") que lo extiende, todos los seguimientos, y el alta que lo
    /// cierra. Antes de esta tabla, el alta vivía en el descanso individual y no había forma de
    /// ver el historial completo de un caso sin caminar la cadena de <c>ProrrogaDelId</c> a mano.
    /// </summary>
    [Table("ss_descanso_caso")]
    public class SsDescansoCaso
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("worker_id")]
        public int WorkerId { get; set; }

        [Column("fecha_apertura")]
        public DateOnly FechaApertura { get; set; }

        /// <summary>"Abierto" | "Cerrado".</summary>
        [Column("estado")]
        public string Estado { get; set; } = "Abierto";

        [Column("fecha_cierre")]
        public DateOnly? FechaCierre { get; set; }

        [Column("alta_por_id")]
        public int? AltaPorId { get; set; }

        [Column("alta_observaciones")]
        public string? AltaObservaciones { get; set; }

        /// <summary>Se completa la primera vez que el caso se reabre — de ahí en adelante,
        /// DarAlta exige un descanso nuevo registrado después de esta fecha (ver
        /// DescansoMedicoService.DarAlta) antes de poder volver a cerrar el caso.</summary>
        [Column("fecha_reapertura")]
        public DateOnly? FechaReapertura { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [Column("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        [Column("state")]
        public bool State { get; set; } = true;

        [ForeignKey(nameof(WorkerId))]
        public Worker? Worker { get; set; }
    }
}

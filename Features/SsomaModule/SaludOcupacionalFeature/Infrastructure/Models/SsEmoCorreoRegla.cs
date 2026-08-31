using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    /// <summary>
    /// Una celda de la matriz de destinatarios de los correos de EMO:
    ///
    ///     correo (<see cref="SsEmoCorreoEvento"/>)
    ///   × perfil del trabajador (<see cref="SsEmoCorreoPerfil"/>)
    ///   × destinatario (<see cref="SsEmoCorreoDestinatario"/>)
    ///   → <see cref="Active"/>
    ///
    /// <see cref="Active"/> es el único interruptor que decide el envío: el
    /// <c>active</c> de la fila del destinatario quedó sin uso cuando la
    /// configuración pasó de ser una lista plana a esta matriz.
    /// </summary>
    [Table("ss_emo_correo_regla")]
    public class SsEmoCorreoRegla
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("evento_id")]
        public int EventoId { get; set; }

        [Column("perfil_id")]
        public int PerfilId { get; set; }

        [Column("destinatario_id")]
        public int DestinatarioId { get; set; }

        /// <summary>true = a ese destinatario le llega ese correo para ese perfil de trabajador.</summary>
        [Column("active")]
        public bool Active { get; set; }

        [Column("state")]
        public bool State { get; set; } = true;

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [Column("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        [ForeignKey(nameof(EventoId))]
        public SsEmoCorreoEvento? Evento { get; set; }

        [ForeignKey(nameof(PerfilId))]
        public SsEmoCorreoPerfil? Perfil { get; set; }

        [ForeignKey(nameof(DestinatarioId))]
        public SsEmoCorreoDestinatario? Destinatario { get; set; }
    }
}

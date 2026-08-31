using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Infrastructure.Models;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_alertas_emo")]
    public class SsAlertaEmo
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("worker_id")]
        public int? WorkerId { get; set; }

        [Column("emo_id")]
        public int? EmoId { get; set; }

        [Column("tipo_alerta")]
        public string TipoAlerta { get; set; } = string.Empty;

        [Column("fecha_alerta")]
        public DateOnly FechaAlerta { get; set; }

        [Column("enviado_email")]
        public bool EnviadoEmail { get; set; }

        [Column("fecha_envio")]
        public DateTimeOffset? FechaEnvio { get; set; }

        [Column("destinatarios")]
        public string? Destinatarios { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [ForeignKey(nameof(WorkerId))]
        public Worker? Worker { get; set; }

        [ForeignKey(nameof(EmoId))]
        public WorkerEmo? Emo { get; set; }
    }
}

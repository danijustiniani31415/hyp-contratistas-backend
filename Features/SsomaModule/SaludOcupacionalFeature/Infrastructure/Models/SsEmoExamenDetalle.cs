using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Infrastructure.Models;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_emo_examenes_detalle")]
    public class SsEmoExamenDetalle
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("emo_id")]
        public int EmoId { get; set; }

        [Column("examen_tipo_id")]
        public int ExamenTipoId { get; set; }

        [Column("resultado")]
        public string? Resultado { get; set; }

        [Column("valor")]
        public string? Valor { get; set; }

        [Column("unidad")]
        public string? Unidad { get; set; }

        [Column("observacion")]
        public string? Observacion { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [ForeignKey(nameof(EmoId))]
        public WorkerEmo? Emo { get; set; }

        [ForeignKey(nameof(ExamenTipoId))]
        public SsExamenTipo? ExamenTipo { get; set; }
    }
}

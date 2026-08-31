using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Infrastructure.Models;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_emo_restricciones")]
    public class SsEmoRestriccion
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("emo_id")]
        public int EmoId { get; set; }

        [Column("restriccion_tipo_id")]
        public int? RestriccionTipoId { get; set; }

        [Column("descripcion_libre")]
        public string? DescripcionLibre { get; set; }

        [Column("vigente")]
        public bool Vigente { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [ForeignKey(nameof(EmoId))]
        public WorkerEmo? Emo { get; set; }

        [ForeignKey(nameof(RestriccionTipoId))]
        public SsRestriccionTipo? RestriccionTipo { get; set; }
    }
}

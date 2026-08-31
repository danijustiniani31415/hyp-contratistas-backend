using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Infrastructure.Models
{
    [Table("ac_tareo_enrolamiento")]
    public class AcTareoEnrolamiento
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("worker_id")]
        public int WorkerId { get; set; }

        [Column("embedding", TypeName = "real[]")]
        public float[] Embedding { get; set; } = [];

        [Column("foto_url")]
        public string FotoUrl { get; set; } = string.Empty;

        [Column("consentimiento_en")]
        public DateTime ConsentimientoEn { get; set; }

        [Column("activo")]
        public bool Activo { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}

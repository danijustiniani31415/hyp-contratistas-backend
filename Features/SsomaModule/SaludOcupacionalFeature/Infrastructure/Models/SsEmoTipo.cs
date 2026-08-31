using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_emo_tipos")]
    public class SsEmoTipo
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("vigencia_meses")]
        public int? VigenciaMeses { get; set; }

        [Column("requiere_nuevo")]
        public bool RequiereNuevo { get; set; }

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("activo")]
        public bool Activo { get; set; }
    }
}

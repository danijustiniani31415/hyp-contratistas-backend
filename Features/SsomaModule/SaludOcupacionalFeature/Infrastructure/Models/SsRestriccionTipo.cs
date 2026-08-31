using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_restriccion_tipos")]
    public class SsRestriccionTipo
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("descripcion")]
        public string Descripcion { get; set; } = string.Empty;

        [Column("categoria")]
        public string? Categoria { get; set; }

        [Column("activo")]
        public bool Activo { get; set; }
    }
}

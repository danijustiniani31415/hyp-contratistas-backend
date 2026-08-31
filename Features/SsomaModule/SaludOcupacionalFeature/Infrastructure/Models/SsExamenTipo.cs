using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_examen_tipos")]
    public class SsExamenTipo
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("codigo")]
        public string? Codigo { get; set; }

        [Column("categoria")]
        public string? Categoria { get; set; }

        [Column("activo")]
        public bool Activo { get; set; }
    }
}

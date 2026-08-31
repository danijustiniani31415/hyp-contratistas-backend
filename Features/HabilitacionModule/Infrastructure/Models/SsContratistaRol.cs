using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("ss_contratista_rol")]
    public class SsContratistaRol
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;
    }
}

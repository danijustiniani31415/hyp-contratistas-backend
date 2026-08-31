using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Infrastructure.Models
{
    [Table("ac_categorias")]
    public class AcCategoria
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;
    }
}

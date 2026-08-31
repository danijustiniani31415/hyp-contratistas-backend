using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Infrastructure.Models
{
    [Table("ac_especialidades")]
    public class AcEspecialidad
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;
    }
}

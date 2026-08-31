using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("cat_categoria")]
    public class CatCategoria
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Orden { get; set; }
        public bool Activo { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}

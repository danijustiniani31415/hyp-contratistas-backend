using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("cat_subarea")]
    public class CatSubarea
    {
        public int Id { get; set; }
        public string Subarea { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string? Jefatura { get; set; }
        public bool Activo { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}

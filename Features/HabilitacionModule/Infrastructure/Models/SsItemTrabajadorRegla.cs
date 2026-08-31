using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("ss_item_trabajador_regla")]
    public class SsItemTrabajadorRegla
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public int? CategoriaId { get; set; }
        public string? TipoTrabajador { get; set; }
        public bool Requerido { get; set; } = true;
        public string? EvaluadorRol { get; set; }
        public string? Nota { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(ItemId))]
        public SsItemTrabajador? Item { get; set; }
    }
}

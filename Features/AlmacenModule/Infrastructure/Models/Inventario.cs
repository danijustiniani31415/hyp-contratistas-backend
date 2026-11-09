using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.AlmacenModule.Infrastructure.Models
{
    /// <summary>Toma de inventario físico de un almacén: EN_CONTEO → CERRADO (genera los ajustes en el kardex) o ANULADO.</summary>
    [Table("lb_inventario")]
    public class Inventario
    {
        public long Id { get; set; }
        public int AlmacenId { get; set; }
        public DateOnly Fecha { get; set; }
        public string Estado { get; set; } = "EN_CONTEO";
        public string? Observacion { get; set; }
        public long? CreadoPor { get; set; }
        public DateTimeOffset CreadoEn { get; set; }
        public long? CerradoPor { get; set; }
        public DateTimeOffset? CerradoEn { get; set; }
        public int CantidadAjustes { get; set; }

        public List<InventarioItem> Items { get; set; } = new();
    }

    [Table("lb_inventario_item")]
    public class InventarioItem
    {
        public long Id { get; set; }
        public long InventarioId { get; set; }
        public long ProductoId { get; set; }
        public string Talla { get; set; } = "";
        public string Color { get; set; } = "";
        public decimal CantidadSistema { get; set; }
        public decimal? CantidadContada { get; set; }
        public decimal? CantidadAjuste { get; set; }
        public string? Motivo { get; set; }
    }
}

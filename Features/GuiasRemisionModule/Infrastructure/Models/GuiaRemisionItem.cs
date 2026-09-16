using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.CatalogoModule.Infrastructure.Models;

namespace Abril_Backend.Features.GuiasRemisionModule.Infrastructure.Models
{
    [Table("lb_guia_remision_item")]
    public class GuiaRemisionItem
    {
        public long Id { get; set; }
        public long GuiaRemisionId { get; set; }
        public long ProductoId { get; set; }
        public string Talla { get; set; } = "";
        public decimal Cantidad { get; set; }
        /// <summary>Catálogo 03 SUNAT (unidad de medida) — NIU = unidad por defecto.</summary>
        public string UnidadMedida { get; set; } = "NIU";

        [ForeignKey(nameof(GuiaRemisionId))]
        public GuiaRemision? GuiaRemision { get; set; }
        [ForeignKey(nameof(ProductoId))]
        public Producto? Producto { get; set; }
    }
}

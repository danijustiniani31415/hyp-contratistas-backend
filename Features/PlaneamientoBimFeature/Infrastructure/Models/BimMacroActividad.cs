using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Models
{
    [Table("bim_macro_actividad")]
    public class BimMacroActividad
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("orden")]
        public int Orden { get; set; }

        public List<BimActividad> Actividades { get; set; } = new();
    }
}

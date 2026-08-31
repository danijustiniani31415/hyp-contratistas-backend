using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Models
{
    [Table("bim_fase")]
    public class BimFase
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("orden")]
        public int Orden { get; set; }
    }
}

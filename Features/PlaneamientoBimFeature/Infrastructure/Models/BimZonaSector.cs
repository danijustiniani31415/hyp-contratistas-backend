using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Models
{
    [Table("bim_zona_sector")]
    public class BimZonaSector
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("zona_id")]
        public int ZonaId { get; set; }
        public BimProyectoZona Zona { get; set; } = null!;

        /// <summary>NULL = sector compartido por todos los niveles de la zona
        /// (comportamiento historico). Con valor = sector exclusivo de ese nivel.</summary>
        [Column("zona_nivel_id")]
        public int? ZonaNivelId { get; set; }
        public BimZonaNivel? ZonaNivel { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("orden")]
        public int Orden { get; set; }
    }
}

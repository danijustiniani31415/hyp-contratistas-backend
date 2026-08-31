using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Models
{
    [Table("bim_zona_nivel")]
    public class BimZonaNivel
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("zona_id")]
        public int ZonaId { get; set; }
        public BimProyectoZona Zona { get; set; } = null!;

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("orden")]
        public int Orden { get; set; }

        /// <summary>'SUBESTRUCTURA' | 'SUPERESTRUCTURA' | NULL (sin clasificar).</summary>
        [Column("tipo_estructura")]
        public string? TipoEstructura { get; set; }

        /// <summary>Sectores EXCLUSIVOS de este nivel (zona_nivel_id = este Id).
        /// No incluye los "compartidos" de la zona (zona_nivel_id NULL).</summary>
        public List<BimZonaSector> Sectores { get; set; } = new();
    }
}

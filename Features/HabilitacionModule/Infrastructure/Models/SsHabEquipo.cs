using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("ss_hab_equipo")]
    public class SsHabEquipo
    {
        public int Id { get; set; }
        public int EquipoId { get; set; }
        public int ItemId { get; set; }
        public string Estado { get; set; } = "Falta";
        public DateTime? Vigencia { get; set; }
        [Column(TypeName = "text")]
        public string? ArchivoUrl { get; set; }
        public string? ObsAbril { get; set; }
        public string? ObsContratista { get; set; }
        public int? AprobadoPor { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(EquipoId))]
        public SsEquipo? Equipo { get; set; }

        [ForeignKey(nameof(ItemId))]
        public SsItemEquipo? Item { get; set; }
    }
}

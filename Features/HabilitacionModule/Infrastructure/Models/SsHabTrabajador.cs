using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Infrastructure.Models;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("ss_hab_trabajador")]
    public class SsHabTrabajador
    {
        public int Id { get; set; }
        public int WorkerId { get; set; }
        public int ItemId { get; set; }
        public string Estado { get; set; } = "Falta";
        public DateTime? Vigencia { get; set; }
        // Fecha de vigencia propuesta por el contratista en una renovación (estado "Renovando").
        // Se aplica a Vigencia recién cuando Abril aprueba; mientras tanto Vigencia conserva la
        // vigencia aprobada anterior para no dejar al trabajador sin habilitación durante la revisión.
        public DateTime? VigenciaPropuesta { get; set; }
        public string? ArchivoUrl { get; set; }
        public string? ObsAbril { get; set; }
        public string? ObsContratista { get; set; }
        public int? AprobadoPor { get; set; }
        public DateTime? FechaAprobacion { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(WorkerId))]
        public Worker? Worker { get; set; }

        [ForeignKey(nameof(ItemId))]
        public SsItemTrabajador? Item { get; set; }
    }
}

using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.SsomaModule.ChecklistFeature.Infrastructure.Models
{
    [Table("ss_checklist_plantilla_item")]
    public class SsChecklistPlantillaItem
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("plantilla_id")]
        public int PlantillaId { get; set; }

        [Column("descripcion")]
        public string Descripcion { get; set; } = null!;

        [Column("orden")]
        public int Orden { get; set; } = 0;

        // Indica si históricamente este item requería un adjunto en la app anterior
        [Column("tiene_adjunto_ref")]
        public bool TieneAdjuntoRef { get; set; } = false;

        [Column("activo")]
        public bool Activo { get; set; } = true;

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTimeOffset? UpdatedAt { get; set; }

        [ForeignKey(nameof(PlantillaId))]
        public SsChecklistPlantilla? Plantilla { get; set; }
    }
}

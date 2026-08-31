using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Infrastructure.Models;

namespace Abril_Backend.Features.SsomaModule.ChecklistFeature.Infrastructure.Models
{
    [Table("ss_checklist_proyecto_item")]
    public class SsChecklistProyectoItem
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("checklist_proyecto_id")]
        public int ChecklistProyectoId { get; set; }

        [Column("plantilla_item_id")]
        public int PlantillaItemId { get; set; }

        [Column("completado")]
        public bool Completado { get; set; } = false;

        [Column("fecha_completado")]
        public DateTimeOffset? FechaCompletado { get; set; }

        [Column("completado_por_id")]
        public int? CompletadoPorId { get; set; }

        [Column("observacion")]
        public string? Observacion { get; set; }

        [Column("url_adjunto")]
        public string? UrlAdjunto { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTimeOffset? UpdatedAt { get; set; }

        [ForeignKey(nameof(ChecklistProyectoId))]
        public SsChecklistProyecto? ChecklistProyecto { get; set; }

        [ForeignKey(nameof(PlantillaItemId))]
        public SsChecklistPlantillaItem? PlantillaItem { get; set; }

        [ForeignKey(nameof(CompletadoPorId))]
        public User? CompletadoPor { get; set; }
    }
}

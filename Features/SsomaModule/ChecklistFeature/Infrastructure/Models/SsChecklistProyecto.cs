using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.SsomaModule.ChecklistFeature.Infrastructure.Models
{
    [Table("ss_checklist_proyecto")]
    [Index(nameof(ProyectoId), nameof(PlantillaId), IsUnique = true)]
    public class SsChecklistProyecto
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("proyecto_id")]
        public int ProyectoId { get; set; }

        [Column("plantilla_id")]
        public int PlantillaId { get; set; }

        // "pendiente" | "en_progreso" | "completado"
        [Column("estado")]
        public string Estado { get; set; } = "pendiente";

        [Column("porcentaje_completado")]
        public decimal PorcentajeCompletado { get; set; } = 0;

        [Column("fecha_activacion")]
        public DateTimeOffset FechaActivacion { get; set; }

        [Column("activado_por_id")]
        public int? ActivadoPorId { get; set; }

        [Column("fecha_completado")]
        public DateTimeOffset? FechaCompletado { get; set; }

        // Evita enviar el email más de una vez aunque se re-evalúe
        [Column("notificacion_enviada")]
        public bool NotificacionEnviada { get; set; } = false;

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTimeOffset? UpdatedAt { get; set; }

        [ForeignKey(nameof(PlantillaId))]
        public SsChecklistPlantilla? Plantilla { get; set; }

        [ForeignKey(nameof(ProyectoId))]
        public Project? Proyecto { get; set; }

        public ICollection<SsChecklistProyectoItem> Items { get; set; } = new List<SsChecklistProyectoItem>();
    }
}

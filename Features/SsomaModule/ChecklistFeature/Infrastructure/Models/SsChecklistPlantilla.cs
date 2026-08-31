using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.SsomaModule.ChecklistFeature.Infrastructure.Models
{
    [Table("ss_checklist_plantilla")]
    public class SsChecklistPlantilla
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = null!;

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        // "automatico" = se crea al crear el proyecto | "manual" = el usuario lo activa
        [Column("tipo_activacion")]
        public string TipoActivacion { get; set; } = "manual";

        // Null si es manual. Ej: "TORRE_GRUA", "PLACING_BOOM", "GRUA_MOVIL"
        [Column("evento_activacion")]
        public string? EventoActivacion { get; set; }

        // Si es true, TODOS los proyectos deben tenerlo completado
        [Column("es_obligatorio")]
        public bool EsObligatorio { get; set; } = false;

        [Column("orden")]
        public int Orden { get; set; } = 0;

        [Column("activo")]
        public bool Activo { get; set; } = true;

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTimeOffset? UpdatedAt { get; set; }

        public ICollection<SsChecklistPlantillaItem> Items { get; set; } = new List<SsChecklistPlantillaItem>();
        public ICollection<SsChecklistProyecto> ChecklistsProyecto { get; set; } = new List<SsChecklistProyecto>();
    }
}

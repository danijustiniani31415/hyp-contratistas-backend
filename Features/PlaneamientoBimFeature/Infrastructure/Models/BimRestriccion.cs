using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Models
{
    /// <summary>Antes "Bloqueo". La tabla física sigue siendo bim_bloqueo — solo se
    /// renombró la clase/rutas C#, no el esquema.</summary>
    [Table("bim_bloqueo")]
    public class BimRestriccion
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("project_id")]
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        [Column("descripcion")]
        public string Descripcion { get; set; } = string.Empty;

        [Column("estado")]
        public string Estado { get; set; } = string.Empty;

        [Column("fecha_creacion")]
        public DateTimeOffset FechaCreacion { get; set; }

        [Column("fecha_actualizacion")]
        public DateTimeOffset? FechaActualizacion { get; set; }

        /// <summary>Fecha real de levantamiento — ya cubría este rol antes del rename,
        /// se completa en Cerrar().</summary>
        [Column("fecha_cierre")]
        public DateTimeOffset? FechaCierre { get; set; }

        /// <summary>Fecha estimada/objetivo de levantamiento. Nueva.</summary>
        [Column("fecha_levantamiento_prevista")]
        public DateOnly? FechaLevantamientoPrevista { get; set; }

        [Column("created_user_id")]
        public int CreatedUserId { get; set; }

        // ── Ubicación afectada (todas nullable) ─────────────────────────────
        [Column("zona_id")]
        public int? ZonaId { get; set; }
        public BimProyectoZona? Zona { get; set; }

        [Column("zona_nivel_id")]
        public int? ZonaNivelId { get; set; }
        public BimZonaNivel? ZonaNivel { get; set; }

        [Column("zona_sector_id")]
        public int? ZonaSectorId { get; set; }
        public BimZonaSector? ZonaSector { get; set; }

        [Column("actividad_id")]
        public int? ActividadId { get; set; }
        public BimActividad? Actividad { get; set; }
    }
}

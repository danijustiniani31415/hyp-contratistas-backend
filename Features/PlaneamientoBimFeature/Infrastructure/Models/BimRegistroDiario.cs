using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Models
{
    /// <summary>
    /// Único por (project_id, zona_id, nivel_id, sector_id, actividad_id, fecha) —
    /// ver índice único en AppContext. Una corrección dentro de la ventana de edición
    /// es UPDATE sobre esa fila, no un INSERT nuevo.
    /// </summary>
    [Table("bim_registro_diario")]
    public class BimRegistroDiario
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("project_id")]
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        [Column("zona_id")]
        public int ZonaId { get; set; }
        public BimProyectoZona Zona { get; set; } = null!;

        [Column("nivel_id")]
        public int NivelId { get; set; }
        public BimZonaNivel Nivel { get; set; } = null!;

        [Column("sector_id")]
        public int SectorId { get; set; }
        public BimZonaSector Sector { get; set; } = null!;

        [Column("actividad_id")]
        public int ActividadId { get; set; }
        public BimActividad Actividad { get; set; } = null!;

        [Column("fecha")]
        public DateOnly Fecha { get; set; }

        /// <summary>% de avance de la actividad ese día, 0-100. Set fijo validado en el service
        /// (0/25/50/75/100) — ver PlaneamientoBimCargaDiariaService.PorcentajesValidos.</summary>
        [Column("porcentaje_avance")]
        public decimal PorcentajeAvance { get; set; }

        [Column("causa_id")]
        public int? CausaId { get; set; }
        public BimCausaNoCumplimiento? Causa { get; set; }

        [Column("causa_detalle")]
        public string? CausaDetalle { get; set; }

        [Column("created_user_id")]
        public int CreatedUserId { get; set; }

        [Column("created_date_time")]
        public DateTimeOffset CreatedDateTime { get; set; }

        [Column("updated_user_id")]
        public int? UpdatedUserId { get; set; }

        [Column("updated_date_time")]
        public DateTimeOffset? UpdatedDateTime { get; set; }
    }
}

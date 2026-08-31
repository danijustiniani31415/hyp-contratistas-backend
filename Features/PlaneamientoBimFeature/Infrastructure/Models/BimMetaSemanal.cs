using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Models
{
    /// <summary>
    /// Meta de Plan Maestro por (project_id, macro_actividad_id, fecha_inicio_semana) — ver índice
    /// único en AppContext. meta_avance es % de avance ACUMULADO planificado al cierre de esa semana
    /// (curva S), no incremental — comparable directo contra el % real acumulado calculado sobre
    /// bim_registro_diario (mismo criterio que MetaPpc en Project: decimal como porcentaje 0-100).
    /// </summary>
    [Table("bim_meta_semanal")]
    public class BimMetaSemanal
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("project_id")]
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        [Column("macro_actividad_id")]
        public int MacroActividadId { get; set; }
        public BimMacroActividad MacroActividad { get; set; } = null!;

        [Column("fecha_inicio_semana")]
        public DateOnly FechaInicioSemana { get; set; }

        [Column("fecha_fin_semana")]
        public DateOnly FechaFinSemana { get; set; }

        [Column("meta_avance")]
        public decimal MetaAvance { get; set; }

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

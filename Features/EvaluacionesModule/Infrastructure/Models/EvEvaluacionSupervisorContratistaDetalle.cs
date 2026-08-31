using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Models
{
    [Table("ev_evaluacion_supervisor_contratista_detalle")]
    public class EvEvaluacionSupervisorContratistaDetalle
    {
        public int Id { get; set; }

        [Column("evaluacion_supervisor_contratista_id")]
        public int EvaluacionId { get; set; }
        public int? PlantillaId { get; set; }
        public string Criterio { get; set; } = string.Empty;
        public int? Puntaje { get; set; }
        public bool EsNa { get; set; } = false;

        [ForeignKey(nameof(EvaluacionId))]
        public EvEvaluacionSupervisorContratista? Evaluacion { get; set; }
    }
}

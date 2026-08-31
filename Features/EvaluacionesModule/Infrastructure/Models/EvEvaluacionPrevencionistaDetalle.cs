using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Models
{
    [Table("ev_evaluacion_prevencionista_detalle")]
    public class EvEvaluacionPrevencionistaDetalle
    {
        public int Id { get; set; }

        [Column("evaluacion_prevencionista_id")]
        public int EvaluacionId { get; set; }
        public int? PlantillaId { get; set; }
        public string Criterio { get; set; } = string.Empty;
        public int Puntaje { get; set; }

        [ForeignKey(nameof(EvaluacionId))]
        public EvEvaluacionPrevencionista? Evaluacion { get; set; }
    }
}

using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Models
{
    [Table("ev_evaluacion_jefe_ssoma_detalle")]
    public class EvEvaluacionJefeSsomaDetalle
    {
        public int Id { get; set; }

        [Column("evaluacion_jefe_ssoma_id")]
        public int EvaluacionId { get; set; }
        public int? PlantillaId { get; set; }
        public string Criterio { get; set; } = string.Empty;
        public int Puntaje { get; set; }

        [ForeignKey(nameof(EvaluacionId))]
        public EvEvaluacionJefeSsoma? Evaluacion { get; set; }
    }
}

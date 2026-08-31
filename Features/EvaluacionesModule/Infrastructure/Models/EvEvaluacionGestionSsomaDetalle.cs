using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Models
{
    [Table("ev_evaluacion_gestion_ssoma_detalle")]
    public class EvEvaluacionGestionSsomaDetalle
    {
        public int Id { get; set; }

        [Column("evaluacion_gestion_ssoma_id")]
        public int EvaluacionId { get; set; }
        public int? PlantillaId { get; set; }
        public string Criterio { get; set; } = string.Empty;
        public int Puntaje { get; set; }

        [ForeignKey(nameof(EvaluacionId))]
        public EvEvaluacionGestionSsoma? Evaluacion { get; set; }
    }
}

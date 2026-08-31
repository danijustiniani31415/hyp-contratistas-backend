using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Models
{
    [Table("ev_evaluacion_residente_detalle")]
    public class EvEvaluacionResidenteDetalle
    {
        public int Id { get; set; }
        public int EvaluacionId { get; set; }
        public int? PlantillaId { get; set; }
        public string Criterio { get; set; } = string.Empty;
        public int? Puntaje { get; set; }
        public bool EsNa { get; set; } = false;

        [ForeignKey(nameof(EvaluacionId))]
        public EvEvaluacionResidente? Evaluacion { get; set; }

        [ForeignKey(nameof(PlantillaId))]
        public EvPlantilla? Plantilla { get; set; }
    }
}

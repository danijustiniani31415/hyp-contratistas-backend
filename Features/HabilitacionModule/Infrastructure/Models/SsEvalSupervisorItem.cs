using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("ss_eval_supervisor_item")]
    public class SsEvalSupervisorItem
    {
        public int Id { get; set; }
        public int EvaluacionId { get; set; }
        public int CriterioId { get; set; }
        public decimal Nota { get; set; } = 0;

        [ForeignKey(nameof(EvaluacionId))]
        public SsEvalSupervisor? Evaluacion { get; set; }

        [ForeignKey(nameof(CriterioId))]
        public SsCriterioEvaluacion? Criterio { get; set; }
    }
}

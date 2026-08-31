using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Models
{
    [Table("ev_no_aplica")]
    public class EvNoAplica
    {
        public int Id { get; set; }
        public int PeriodoId { get; set; }
        public int EvaluadoUserId { get; set; }
        public int? ProjectId { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public int? RegistradoPor { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(PeriodoId))]
        public EvPeriodo? Periodo { get; set; }
    }
}

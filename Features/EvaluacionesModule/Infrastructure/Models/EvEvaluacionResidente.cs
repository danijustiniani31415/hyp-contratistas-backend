using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Models
{
    [Table("ev_evaluacion_residente")]
    public class EvEvaluacionResidente
    {
        public int Id { get; set; }
        public int PeriodoId { get; set; }
        public int? EvaluadorUserId { get; set; }
        public int? EvaluadorPersonId { get; set; }
        public int EvaluadoUserId { get; set; }
        public int? ProjectId { get; set; }
        public string AreaNombre { get; set; } = string.Empty;
        public decimal? Nota { get; set; }
        public string? Comentario { get; set; }
        public bool NoAplica { get; set; } = false;
        public string? NoAplicaMotivo { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(PeriodoId))]
        public EvPeriodo? Periodo { get; set; }

        [ForeignKey(nameof(ProjectId))]
        public Project? Project { get; set; }

        public ICollection<EvEvaluacionResidenteDetalle> Detalles { get; set; } = [];
    }
}

using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Models
{
    [Table("ev_evaluacion_prevencionista")]
    public class EvEvaluacionPrevencionista
    {
        public int Id { get; set; }
        public int PeriodoId { get; set; }
        public int ProyectoId { get; set; }
        public int EvaluadoUserId { get; set; }
        public int EvaluadorContributorId { get; set; }

        [Column("evaluador_ss_contratista_usuario_id")]
        public int EvaluadorSsContratistaUsuarioId { get; set; }
        public decimal? Nota { get; set; }
        public string? Comentario { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<EvEvaluacionPrevencionistaDetalle> Detalles { get; set; } = [];
    }
}

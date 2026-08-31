using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Models
{
    // A propósito NO tiene EvaluadorUserId: la identidad de quien evalúa vive,
    // separada y sin FK hacia acá, en EvEvaluacionJefeSsomaCumplimiento. Así se
    // puede saber quién ya evaluó (para exigirlo) sin poder cruzar esa identidad
    // con la nota/comentario que puso. Ver EvEvaluacionJefeSsomaCumplimiento.
    [Table("ev_evaluacion_jefe_ssoma")]
    public class EvEvaluacionJefeSsoma
    {
        public int Id { get; set; }
        public int PeriodoId { get; set; }
        public decimal? Nota { get; set; }
        public string? Comentario { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<EvEvaluacionJefeSsomaDetalle> Detalles { get; set; } = [];
    }
}

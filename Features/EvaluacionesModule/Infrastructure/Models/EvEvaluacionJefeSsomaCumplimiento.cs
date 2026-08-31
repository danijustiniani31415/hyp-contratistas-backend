using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Models
{
    // Registro de "quién ya evaluó al jefe SSOMA este período", separado a
    // propósito de EvEvaluacionJefeSsoma (que guarda la nota/comentario sin
    // autor). No lleva FK hacia la evaluación ni al revés: son dos escrituras
    // en la misma transacción, pero nada en el esquema permite unirlas.
    [Table("ev_evaluacion_jefe_ssoma_cumplimiento")]
    public class EvEvaluacionJefeSsomaCumplimiento
    {
        public int Id { get; set; }
        public int PeriodoId { get; set; }
        public int EvaluadorUserId { get; set; }
        public DateTime CompletadoAt { get; set; } = DateTime.UtcNow;
    }
}

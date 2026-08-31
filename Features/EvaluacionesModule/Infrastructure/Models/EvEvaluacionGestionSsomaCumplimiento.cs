using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Models
{
    // Registro de "este Prevencionista ya evaluó a su Coordinador SSOMA este
    // período" (D4), separado a propósito de EvEvaluacionGestionSsoma (que en
    // las filas de D4 guarda la nota/comentario con EvaluadorUserId = NULL). No
    // lleva FK hacia la evaluación ni al revés — igual que el cumplimiento de
    // Jefe SSOMA, sirve para exigir la evaluación y armar el recordatorio por
    // correo sin poder unir autor con respuesta.
    [Table("ev_evaluacion_gestion_ssoma_cumplimiento")]
    public class EvEvaluacionGestionSsomaCumplimiento
    {
        public int Id { get; set; }
        public int PeriodoId { get; set; }
        public int EvaluadorUserId { get; set; }
        public DateTime CompletadoAt { get; set; } = DateTime.UtcNow;
    }
}

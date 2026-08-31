using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Models
{
    [Table("ev_asignacion_supervisor")]
    public class EvAsignacionSupervisor
    {
        public int Id { get; set; }
        public int SupervisorWorkerId { get; set; }
        public int ProjectId { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedByUserId { get; set; }
    }
}

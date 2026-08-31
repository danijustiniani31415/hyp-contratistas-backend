using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.CostsModule.Shared.Models;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Infrastructure.Models
{
    [Table("ss_hab_worker_proyecto")]
    public class WorkerProyecto
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("worker_id")]
        public int WorkerId { get; set; }

        [Column("proyecto_id")]
        public int ProyectoId { get; set; }

        [Column("empresa_id")]
        public int? EmpresaId { get; set; }

        [Column("fecha_inicio")]
        public DateOnly FechaInicio { get; set; }

        [Column("fecha_fin")]
        public DateOnly? FechaFin { get; set; }

        [Column("induccion_completada")]
        public bool InduccionCompletada { get; set; }

        [Column("fecha_induccion")]
        public DateOnly? FechaInduccion { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTimeOffset? UpdatedAt { get; set; }

        [ForeignKey(nameof(WorkerId))]
        public Worker? Worker { get; set; }

        [ForeignKey(nameof(ProyectoId))]
        public Project? Proyecto { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public Contributor? Empresa { get; set; }
    }
}

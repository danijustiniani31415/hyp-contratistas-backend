using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.CostsModule.Shared.Models;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("ss_eval_supervisor")]
    public class SsEvalSupervisor
    {
        public int Id { get; set; }
        public int WorkerId { get; set; }
        public int? EvaluadorUserId { get; set; }
        public string? EvaluadorNombre { get; set; }
        public int EmpresaId { get; set; }
        public int ProyectoId { get; set; }
        public int Mes { get; set; }
        public int Anio { get; set; }
        public decimal? NotaTotal { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(WorkerId))]
        public Worker? Worker { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public Contributor? Empresa { get; set; }

        [ForeignKey(nameof(ProyectoId))]
        public Project? Proyecto { get; set; }

        public ICollection<SsEvalSupervisorItem> Items { get; set; } = [];
    }
}

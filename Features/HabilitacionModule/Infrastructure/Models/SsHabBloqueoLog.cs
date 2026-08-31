using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.CostsModule.Shared.Models;
using Abril_Backend.Infrastructure.Models;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("ss_hab_bloqueo_log")]
    public class SsHabBloqueoLog
    {
        public int Id { get; set; }
        public int WorkerId { get; set; }
        public int EmpresaSolicitanteId { get; set; }
        public int EmpresaPropietariaId { get; set; }
        public string? Motivo { get; set; }
        public DateTime? CreatedAt { get; set; }

        [ForeignKey(nameof(WorkerId))]
        public Worker? Worker { get; set; }

        [ForeignKey(nameof(EmpresaSolicitanteId))]
        public Contributor? EmpresaSolicitante { get; set; }

        [ForeignKey(nameof(EmpresaPropietariaId))]
        public Contributor? EmpresaPropietaria { get; set; }
    }
}

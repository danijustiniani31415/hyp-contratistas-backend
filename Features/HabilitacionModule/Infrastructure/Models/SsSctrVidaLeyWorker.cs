using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Infrastructure.Models;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("ss_sctr_vidaley_worker")]
    public class SsSctrVidaLeyWorker
    {
        public int Id { get; set; }
        [Column("sctr_vidaley_id")]
        public int SctrVidaLeyId { get; set; }
        public int WorkerId { get; set; }
        public DateTime? FechaInicioCobertura { get; set; }

        [ForeignKey(nameof(SctrVidaLeyId))]
        public SsSctrVidaley? SctrVidaley { get; set; }

        [ForeignKey(nameof(WorkerId))]
        public Worker? Worker { get; set; }
    }
}

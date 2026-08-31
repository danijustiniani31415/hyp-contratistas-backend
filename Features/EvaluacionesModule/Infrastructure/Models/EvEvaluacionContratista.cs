using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.CostsModule.Shared.Models;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Models
{
    [Table("ev_evaluacion_contratista")]
    public class EvEvaluacionContratista
    {
        public int Id { get; set; }
        public int PeriodoId { get; set; }
        public int? ProyectoId { get; set; }
        public int? ContributorId { get; set; }
        public string AreaNombre { get; set; } = string.Empty;
        public int EvaluadorUserId { get; set; }
        public decimal? Nota { get; set; }
        public string? Comentario { get; set; }
        // No corresponde evaluar a ningun contratista este periodo (ej. sin contratistas
        // con tareo asignado a su area/proyecto), distinto del EsNa por criterio.
        public bool NoAplica { get; set; }
        public string? NoAplicaMotivo { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(PeriodoId))]
        public EvPeriodo? Periodo { get; set; }

        [ForeignKey(nameof(ProyectoId))]
        public Project? Proyecto { get; set; }

        [ForeignKey(nameof(ContributorId))]
        public Contributor? Contributor { get; set; }

        public ICollection<EvEvaluacionContratistaDetalle> Detalles { get; set; } = [];
    }
}

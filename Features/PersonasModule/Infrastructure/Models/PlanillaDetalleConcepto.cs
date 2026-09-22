using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.PersonasModule.Infrastructure.Models
{
    /// <summary>Una línea (ingreso/descuento/aporte) dentro de una boleta calculada.</summary>
    [Table("lb_planilla_detalle_concepto")]
    public class PlanillaDetalleConcepto
    {
        public long Id { get; set; }
        public long DetalleId { get; set; }
        public int ConceptoPlanillaId { get; set; }
        public string ConceptoNombre { get; set; } = null!;
        public string Tipo { get; set; } = null!;
        public decimal Monto { get; set; }

        [ForeignKey(nameof(DetalleId))]
        public PlanillaDetalle? Detalle { get; set; }
        [ForeignKey(nameof(ConceptoPlanillaId))]
        public ConceptoPlanilla? ConceptoPlanilla { get; set; }
    }
}

using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.PersonasModule.Infrastructure.Models
{
    /// <summary>
    /// Boleta calculada de UNA persona en UN período — equivale a hr.payslip en Odoo. Los campos
    /// snapshot (categoría, sueldo, jornal) quedan congelados al momento del cálculo: si luego
    /// cambian los datos de planilla de la persona, esta boleta histórica no se altera.
    /// </summary>
    [Table("lb_planilla_detalle")]
    public class PlanillaDetalle
    {
        public long Id { get; set; }
        public int PeriodoId { get; set; }
        public int PersonaId { get; set; }
        public string? CategoriaLaboral { get; set; }
        public decimal? SueldoBase { get; set; }
        public decimal? Jornal { get; set; }
        public decimal DiasTrabajados { get; set; }
        public decimal DiasFalta { get; set; }
        public decimal TotalIngresos { get; set; }
        public decimal TotalDescuentos { get; set; }
        public decimal TotalAportesEmpleador { get; set; }
        public decimal NetoPagar { get; set; }
        public DateTimeOffset CreadoEn { get; set; }

        [ForeignKey(nameof(PeriodoId))]
        public PlanillaPeriodo? Periodo { get; set; }
        [ForeignKey(nameof(PersonaId))]
        public Persona? Persona { get; set; }

        public ICollection<PlanillaDetalleConcepto> Conceptos { get; set; } = new List<PlanillaDetalleConcepto>();
    }
}

using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.PersonasModule.Infrastructure.Models
{
    /// <summary>Un mes de planilla a calcular — equivale a hr.payslip.run en Odoo.</summary>
    [Table("lb_planilla_periodo")]
    public class PlanillaPeriodo
    {
        public int Id { get; set; }
        public int Anio { get; set; }
        public int Mes { get; set; }
        /// <summary>Null = todos los proyectos en un solo período.</summary>
        public int? ProyectoId { get; set; }
        /// <summary>BORRADOR, CALCULADO o CERRADO.</summary>
        public string Estado { get; set; } = "BORRADOR";
        public DateTimeOffset? CalculadoEn { get; set; }
        public long? CalculadoPorUsuarioSistemaId { get; set; }
        public DateTimeOffset? CerradoEn { get; set; }
        public DateTimeOffset CreadoEn { get; set; }

        [ForeignKey(nameof(ProyectoId))]
        public Proyecto? Proyecto { get; set; }

        public ICollection<PlanillaDetalle> Detalles { get; set; } = new List<PlanillaDetalle>();
    }
}

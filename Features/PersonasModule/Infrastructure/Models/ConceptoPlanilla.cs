using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.PersonasModule.Infrastructure.Models
{
    /// <summary>
    /// Regla de cálculo de planilla configurable desde el frontend — equivale a salary.rule en
    /// Odoo. Las tasas (AFP, ONP, EsSalud, etc.) NUNCA van hardcodeadas en código: el usuario las
    /// define y actualiza acá, porque cambian por ley y porque cada AFP tiene su propia comisión.
    /// </summary>
    [Table("lb_concepto_planilla")]
    public class ConceptoPlanilla
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        /// <summary>INGRESO, DESCUENTO o APORTE_EMPLEADOR (informativo, no reduce el neto a pagar).</summary>
        public string Tipo { get; set; } = null!;
        /// <summary>OBRERO, EMPLEADO o null (ambas).</summary>
        public string? CategoriaLaboral { get; set; }
        /// <summary>FIJO, PORCENTAJE_SUELDO, PORCENTAJE_JORNAL o POR_DIA_TAREO.</summary>
        public string FormaCalculo { get; set; } = null!;
        public decimal Valor { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; } = true;
        public DateTimeOffset CreadoEn { get; set; }
    }
}

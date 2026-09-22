using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.PersonasModule.Infrastructure.Models
{
    /// <summary>
    /// Datos fijos de planilla (Fase 1 del motor de Planillas) — equivale a los campos fijos de
    /// hr.contract en Odoo. 1:1 con Persona. Todo opcional: se llena cuando se tenga la info.
    /// </summary>
    [Table("lb_persona_planilla")]
    public class PersonaPlanilla
    {
        public int PersonaId { get; set; }
        public string? CodigoTrabajador { get; set; }
        public string? Banco { get; set; }
        public string? NumeroCuenta { get; set; }
        public string? Cusp { get; set; }
        public string? TipoAfpOnp { get; set; }
        /// <summary>OBRERO o EMPLEADO — define qué formato de boleta le corresponde (BOLT_OBR / BOLET_EMPL en el Excel de H&P).</summary>
        public string? CategoriaLaboral { get; set; }
        public decimal? SueldoBase { get; set; }
        public decimal? Jornal { get; set; }
        public bool AsignacionFamiliar { get; set; }
        public bool Sctr { get; set; }
        public DateTimeOffset ActualizadoEn { get; set; }

        [ForeignKey(nameof(PersonaId))]
        public Persona? Persona { get; set; }
    }
}

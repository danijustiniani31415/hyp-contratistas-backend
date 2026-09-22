using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.PersonasModule.Infrastructure.Models
{
    /// <summary>Fase 2 del motor de Planillas — asistencia diaria, equivale a hr.attendance en Odoo.</summary>
    [Table("lb_tareo")]
    public class Tareo
    {
        public long Id { get; set; }
        public int PersonaId { get; set; }
        public DateOnly Fecha { get; set; }
        /// <summary>NORMAL, DL (descanso libre), F (falta), P (permiso), VC (vacaciones), DM (descanso médico).</summary>
        public string TipoDia { get; set; } = "NORMAL";
        public decimal? HorasTrabajadas { get; set; }
        public decimal HorasExtra { get; set; }
        public long? RegistradoPorUsuarioSistemaId { get; set; }
        public DateTimeOffset CreadoEn { get; set; }

        [ForeignKey(nameof(PersonaId))]
        public Persona? Persona { get; set; }
    }
}

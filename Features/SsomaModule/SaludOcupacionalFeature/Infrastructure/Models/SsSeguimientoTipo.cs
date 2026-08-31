using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    /// <summary>
    /// Catálogo de tipos de seguimiento de un caso de descanso médico. Reemplaza la lista
    /// hardcodeada que vivía en el frontend (descanso-modal.component.ts): 'Médico',
    /// 'Asistenta Social', 'Seguimiento', 'Alta'.
    /// </summary>
    [Table("ss_seguimiento_tipo")]
    public class SsSeguimientoTipo
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("orden")]
        public int Orden { get; set; }

        [Column("active")]
        public bool Active { get; set; } = true;
    }
}

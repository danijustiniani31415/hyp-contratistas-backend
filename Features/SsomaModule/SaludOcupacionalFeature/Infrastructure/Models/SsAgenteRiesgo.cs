using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_agente_riesgo")]
    public class SsAgenteRiesgo
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        // Físico | Químico | Biológico | Ergonómico | Psicosocial | Disergonómico
        [Column("tipo")]
        public string Tipo { get; set; } = string.Empty;

        [Column("activo")]
        public bool Activo { get; set; } = true;
    }
}

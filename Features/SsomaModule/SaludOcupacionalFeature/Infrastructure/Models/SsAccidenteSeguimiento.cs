using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_accidente_seguimiento")]
    public class SsAccidenteSeguimiento
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("accidente_id")]
        public int AccidenteId { get; set; }

        [Column("fecha")]
        public DateOnly Fecha { get; set; }

        [Column("tipo")]
        public string? Tipo { get; set; }

        [Column("descripcion")]
        public string Descripcion { get; set; } = string.Empty;

        [Column("proxima_cita")]
        public DateOnly? ProximaCita { get; set; }

        [Column("registrado_por_id")]
        public int RegistradoPorId { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [ForeignKey(nameof(AccidenteId))]
        public SsAccidenteTrabajo? Accidente { get; set; }
    }
}

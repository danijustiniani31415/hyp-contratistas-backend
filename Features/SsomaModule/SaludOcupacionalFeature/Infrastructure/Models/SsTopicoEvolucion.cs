using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_topico_evolucion")]
    public class SsTopicoEvolucion
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("atencion_id")]
        public int AtencionId { get; set; }

        [Column("fecha_evolucion")]
        public DateTimeOffset FechaEvolucion { get; set; } = DateTimeOffset.UtcNow;

        [Column("nota_evolucion")]
        public string NotaEvolucion { get; set; } = string.Empty;

        [Column("registrado_por_id")]
        public int? RegistradoPorId { get; set; }

        [Column("url_evidencia")]
        public string? UrlEvidencia { get; set; }

        [Column("state")]
        public bool State { get; set; } = true;

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [ForeignKey(nameof(AtencionId))]
        public TopicoAtencion? Atencion { get; set; }
    }
}

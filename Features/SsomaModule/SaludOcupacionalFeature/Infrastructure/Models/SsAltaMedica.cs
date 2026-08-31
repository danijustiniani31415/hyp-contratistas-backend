using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_alta_medica")]
    public class SsAltaMedica
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("accidente_id")]
        public int AccidenteId { get; set; }

        [Column("tipo_id")]
        public int TipoId { get; set; }

        [Column("fecha_alta")]
        public DateOnly FechaAlta { get; set; }

        [Column("medico")]
        public string? Medico { get; set; }

        [Column("diagnostico_final")]
        public string? DiagnosticoFinal { get; set; }

        [Column("tiene_restriccion")]
        public bool TieneRestriccion { get; set; } = false;

        [Column("descripcion_restriccion")]
        public string? DescripcionRestriccion { get; set; }

        [Column("fecha_fin_restriccion")]
        public DateOnly? FechaFinRestriccion { get; set; }

        [Column("url_certificado")]
        public string? UrlCertificado { get; set; }

        [Column("observaciones")]
        public string? Observaciones { get; set; }

        [Column("registrado_por_id")]
        public int RegistradoPorId { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [Column("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        [Column("state")]
        public bool State { get; set; } = true;

        [ForeignKey(nameof(AccidenteId))]
        public SsAccidenteTrabajo? Accidente { get; set; }

        [ForeignKey(nameof(TipoId))]
        public SsAltaTipo? Tipo { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_cita_medica")]
    public class SsCitaMedica
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("accidente_id")]
        public int AccidenteId { get; set; }

        [Column("tipo_id")]
        public int TipoId { get; set; }

        [Column("fecha_cita")]
        public DateOnly FechaCita { get; set; }

        [Column("hora_cita")]
        public TimeOnly? HoraCita { get; set; }

        [Column("clinica")]
        public string? Clinica { get; set; }

        [Column("medico")]
        public string? Medico { get; set; }

        [Column("diagnostico")]
        public string? Diagnostico { get; set; }

        [Column("indicaciones")]
        public string? Indicaciones { get; set; }

        [Column("proxima_cita")]
        public DateOnly? ProximaCita { get; set; }

        [Column("url_evidencia")]
        public string? UrlEvidencia { get; set; }

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
        public SsCitaTipo? Tipo { get; set; }
    }
}

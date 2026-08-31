using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_sctr_gestion")]
    public class SsSctrGestion
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("caso_social_id")]
        public Guid CasoSocialId { get; set; }

        [Column("numero_siniestro")]
        public string? NumeroSiniestro { get; set; }

        [Column("fecha_reporte_sctr")]
        public DateOnly? FechaReporteSctr { get; set; }

        [Column("fecha_atencion_sctr")]
        public DateOnly? FechaAtencionSctr { get; set; }

        [Column("aseguradora")]
        public string? Aseguradora { get; set; }

        [Column("monto_cubierto")]
        public decimal? MontoCubierto { get; set; }

        [Column("url_hoja_atencion")]
        public string? UrlHojaAtencion { get; set; }

        [Column("url_documentos_adicionales")]
        public string? UrlDocumentosAdicionales { get; set; }

        [Column("estado_id")]
        public int EstadoId { get; set; }

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

        [ForeignKey(nameof(CasoSocialId))]
        public SsCasoSocial? CasoSocial { get; set; }

        [ForeignKey(nameof(EstadoId))]
        public SsSctrEstado? Estado { get; set; }
    }
}

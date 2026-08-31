using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    /// <summary>
    /// Auditoría de cada acto de firma de una convalidación: quién, cuándo, desde dónde
    /// (IP/user-agent) y sobre qué versión exacta del documento (hash), para sostener la
    /// validez de la firma electrónica del médico ante una eventual disputa.
    /// </summary>
    [Table("ss_convalidacion_firma_log")]
    public class SsConvalidacionFirmaLog
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("convalidacion_id")]
        public int ConvalidacionId { get; set; }

        [Column("medico_id")]
        public int? MedicoId { get; set; }

        [Column("fecha_hora")]
        public DateTimeOffset FechaHora { get; set; }

        [Column("ip")]
        public string? Ip { get; set; }

        [Column("user_agent")]
        public string? UserAgent { get; set; }

        /// <summary>SHA-256 de los datos exactos de la convalidación en el momento de la firma
        /// (resultado, fechas, empresa destino, puesto destino, médico) — permite demostrar que
        /// el documento no fue alterado después de firmado.</summary>
        [Column("documento_hash")]
        public string DocumentoHash { get; set; } = string.Empty;

        [Column("resultado")]
        public string Resultado { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }
    }
}

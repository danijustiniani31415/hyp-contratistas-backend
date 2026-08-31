using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_clinica_tokens")]
    public class SsClinicaToken
    {
        [Column("token_id")]
        public int TokenId { get; set; }

        [Column("clinica_usuario_id")]
        public int ClinicaUsuarioId { get; set; }

        [Column("token")]
        public string Token { get; set; } = string.Empty;

        [Column("tipo")]
        public string Tipo { get; set; } = string.Empty;

        [Column("expiracion")]
        public DateTime Expiracion { get; set; }

        [Column("usado_en")]
        public DateTime? UsadoEn { get; set; }

        [Column("creado_en")]
        public DateTime CreadoEn { get; set; }

        [Column("ip_solicitud")]
        public string? IpSolicitud { get; set; }

        [ForeignKey(nameof(ClinicaUsuarioId))]
        public SsClinicaUsuario? ClinicaUsuario { get; set; }
    }
}

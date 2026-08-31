using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_clinica_auditoria")]
    public class SsClinicaAuditoria
    {
        [Column("auditoria_id")]
        public int AuditoriaId { get; set; }

        [Column("clinica_usuario_id")]
        public int? ClinicaUsuarioId { get; set; }

        [Column("clinica_id")]
        public int? ClinicaId { get; set; }

        [Column("accion")]
        public string Accion { get; set; } = string.Empty;

        [Column("realizado_en")]
        public DateTime RealizadoEn { get; set; }

        [Column("ip_origen")]
        public string? IpOrigen { get; set; }

        [Column("user_agent")]
        public string? UserAgent { get; set; }

        [Column("detalle_adicional")]
        public string? DetalleAdicional { get; set; }

        [ForeignKey(nameof(ClinicaUsuarioId))]
        public SsClinicaUsuario? ClinicaUsuario { get; set; }
    }
}

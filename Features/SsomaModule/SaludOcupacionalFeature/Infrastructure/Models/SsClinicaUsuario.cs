using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_clinica_usuarios")]
    public class SsClinicaUsuario
    {
        [Column("clinica_usuario_id")]
        public int ClinicaUsuarioId { get; set; }

        [Column("clinica_id")]
        public int ClinicaId { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("password_hash")]
        public string PasswordHash { get; set; } = "PENDIENTE_RESET";

        [Column("activo")]
        public bool Activo { get; set; } = true;

        [Column("ultimo_acceso")]
        public DateTime? UltimoAcceso { get; set; }

        [Column("creado_en")]
        public DateTime CreadoEn { get; set; }

        [Column("creado_por")]
        public int? CreadoPor { get; set; }

        [Column("modificado_en")]
        public DateTime? ModificadoEn { get; set; }

        [Column("modificado_por")]
        public int? ModificadoPor { get; set; }

        [Column("desactivado_en")]
        public DateTime? DesactivadoEn { get; set; }

        [Column("desactivado_por")]
        public int? DesactivadoPor { get; set; }

        [ForeignKey(nameof(ClinicaId))]
        public SsClinica? Clinica { get; set; }
    }
}

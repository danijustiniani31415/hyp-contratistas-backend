using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    /// <summary>
    /// Registro de alertas enviadas por el cron de SSOMA (accidentes, descansos, casos sociales).
    /// Previene duplicados al verificar (tipo_alerta, referencia_id, fecha_alerta) antes de enviar.
    /// </summary>
    [Table("ss_alertas_ssoma")]
    public class SsAlertaSsoma
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>ACCIDENTE_SIN_ALTA | DESCANSO_VENCIDO | REINDUCCION_PENDIENTE | CASO_SOCIAL_SIN_SEGUIMIENTO</summary>
        [Column("tipo_alerta")]
        public string TipoAlerta { get; set; } = string.Empty;

        /// <summary>ID del registro afectado (int o Guid convertido a string).</summary>
        [Column("referencia_id")]
        public string ReferenciaId { get; set; } = string.Empty;

        [Column("fecha_alerta")]
        public DateOnly FechaAlerta { get; set; }

        [Column("enviado_email")]
        public bool EnviadoEmail { get; set; }

        [Column("fecha_envio")]
        public DateTimeOffset? FechaEnvio { get; set; }

        [Column("destinatarios")]
        public string? Destinatarios { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}

using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.PersonasModule.Infrastructure.Models;

namespace Abril_Backend.Features.NotificacionesModule.Infrastructure.Models
{
    /// <summary>Bitácora de envío — sin esto no hay forma de auditar "¿le llegó el aviso al
    /// Residente o no?" (CONTEXT_LOGISTICA.md sección 5). También sirve para no reenviar la
    /// misma alerta de SLA cada vez que corre el job.</summary>
    [Table("lb_notif_envio")]
    public class NotifEnvio
    {
        public long Id { get; set; }
        public int NotifReglaId { get; set; }
        public long? UsuarioSistemaId { get; set; }
        public string EmailDestino { get; set; } = null!;
        public string Canal { get; set; } = null!;
        public string Estado { get; set; } = "PENDIENTE";
        public string? Error { get; set; }
        public string? ReferenciaTipo { get; set; }
        public long? ReferenciaId { get; set; }
        public DateTimeOffset CreadoEn { get; set; }
        public DateTimeOffset? EnviadoEn { get; set; }

        [ForeignKey(nameof(NotifReglaId))]
        public NotifRegla? NotifRegla { get; set; }
        [ForeignKey(nameof(UsuarioSistemaId))]
        public UsuarioSistema? UsuarioSistema { get; set; }
    }
}

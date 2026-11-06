using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.PersonasModule.Infrastructure.Models;

namespace Abril_Backend.Features.NotificacionesModule.Infrastructure.Models
{
    /// <summary>A quién avisar cuando ocurre un evento — nunca hardcodeado en código
    /// (CONTEXT_LOGISTICA.md sección 5). Una fila por combinación evento+rol.</summary>
    [Table("lb_notif_regla")]
    public class NotifRegla
    {
        public int Id { get; set; }
        public string Evento { get; set; } = null!;
        public short? RolDestinoId { get; set; }
        /// <summary>GLOBAL, MISMO_PROYECTO, SOLICITANTE.</summary>
        public string ScopeType { get; set; } = "MISMO_PROYECTO";
        public string Canal { get; set; } = "EMAIL";
        public bool Activo { get; set; } = true;

        [ForeignKey(nameof(RolDestinoId))]
        public Rol? RolDestino { get; set; }
    }
}

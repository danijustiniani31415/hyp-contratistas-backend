using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.NotificacionesModule.Infrastructure.Models
{
    /// <summary>Email puntual adicional para una regla (ej. Contabilidad, Auditoría externa) —
    /// se suma a los destinatarios resueltos por rol, no los reemplaza.</summary>
    [Table("lb_notif_destinatario_extra")]
    public class NotifDestinatarioExtra
    {
        public int Id { get; set; }
        public int NotifReglaId { get; set; }
        public string Email { get; set; } = null!;
        public string? Descripcion { get; set; }

        [ForeignKey(nameof(NotifReglaId))]
        public NotifRegla? NotifRegla { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("ss_retiro_automatico_log")]
    public class SsRetiroAutomaticoLog
    {
        [Key]
        public int Id { get; set; }
        public int WorkerId { get; set; }
        public int? EmpresaId { get; set; }
        public string? Motivo { get; set; }
        public DateTimeOffset EjecutadoEn { get; set; } = DateTimeOffset.UtcNow;
        [MaxLength(50)]
        public string TipoRetiro { get; set; } = string.Empty;
        public int? DiasGracia { get; set; }
        public string? EntregablesVencidos { get; set; }
    }
}

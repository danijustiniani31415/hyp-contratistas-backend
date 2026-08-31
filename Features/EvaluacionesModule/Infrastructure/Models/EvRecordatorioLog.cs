using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Models
{
    [Table("ev_recordatorio_log")]
    public class EvRecordatorioLog
    {
        public int Id { get; set; }
        public int PeriodoId { get; set; }
        public int? UserId { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string? EmailDestino { get; set; }
        public bool CcJefatura { get; set; } = false;
        public bool CcGerencia { get; set; } = false;
        public DateTime EnviadoAt { get; set; } = DateTime.UtcNow;
    }
}

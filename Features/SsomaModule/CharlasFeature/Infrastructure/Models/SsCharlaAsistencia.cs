using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.SsomaModule.CharlasFeature.Infrastructure.Models;

[Table("ss_charla_asistencia")]
public class SsCharlaAsistencia
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("charla_id")] public int CharlaId { get; set; }
    [Column("worker_id")] public int WorkerId { get; set; }
    [Column("asistio")] public bool Asistio { get; set; } = true;
    [Column("registrado_por_id")] public int? RegistradoPorId { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")] public DateTime? UpdatedAt { get; set; }
    [Column("state")] public bool State { get; set; } = true;

    public virtual SsCharla? Charla { get; set; }
}

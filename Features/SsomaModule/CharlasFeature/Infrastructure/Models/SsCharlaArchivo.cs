using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.SsomaModule.CharlasFeature.Infrastructure.Models;

[Table("ss_charla_archivo")]
public class SsCharlaArchivo
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("charla_id")] public int CharlaId { get; set; }
    [Column("url")][MaxLength(1000)] public string Url { get; set; } = string.Empty;
    [Column("nombre")][MaxLength(300)] public string Nombre { get; set; } = string.Empty;
    [Column("sp_id")][MaxLength(200)] public string? SpId { get; set; }
    [Column("state")] public bool State { get; set; } = true;
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CharlaId))]
    public virtual SsCharla Charla { get; set; } = null!;
}

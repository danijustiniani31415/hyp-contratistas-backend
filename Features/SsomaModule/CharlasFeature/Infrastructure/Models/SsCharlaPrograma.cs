using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.SsomaModule.CharlasFeature.Infrastructure.Models;

[Table("ss_charla_programa")]
public class SsCharlaPrograma
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("proyecto_id")] public int ProyectoId { get; set; }
    [Column("mes")] public int Mes { get; set; }
    [Column("anio")] public int Anio { get; set; }
    [Column("nombre")][MaxLength(200)] public string Nombre { get; set; } = "Programa Charlas Staff";
    [Column("descripcion")] public string? Descripcion { get; set; }
    [Column("estado")][MaxLength(20)] public string Estado { get; set; } = "Borrador";
    [Column("creado_por_id")] public int? CreadoPorId { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")] public DateTime? UpdatedAt { get; set; }
    [Column("state")] public bool State { get; set; } = true;

    public virtual ICollection<SsCharla> Charlas { get; set; } = new List<SsCharla>();
}

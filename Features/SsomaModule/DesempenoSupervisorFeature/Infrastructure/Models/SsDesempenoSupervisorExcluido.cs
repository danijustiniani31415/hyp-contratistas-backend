using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.SsomaModule.DesempenoSupervisorFeature.Infrastructure.Models;

/// <summary>
/// Trabajadores excluidos manualmente del indicador "Desempeño Supervisor"
/// (ej. de licencia, en proceso de salida, rol administrativo sin campo).
/// Exclusión global, no por mes/proyecto — si vuelve a aplicar, se quita de aquí.
/// </summary>
[Table("ss_desempeno_supervisor_excluido")]
public class SsDesempenoSupervisorExcluido
{
    [Key]
    [Column("worker_id")]
    public int WorkerId { get; set; }

    [Column("motivo")]
    public string? Motivo { get; set; }

    [Column("excluido_por")]
    public int? ExcluidoPor { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

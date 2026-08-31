using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Infrastructure.Models;

/// <summary>
/// Empresas excluidas manualmente del indicador "Seguimiento de Indicadores Proactivos"
/// (ej. sin supervisores asignados, meta no representativa). Exclusión global, no por
/// mes/proyecto — si vuelve a aplicar, se quita de aquí.
/// </summary>
[Table("ss_indicador_empresa_excluida")]
public class SsIndicadorEmpresaExcluida
{
    [Key]
    [Column("empresa_id")]
    public int EmpresaId { get; set; }

    [Column("motivo")]
    public string? Motivo { get; set; }

    [Column("excluido_por")]
    public int? ExcluidoPor { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

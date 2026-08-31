using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Features.SsomaModule.InspeccionFeature.Infrastructure.Models;
using Abril_Backend.Features.CostsModule.Shared.Models;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Infrastructure.Models;

/// <summary>
/// Configuración de qué tipos de inspección aplican a cada empresa (contratista o casa)
/// por proyecto y mes. El coordinador SSOMA la configura para casa; cada contratista
/// configura la suya desde su portal.
/// Tabla: ssoma_prog_inspeccion_empresa
/// </summary>
[Table("ssoma_prog_inspeccion_empresa")]
public class SsomaProgInspeccionEmpresa
{
    public int Id { get; set; }

    public int ProyectoId { get; set; }

    /// <summary>null cuando EmpresaTipo = "Casa"</summary>
    public int? EmpresaId { get; set; }

    /// <summary>"Casa" | "Contratista"</summary>
    public string EmpresaTipo { get; set; } = "Contratista";

    public int Mes { get; set; }
    public int Anio { get; set; }

    public int InspeccionTipoId { get; set; }

    public int? CreadoPor { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(ProyectoId))]
    public Project? Proyecto { get; set; }

    [ForeignKey(nameof(EmpresaId))]
    public Contributor? Empresa { get; set; }

    [ForeignKey(nameof(InspeccionTipoId))]
    public SsomaInspeccionTipo? InspeccionTipo { get; set; }
}

/// <summary>
/// Meta anual de indicadores reactivos (IF/IG/IA), definida manualmente por SSOMA cada año
/// (ej. "10% menos que el año anterior en frecuencia y gravedad"). Un registro por año;
/// null en algún campo significa que esa meta todavía no se configuró.
/// Tabla: ssoma_meta_anual
/// </summary>
[Table("ssoma_meta_anual")]
public class SsomaMetaAnual
{
    public int Id { get; set; }
    public int Anio { get; set; }
    public decimal? MetaIndiceFrecuencia { get; set; }
    public decimal? MetaIndiceGravedad { get; set; }
    public decimal? MetaIndiceAccidentabilidad { get; set; }
    public int? ActualizadoPorId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

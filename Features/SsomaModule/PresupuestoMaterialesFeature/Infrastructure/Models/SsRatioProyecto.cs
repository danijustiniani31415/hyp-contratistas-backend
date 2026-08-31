using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Models;

public class SsRatioProyecto
{
    public int Id { get; set; }
    public int FamiliaId { get; set; }
    public int ProjectId { get; set; }
    public string VariableBase { get; set; } = null!;
    public decimal CantidadTotal { get; set; }
    public decimal PrecioUnitarioPromedio { get; set; }
    public decimal ValorDriver { get; set; }
    public decimal RatioCantidad { get; set; }
    public bool EsOutlier { get; set; } = false;
    /// <summary>Si es false, este proyecto se excluye manualmente del cálculo del RATIO (mediana) recomendado, aunque no sea outlier.</summary>
    public bool IncluidoManualRatio { get; set; } = true;
    /// <summary>Si es false, este proyecto se excluye manualmente del cálculo del PRECIO promedio recomendado, aunque no sea outlier.</summary>
    public bool IncluidoManualPrecio { get; set; } = true;
    public DateTimeOffset CalculadoEn { get; set; } = DateTimeOffset.UtcNow;

    public SsMaterialFamilia Familia { get; set; } = null!;
    public Project Proyecto { get; set; } = null!;
}

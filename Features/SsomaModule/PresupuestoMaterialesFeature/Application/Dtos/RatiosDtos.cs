namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;

// ─── Cálculo de ratios ────────────────────────────────────────────────────────

public class CalcularRatiosResultDto
{
    public int ProjectId { get; set; }
    public string ProjectDescription { get; set; } = null!;
    public int RatiosCalculados { get; set; }
    public int FamiliasSinDriver { get; set; }
    public List<string> Advertencias { get; set; } = [];
}

/// <summary>Resultado de calcular ratios de todos los proyectos con consumo estandarizado de una vez.</summary>
public class CalcularRatiosTodosResultDto
{
    public int TotalProyectosProcesados { get; set; }
    public List<CalcularRatiosResultDto> Proyectos { get; set; } = [];
}

public class RatioProyectoDto
{
    public int Id { get; set; }
    public int FamiliaId { get; set; }
    public string NombreFamilia { get; set; } = null!;
    public string TipoMaterial { get; set; } = null!;
    public int ProjectId { get; set; }
    public string ProjectDescription { get; set; } = null!;
    public string VariableBase { get; set; } = null!;
    public decimal CantidadTotal { get; set; }
    public decimal PrecioUnitarioPromedio { get; set; }
    public decimal ValorDriver { get; set; }
    public decimal RatioCantidad { get; set; }
    public bool EsOutlier { get; set; }
    public bool IncluidoManualRatio { get; set; } = true;
    public bool IncluidoManualPrecio { get; set; } = true;
}

// ─── Comparación entre proyectos ──────────────────────────────────────────────

public class RatioFamiliaComparacionDto
{
    public int FamiliaId { get; set; }
    public string NombreFamilia { get; set; } = null!;
    public string TipoMaterial { get; set; } = null!;
    public string VariableBase { get; set; } = null!;
    public List<RatioProyectoItemDto> Proyectos { get; set; } = [];
    public decimal PromedioRatio { get; set; }
    public decimal MedianaRatio { get; set; }
    public decimal MinRatio { get; set; }
    public decimal MaxRatio { get; set; }
    public decimal PromedioPrecioUnitario { get; set; }
}

public class RatioProyectoItemDto
{
    public int ProjectId { get; set; }
    public string ProjectDescription { get; set; } = null!;
    public decimal RatioCantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal CantidadTotal { get; set; }
    public decimal ValorDriver { get; set; }
    public bool EsOutlier { get; set; }
    public bool IncluidoManualRatio { get; set; } = true;
    public bool IncluidoManualPrecio { get; set; } = true;
}

public class ActualizarIncluidoManualDto
{
    public bool Incluir { get; set; }
    /// <summary>RATIO | PRECIO — cuál cálculo afecta esta inclusión/exclusión.</summary>
    public string Campo { get; set; } = "RATIO";
}

public class FamiliaConRatioDto
{
    public int FamiliaId { get; set; }
    public string NombreFamilia { get; set; } = null!;
    public string TipoMaterial { get; set; } = null!;
    public string VariableBase { get; set; } = null!;
    public int NProyectos { get; set; }
    public int NOutliers { get; set; }
}

// ─── Resumen general ──────────────────────────────────────────────────────────

public class ResumenRatiosDto
{
    public List<ResumenProyectoRatioDto> Proyectos { get; set; } = [];
    public int TotalFamilias { get; set; }
}

public class ResumenProyectoRatioDto
{
    public int ProjectId { get; set; }
    public string ProjectDescription { get; set; } = null!;
    public int FamiliasCalculadas { get; set; }
    public decimal TotalGastoSsoma { get; set; }
    public DateOnly? FechaMin { get; set; }
    public DateOnly? FechaMax { get; set; }
}

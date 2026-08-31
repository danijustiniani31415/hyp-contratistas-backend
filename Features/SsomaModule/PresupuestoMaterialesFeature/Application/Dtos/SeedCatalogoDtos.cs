namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;

/// <summary>Una fila cruda del catálogo (espejo de MaterialesEstandarizados3.csv).</summary>
public class SeedMaterialItemDto
{
    public string Recurso { get; set; } = null!;
    public string NomStd1 { get; set; } = null!;
    public string NomStd2 { get; set; } = null!;
    public string TipoMaterial { get; set; } = null!;
    public bool PerteneceSsoma { get; set; }
    public string VariableBase { get; set; } = null!;
    /// <summary>Unidades reales contenidas en 1 "Recurso" comprado (columna CantidadComprada del CSV). Default 1.</summary>
    public decimal CantidadComprada { get; set; } = 1;
}

public class SeedCatalogoRequestDto
{
    public List<SeedMaterialItemDto> Items { get; set; } = [];
}

public class SeedCatalogoResultDto
{
    public int TiposCreados { get; set; }
    public int FamiliasCreadas { get; set; }
    public int FamiliasExistentes { get; set; }
    public int ItemsCreados { get; set; }
    public int ItemsExistentes { get; set; }
    public int AliasCreados { get; set; }
    public List<string> Advertencias { get; set; } = [];
}

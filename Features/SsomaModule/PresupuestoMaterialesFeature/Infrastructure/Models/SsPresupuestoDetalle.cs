namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Models;

public class SsPresupuestoDetalle
{
    public int     Id                { get; set; }
    public int     PresupuestoId     { get; set; }
    public int     FamiliaId         { get; set; }
    public int     TipoId            { get; set; }
    public string  VariableBase      { get; set; } = null!;
    public decimal RatioRecomendado  { get; set; }
    public int     NProyectosBase    { get; set; }
    public decimal ValorDriver       { get; set; }
    public decimal CantidadEstimada  { get; set; }
    public decimal PrecioUnitario    { get; set; }
    public decimal TotalEstimado     { get; set; }
    public bool    TieneHistoria     { get; set; }
    public decimal? CantidadManual   { get; set; }
    public decimal? PrecioManual     { get; set; }
    public string? NotasLinea        { get; set; }

    public SsPresupuesto    Presupuesto { get; set; } = null!;
    public SsMaterialFamilia Familia   { get; set; } = null!;
}

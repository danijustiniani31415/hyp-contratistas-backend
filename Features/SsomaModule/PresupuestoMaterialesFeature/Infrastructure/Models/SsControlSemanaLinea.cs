namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Models;

public class SsControlSemanaLinea
{
    public int     Id             { get; set; }
    public int     ControlId      { get; set; }
    public int     FamiliaId      { get; set; }
    public decimal CantidadReal   { get; set; }
    public decimal? PrecioUnitario { get; set; }
    public decimal TotalReal      { get; set; } // GENERATED ALWAYS AS (cantidad_real * COALESCE(precio_unitario, 0)) STORED
    public string? Notas          { get; set; }
}

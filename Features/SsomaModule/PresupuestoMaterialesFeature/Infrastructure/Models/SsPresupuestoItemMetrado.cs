namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Models;

public class SsPresupuestoItemMetrado
{
    public int Id { get; set; }
    public int PresupuestoId { get; set; }
    public int FamiliaId { get; set; }
    public decimal Metrado { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Total { get; set; }
    public string? Descripcion { get; set; }

    public SsPresupuesto Presupuesto { get; set; } = null!;
    public SsMaterialFamilia Familia { get; set; } = null!;
}

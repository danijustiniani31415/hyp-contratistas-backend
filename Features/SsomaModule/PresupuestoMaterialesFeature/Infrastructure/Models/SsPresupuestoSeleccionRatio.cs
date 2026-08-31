namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Models;

public class SsPresupuestoSeleccionRatio
{
    public int PresupuestoId { get; set; }
    public int FamiliaId { get; set; }
    public int ProjectId { get; set; }
    public bool Incluido { get; set; } = true;

    public SsPresupuesto Presupuesto { get; set; } = null!;
    public SsMaterialFamilia Familia { get; set; } = null!;
}

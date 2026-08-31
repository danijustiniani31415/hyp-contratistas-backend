using Abril_Backend.Infrastructure.Models;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Models;

public class SsPresupuestoPersonalHito
{
    public int Id { get; set; }
    public int PresupuestoId { get; set; }
    /// <summary>Apunta al hito REAL del cronograma del proyecto (MilestoneSchedule).</summary>
    public int HitoId { get; set; }
    // VIGIA | MONITOR | PREVENCIONISTA
    public string Rol { get; set; } = null!;
    public int Cantidad { get; set; }
    public decimal Semanas { get; set; }
    public decimal CostoMensual { get; set; }
    public decimal Total { get; set; }

    public SsPresupuesto Presupuesto { get; set; } = null!;
    public MilestoneSchedule Hito { get; set; } = null!;
}

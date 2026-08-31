using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Models;

public class SsPresupuesto
{
    public int    Id              { get; set; }
    public int    ProjectId       { get; set; }
    public int    Version         { get; set; } = 1;
    public string Estado          { get; set; } = "BORRADOR";   // BORRADOR | APROBADO
    public decimal HhUsado        { get; set; }
    public decimal AreaUsada      { get; set; }
    public int    TrabajadoresUsados { get; set; }
    public decimal TotalEstimado  { get; set; }
    public int?   GeneradoPor     { get; set; }
    public DateTimeOffset GeneradoEn  { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AprobadoEn { get; set; }
    public string? Notas          { get; set; }

    public Project Proyecto       { get; set; } = null!;
    public ICollection<SsPresupuestoDetalle>       Detalles        { get; set; } = [];
    public ICollection<SsPresupuestoSeleccionRatio> SeleccionesRatio { get; set; } = [];
    public ICollection<SsPresupuestoItemMetrado>   ItemsMetrado    { get; set; } = [];
    public ICollection<SsPresupuestoPersonalHito>  PersonalHitos   { get; set; } = [];
}

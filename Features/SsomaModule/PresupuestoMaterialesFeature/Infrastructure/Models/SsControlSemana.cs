namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Models;

public class SsControlSemana
{
    public int     Id            { get; set; }
    public int     PresupuestoId { get; set; }
    public int     ProjectId     { get; set; }
    public int     SemanaNum     { get; set; }
    public DateOnly FechaInicio  { get; set; }
    public DateOnly FechaFin     { get; set; }
    public string  Estado        { get; set; } = "ABIERTO";   // ABIERTO | CERRADO
    public string? Observaciones { get; set; }
    public int?    RegistradoPor { get; set; }
    public DateTimeOffset RegistradoEn { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CerradoEn   { get; set; }

    public SsPresupuesto Presupuesto { get; set; } = null!;
}

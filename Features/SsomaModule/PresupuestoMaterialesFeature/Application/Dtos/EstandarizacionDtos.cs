namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;

public class EstandarizacionLineaDto
{
    public long LineaId { get; set; }
    public string RecursoCrudo { get; set; } = null!;
    /// <summary>AUTO_ALIAS | AUTO_EXACTO | AUTO_FUZZY | AUTO_RECHAZADO | REVISION | SIN_MATCH</summary>
    public string Resultado { get; set; } = null!;
    public int? ItemId { get; set; }
    public string? NombreItem { get; set; }
    public string? NombreFamilia { get; set; }
    public decimal? Score { get; set; }
}

public class EstandarizacionLoteResultDto
{
    public int TotalProcesadas { get; set; }
    public int AutoResueltas { get; set; }
    /// <summary>Auto-rechazadas por un alias de rechazo ya confirmado antes (ver RECHAZO_CONFIRMADO) — no pasan por Revisión.</summary>
    public int AutoRechazadas { get; set; }
    public int EnRevision { get; set; }
    public int SinMatch { get; set; }
    /// <summary>Líneas que fallaron por un error transitorio (ej. corte de conexión a la base) durante
    /// el proceso — quedan sin tocar, no se pierden; "Re-estandarizar" las vuelve a intentar.</summary>
    public int ConError { get; set; }
    public List<EstandarizacionLineaDto> Detalles { get; set; } = [];
}

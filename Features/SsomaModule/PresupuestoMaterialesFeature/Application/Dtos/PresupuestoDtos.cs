namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;

// ── Request ───────────────────────────────────────────────────────────────────

public class GenerarPresupuestoDto
{
    /// <summary>Override opcional. Si null, usa los drivers guardados del proyecto.</summary>
    public decimal? HhTotalCasa     { get; set; }
    public decimal? AreaTechadaM2   { get; set; }
    public int?     Trabajadores    { get; set; }
    public string?  Notas           { get; set; }
}

public class ActualizarLineaPresupuestoDto
{
    public decimal? CantidadManual  { get; set; }
    public decimal? PrecioManual    { get; set; }
    public string?  NotasLinea      { get; set; }
}

// ── Response ──────────────────────────────────────────────────────────────────

public class PresupuestoResumenDto
{
    public int      Id                 { get; set; }
    public int      ProjectId          { get; set; }
    public string   ProjectDescription { get; set; } = null!;
    public int      Version            { get; set; }
    public string   Estado             { get; set; } = null!;
    public decimal  HhUsado            { get; set; }
    public decimal  AreaUsada          { get; set; }
    public int      TrabajadoresUsados { get; set; }
    public decimal  TotalEstimado      { get; set; }
    public int      TotalFamilias      { get; set; }
    public int      FamiliasSinHistoria{ get; set; }
    public DateTimeOffset GeneradoEn   { get; set; }
}

public class PresupuestoDetalleDto : PresupuestoResumenDto
{
    public string?  Notas              { get; set; }
    public List<PresupuestoTipoDto> Tipos { get; set; } = [];
}

public class PresupuestoTipoDto
{
    public int      TipoId             { get; set; }
    public string   NombreTipo         { get; set; } = null!;
    public decimal  TotalEstimado      { get; set; }
    public List<PresupuestoLineaDto> Familias { get; set; } = [];
}

public class PresupuestoLineaDto
{
    public int      LineaId            { get; set; }
    public int      FamiliaId          { get; set; }
    public string   NombreFamilia      { get; set; } = null!;
    public int      TipoId             { get; set; }
    public string   NombreTipo         { get; set; } = null!;
    public string   VariableBase       { get; set; } = null!;
    public decimal  RatioRecomendado   { get; set; }
    public int      NProyectosBase     { get; set; }
    public decimal  ValorDriver        { get; set; }
    public decimal  CantidadEstimada   { get; set; }
    public decimal  PrecioUnitario     { get; set; }
    public decimal  TotalEstimado      { get; set; }
    public bool     TieneHistoria      { get; set; }
    // Overrides manuales
    public decimal? CantidadManual     { get; set; }
    public decimal? PrecioManual       { get; set; }
    public string?  NotasLinea         { get; set; }
    // Valores efectivos (manual si existe, calculado si no)
    public decimal  CantidadEfectiva   => CantidadManual ?? CantidadEstimada;
    public decimal  PrecioEfectivo     => PrecioManual   ?? PrecioUnitario;
    public decimal  TotalEfectivo      => Math.Round(CantidadEfectiva * PrecioEfectivo, 2);
}

// ── Ratio recomendado (interno, usado por el servicio) ────────────────────────

public class RatioRecomendadoDto
{
    public int      FamiliaId          { get; set; }
    public string   NombreFamilia      { get; set; } = null!;
    public int      TipoId             { get; set; }
    public string   NombreTipo         { get; set; } = null!;
    public string   VariableBase       { get; set; } = null!;
    public decimal  RatioRecomendado   { get; set; }
    public decimal  PrecioRecomendado  { get; set; }
    public int      NProyectos         { get; set; }
}

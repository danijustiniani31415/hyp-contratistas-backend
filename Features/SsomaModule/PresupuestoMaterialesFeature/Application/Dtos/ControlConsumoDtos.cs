namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;

// ── Requests ──────────────────────────────────────────────────────────────────

public class AbrirSemanaDto
{
    public int    PresupuestoId { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin    { get; set; }
    public string?  Observaciones { get; set; }
}

public class RegistrarConsumoLineaDto
{
    public int     FamiliaId       { get; set; }
    public decimal CantidadReal    { get; set; }
    public decimal? PrecioUnitario { get; set; }
    public string?  Notas          { get; set; }
}

// ── Responses semana ─────────────────────────────────────────────────────────

public class ControlSemanaDto
{
    public int      Id             { get; set; }
    public int      PresupuestoId  { get; set; }
    public int      ProjectId      { get; set; }
    public string   ProjectDescription { get; set; } = null!;
    public int      SemanaNum      { get; set; }
    public DateOnly FechaInicio    { get; set; }
    public DateOnly FechaFin       { get; set; }
    public string   Estado         { get; set; } = null!;
    public string?  Observaciones  { get; set; }
    public DateTimeOffset RegistradoEn { get; set; }
    public List<ControlSemanaLineaDto> Lineas { get; set; } = [];
}

public class ControlSemanaLineaDto
{
    public int      Id             { get; set; }
    public int      FamiliaId      { get; set; }
    public string   NombreFamilia  { get; set; } = null!;
    public string   NombreTipo     { get; set; } = null!;
    public decimal  CantidadReal   { get; set; }
    public decimal? PrecioUnitario { get; set; }
    public decimal  TotalReal      { get; set; }
    public string?  Notas          { get; set; }
}

// ── Dashboard (tiempo real) ───────────────────────────────────────────────────

public class DashboardPresupuestoDto
{
    public int      PresupuestoId      { get; set; }
    public int      ProjectId          { get; set; }
    public string   ProjectDescription { get; set; } = null!;
    public int      Version            { get; set; }
    public decimal  TotalPresupuestado { get; set; }
    public decimal  TotalConsumido     { get; set; }
    public decimal  TotalSaldo         => TotalPresupuestado - TotalConsumido;
    public decimal  PctConsumido       => TotalPresupuestado > 0
                                          ? Math.Round(100 * TotalConsumido / TotalPresupuestado, 1) : 0;
    public int      SemanasRegistradas { get; set; }
    public int      FamiliasEnAlerta   { get; set; }
    public int      FamiliasEnAdvertencia { get; set; }
    public List<DashboardTipoDto> Tipos { get; set; } = [];
}

public class DashboardTipoDto
{
    public int      TipoId             { get; set; }
    public string   NombreTipo         { get; set; } = null!;
    public decimal  TotalPresupuestado { get; set; }
    public decimal  TotalConsumido     { get; set; }
    public decimal  TotalSaldo         => TotalPresupuestado - TotalConsumido;
    public decimal  PctConsumido       => TotalPresupuestado > 0
                                          ? Math.Round(100 * TotalConsumido / TotalPresupuestado, 1) : 0;
    public List<DashboardLineaDto> Familias { get; set; } = [];
}

public class DashboardLineaDto
{
    public int      FamiliaId          { get; set; }
    public string   NombreFamilia      { get; set; } = null!;
    // Usados internamente para agrupar por tipo; no se exponen al cliente
    public int      TipoId             { get; set; }
    public string   VariableBase       { get; set; } = null!;
    public decimal  CantidadPresupuestada { get; set; }
    public decimal  CantidadConsumida  { get; set; }
    public decimal  CantidadSaldo      => CantidadPresupuestada - CantidadConsumida;
    public decimal  PrecioUnitario     { get; set; }
    public decimal  TotalPresupuestado { get; set; }
    public decimal  TotalConsumido     { get; set; }
    public decimal  TotalSaldo         => TotalPresupuestado - TotalConsumido;
    public decimal  PctConsumido       => CantidadPresupuestada > 0
                                          ? Math.Round(100 * CantidadConsumida / CantidadPresupuestada, 1) : 0;
    /// <summary>OK | ADVERTENCIA (≥80%) | ALERTA (≥100%) | SIN_PRESUPUESTO</summary>
    public string   Semaforo           { get; set; } = null!;
}

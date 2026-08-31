namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;

public class DriverProyectoDto
{
    public int    ProjectId          { get; set; }
    public string ProjectDescription { get; set; } = null!;
    public string Estado             { get; set; } = null!;   // Activo / Finalizado / Inactivo
    public decimal? HhTotalCasa      { get; set; }
    public decimal? AreaTechadaM2    { get; set; }
    public int?   Trabajadores       { get; set; }
    public string HhFuente           { get; set; } = null!;   // HH_REAL | HH_PROYECTADO | HH_CALCULADO_MEDIANA
    public int    FamiliasConRatio   { get; set; }
    public bool   TieneConsumos      { get; set; }
    public bool   HabilitadoSsoma    { get; set; }
}

public class ActualizarDriversDto
{
    /// <summary>
    /// Valor real final de HH (para proyectos culminados) o proyectado (para activos).
    /// </summary>
    public decimal HhTotalCasa      { get; set; }
    public decimal AreaTechadaM2    { get; set; }
    public int     Trabajadores     { get; set; }

    /// <summary>
    /// HH_REAL = proyecto culminado con datos reales.
    /// HH_PROYECTADO = proyecto activo con proyección definida.
    /// </summary>
    public string HhFuente          { get; set; } = "HH_REAL";

    /// <summary>Si true, recalcula ss_ratio_proyecto para este proyecto tras guardar.</summary>
    public bool   RecalcularRatios  { get; set; } = true;
}

public class ActualizarDriversResultDto
{
    public int    ProjectId          { get; set; }
    public string ProjectDescription { get; set; } = null!;
    public string HhFuente           { get; set; } = null!;
    public int    RatiosActualizados { get; set; }
    public int    FamiliasSinDriver  { get; set; }
    public List<string> Advertencias { get; set; } = [];
}

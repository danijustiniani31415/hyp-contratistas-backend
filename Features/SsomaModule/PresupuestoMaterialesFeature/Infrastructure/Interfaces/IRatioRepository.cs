using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;

public class RatioRawData
{
    public int FamiliaId { get; set; }
    public string NombreFamilia { get; set; } = null!;
    public string TipoMaterial { get; set; } = null!;
    public string VariableBase { get; set; } = null!;
    public decimal CantidadTotal { get; set; }
    public decimal PrecioUnitarioPromedio { get; set; }
    public decimal PrecioTotal { get; set; }
}

public class RatioUpsertItem
{
    public int FamiliaId { get; set; }
    public int ProjectId { get; set; }
    public string VariableBase { get; set; } = null!;
    public decimal CantidadTotal { get; set; }
    public decimal PrecioUnitarioPromedio { get; set; }
    public decimal ValorDriver { get; set; }
    public decimal RatioCantidad { get; set; }
}

public interface IRatioRepository
{
    Task<List<RatioRawData>> ObtenerConsumosPorProyectoAsync(int projectId);
    /// <summary>Proyectos que tienen al menos una línea de consumo SSOMA ya estandarizada — candidatos para "Calcular ratios de todos".</summary>
    Task<List<(int ProjectId, string ProjectDescription)>> ObtenerProyectosConConsumoEstandarizadoAsync();
    Task UpsertRatiosBulkAsync(List<RatioUpsertItem> items);
    Task<List<RatioProyectoDto>> ObtenerRatiosPorProyectoAsync(int projectId);
    Task<List<RatioProyectoDto>> ObtenerRatiosPorFamiliaAsync(int familiaId);
    Task ActualizarIncluidoManualAsync(int familiaId, int projectId, bool incluir, string campo);
    Task<List<FamiliaConRatioDto>> ListarFamiliasConRatioAsync();
    Task<List<ResumenProyectoRatioDto>> ObtenerResumenAsync();
}

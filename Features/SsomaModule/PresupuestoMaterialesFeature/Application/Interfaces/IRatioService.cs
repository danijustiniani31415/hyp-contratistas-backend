using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;

public interface IRatioService
{
    Task<CalcularRatiosResultDto> CalcularRatiosProyectoAsync(int projectId);
    /// <summary>Calcula ratios de todos los proyectos con consumo SSOMA estandarizado de una sola vez.</summary>
    Task<CalcularRatiosTodosResultDto> CalcularRatiosTodosLosProyectosAsync();
    Task<List<RatioProyectoDto>> ObtenerRatiosProyectoAsync(int projectId);
    Task<RatioFamiliaComparacionDto?> ObtenerComparacionFamiliaAsync(int familiaId);
    Task ActualizarIncluidoManualAsync(int familiaId, int projectId, bool incluir, string campo);
    Task<List<FamiliaConRatioDto>> ListarFamiliasConRatioAsync();
    Task<ResumenRatiosDto> ObtenerResumenAsync();
}

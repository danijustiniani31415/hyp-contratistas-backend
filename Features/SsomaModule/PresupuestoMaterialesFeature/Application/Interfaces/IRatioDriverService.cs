using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;

public interface IRatioDriverService
{
    Task<CalcularRatiosDriversResultDto> CalcularRatiosAsync();
    Task<RatioDriverComparacionDto> ObtenerComparacionAsync(string tipoDriver);
    Task ActualizarIncluidoManualAsync(string tipoDriver, int projectId, bool incluir);
    Task<RatiosDriversRecomendadosDto> ObtenerRecomendadosAsync();
}

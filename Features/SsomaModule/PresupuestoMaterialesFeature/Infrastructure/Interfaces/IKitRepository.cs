using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;

public interface IKitRepository
{
    Task<List<KitResumenDto>> ListarAsync(int? tipoId);
    Task<KitDetalleDto?> ObtenerAsync(int kitId);
    Task<int> CrearAsync(KitCreateDto dto);
    Task<List<KitCalculoLineaDto>> CalcularAsync(int kitId, decimal cantidadKits);
}

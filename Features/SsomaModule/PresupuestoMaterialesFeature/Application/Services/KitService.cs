using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Services;

public class KitService : IKitService
{
    private readonly IKitRepository _repo;
    public KitService(IKitRepository repo) => _repo = repo;

    public Task<List<KitResumenDto>> ListarAsync(int? tipoId) => _repo.ListarAsync(tipoId);

    public Task<KitDetalleDto?> ObtenerAsync(int kitId) => _repo.ObtenerAsync(kitId);

    public Task<int> CrearAsync(KitCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            throw new AbrilException("El kit necesita un nombre.", 400);
        if (dto.Items.Count == 0)
            throw new AbrilException("El kit necesita al menos un ítem en su lista (BOM).", 400);
        return _repo.CrearAsync(dto);
    }

    public Task<List<KitCalculoLineaDto>> CalcularAsync(int kitId, decimal cantidadKits)
    {
        if (cantidadKits <= 0)
            throw new AbrilException("La cantidad de kits debe ser mayor a 0.", 400);
        return _repo.CalcularAsync(kitId, cantidadKits);
    }
}

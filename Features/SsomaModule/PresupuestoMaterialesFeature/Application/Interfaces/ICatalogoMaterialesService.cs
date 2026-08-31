using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;

public interface ICatalogoMaterialesService
{
    Task<SeedCatalogoResultDto> SeedCatalogoAsync(SeedCatalogoRequestDto request);
    Task<List<FamiliaCatalogoDto>> ListarFamiliasAsync(string? q, int? tipoId, bool? perteneceSsoma);
    Task ActualizarFamiliaAsync(int id, ActualizarFamiliaDto dto);
    Task<List<TipoMaterialDto>> ListarTiposAsync();
    Task<BuscarItemDto> CrearItemAsync(CrearItemCatalogoDto dto);
    Task<FamiliaCatalogoDto> CrearFamiliaAsync(CrearFamiliaCatalogoDto dto);
}

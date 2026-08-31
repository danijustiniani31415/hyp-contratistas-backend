using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Models;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;

public interface ICatalogoMaterialesRepository
{
    Task<List<SsMaterialTipo>> GetTiposAsync();
    Task<List<SsMaterialFamilia>> GetFamiliasAsync();
    Task<List<FamiliaCatalogoDto>> ListarFamiliasDetalladoAsync(string? q, int? tipoId, bool? perteneceSsoma);
    Task ActualizarFamiliaAsync(int id, ActualizarFamiliaDto dto);
    Task<List<SsMaterialItem>> GetItemsAsync();
    Task<List<SsMaterialAlias>> GetAliasesAsync();

    Task<SsMaterialTipo> GetOrCreateTipoAsync(string nombre);

    Task<(SsMaterialFamilia Familia, bool Creada)> GetOrCreateFamiliaAsync(
        string nombre, string nombreNormalizado, int tipoId, string variableBase, bool perteneceSsoma);

    Task<(SsMaterialItem Item, bool Creado)> GetOrCreateItemAsync(
        string nombre, string nombreNormalizado, int familiaId, string? talla, string? dimensionNorm, bool noUsar);

    /// <summary>Alta manual desde el buscador de Revisión (a diferencia de GetOrCreateItemAsync, usado en bloque por el seed, esta devuelve família/tipo ya resueltos para la respuesta al front).</summary>
    Task<(SsMaterialItem Item, string NombreFamilia, string NombreTipo, bool PerteneceSsoma)> CrearItemManualAsync(
        string nombre, string nombreNormalizado, int familiaId);

    /// <summary>Alta manual de família desde Revisión, cuando el material no encaja en ninguna família existente.</summary>
    Task<FamiliaCatalogoDto> CrearFamiliaManualAsync(
        string nombre, string nombreNormalizado, int tipoId, string variableBase, string? unidadMedida, bool perteneceSsoma);

    Task<bool> CreateAliasIfNotExistsAsync(
        string textoCrudo, string textoCrudoNorm, int itemId, string origen, decimal? confianza,
        decimal factorConversion = 1);
}

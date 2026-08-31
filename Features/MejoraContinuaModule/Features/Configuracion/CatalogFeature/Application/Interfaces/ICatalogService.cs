using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.CatalogFeature.Application.Dtos;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.CatalogFeature.Application.Interfaces
{
    public interface ICatalogService
    {
        Task<List<CatalogTypeDTO>> GetAllTypesAsync();
        Task<List<CatalogItemDTO>> GetItemsByTypeAsync(int catalogTypeId);
        Task<List<CatalogItemDTO>> GetFullTreeAsync();
        Task CreateTypeAsync(CatalogTypeCreateDTO dto);
        Task UpdateTypeAsync(CatalogTypeEditDTO dto);
        Task DeleteTypeAsync(int catalogTypeId);
        Task CreateItemAsync(CatalogItemCreateDTO dto);
        Task UpdateItemAsync(CatalogItemEditDTO dto);
        Task DeleteItemAsync(int catalogItemId);
    }
}

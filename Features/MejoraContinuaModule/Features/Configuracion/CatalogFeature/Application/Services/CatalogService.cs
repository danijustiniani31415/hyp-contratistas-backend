using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.CatalogFeature.Application.Dtos;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.CatalogFeature.Application.Interfaces;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.CatalogFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.CatalogFeature.Application.Services
{
    public class CatalogService : ICatalogService
    {
        private readonly ICatalogRepository _repo;
        public CatalogService(ICatalogRepository repo) => _repo = repo;

        public Task<List<CatalogTypeDTO>> GetAllTypesAsync() => _repo.GetAllTypesAsync();
        public Task<List<CatalogItemDTO>> GetItemsByTypeAsync(int catalogTypeId) => _repo.GetItemsByTypeAsync(catalogTypeId);
        public Task<List<CatalogItemDTO>> GetFullTreeAsync() => _repo.GetFullTreeAsync();
        public Task CreateTypeAsync(CatalogTypeCreateDTO dto) => _repo.CreateTypeAsync(dto);
        public Task UpdateTypeAsync(CatalogTypeEditDTO dto) => _repo.UpdateTypeAsync(dto);
        public Task DeleteTypeAsync(int catalogTypeId) => _repo.DeleteTypeAsync(catalogTypeId);
        public Task CreateItemAsync(CatalogItemCreateDTO dto) => _repo.CreateItemAsync(dto);
        public Task UpdateItemAsync(CatalogItemEditDTO dto) => _repo.UpdateItemAsync(dto);
        public Task DeleteItemAsync(int catalogItemId) => _repo.DeleteItemAsync(catalogItemId);
    }
}

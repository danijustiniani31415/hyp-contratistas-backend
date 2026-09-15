using Abril_Backend.Features.CatalogoModule.Application.Dtos;

namespace Abril_Backend.Features.CatalogoModule.Application.Interfaces
{
    public interface ICatalogoService
    {
        Task<List<CategoriaDto>> ListCategorias();
        Task<CategoriaDto> CrearCategoria(CategoriaCreateDto dto);

        Task<ProductoListResponseDto> ListProductos(string? search, int page, int pageSize);
        Task<ProductoDetailDto> GetProductoById(long id);
        Task<ProductoDetailDto> CrearProducto(ProductoCreateDto dto);
        Task<ProductoDetailDto> ActualizarProducto(long id, ProductoUpdateDto dto);

        /// <summary>Fuzzy search por similitud de nombre — para avisar de posibles duplicados antes de crear.</summary>
        Task<List<SugerenciaProductoDto>> SugerirProductos(string nombre);
    }
}

using Abril_Backend.Features.CatalogoModule.Application.Dtos;

namespace Abril_Backend.Features.CatalogoModule.Application.Interfaces
{
    public interface ICatalogoService
    {
        Task<List<CategoriaDto>> ListCategorias();
        Task<CategoriaDto> CrearCategoria(CategoriaCreateDto dto);

        /// <summary>Catálogo fijo de tallas (ROPA, CALZADO, GUANTES) para el combo de talla en Pedidos.</summary>
        Task<List<TallaDto>> ListTallas(string tipoTalla);

        Task<ProductoListResponseDto> ListProductos(string? search, int page, int pageSize);
        Task<ProductoDetailDto> GetProductoById(long id);
        Task<ProductoDetailDto> CrearProducto(ProductoCreateDto dto);
        Task<ProductoDetailDto> ActualizarProducto(long id, ProductoUpdateDto dto);

        /// <summary>Fuzzy search por similitud de nombre — para avisar de posibles duplicados antes de crear.</summary>
        Task<List<SugerenciaProductoDto>> SugerirProductos(string nombre);
    }
}

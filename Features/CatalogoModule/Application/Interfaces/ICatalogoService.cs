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

        /// <summary>Todos los alias de todos los productos (para reconocer textos de un Excel).</summary>
        Task<List<ProductoAliasDto>> ListAlias();

        /// <summary>Ids de los productos que tienen alguna referencia (stock, movimientos, pedidos, compras, guías, EPP, préstamos).</summary>
        Task<List<long>> ListProductosConUso();

        /// <summary>Agrega un alias a un producto si no coincide con su nombre ni lo tiene otro producto. No falla si ya existe.</summary>
        Task AgregarAlias(long productoId, string alias);

        /// <summary>Desactiva productos repetidos que NO tienen ninguna referencia y pasa su nombre a alias del principal.</summary>
        Task<ResolverDuplicadosResultDto> ResolverDuplicados(ResolverDuplicadosDto dto);
    }
}

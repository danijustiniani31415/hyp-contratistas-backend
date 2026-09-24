namespace Abril_Backend.Features.CatalogoModule.Application.Dtos
{
    public class CategoriaDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public string Tipo { get; set; } = null!;
    }

    public class CategoriaCreateDto
    {
        public string Nombre { get; set; } = null!;
        public string Tipo { get; set; } = null!;
    }

    public class ProductoListItemDto
    {
        public long Id { get; set; }
        public string? Codigo { get; set; }
        public string Nombre { get; set; } = null!;
        public string CategoriaNombre { get; set; } = null!;
        public string CategoriaTipo { get; set; } = null!;
        public string UnidadMedida { get; set; } = null!;
        public bool RequiereTalla { get; set; }
        public string? TipoTalla { get; set; }
        public bool RequiereColor { get; set; }
        public bool EsRetornable { get; set; }
        public bool Activo { get; set; }
    }

    public class TallaDto
    {
        public string Valor { get; set; } = null!;
    }

    public class ProductoListResponseDto
    {
        public List<ProductoListItemDto> Data { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
    }

    public class ProductoDetailDto
    {
        public long Id { get; set; }
        public string? Codigo { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
        public int CategoriaId { get; set; }
        public string CategoriaNombre { get; set; } = null!;
        public string UnidadMedida { get; set; } = null!;
        public bool RequiereTalla { get; set; }
        public string? TipoTalla { get; set; }
        public bool RequiereColor { get; set; }
        public bool EsRetornable { get; set; }
        public bool Activo { get; set; }
    }

    public class ProductoCreateDto
    {
        public string? Codigo { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
        public int CategoriaId { get; set; }
        public string UnidadMedida { get; set; } = null!;
        public bool RequiereTalla { get; set; }
        public string? TipoTalla { get; set; }
        public bool RequiereColor { get; set; }
        public bool EsRetornable { get; set; }
    }

    public class ProductoUpdateDto : ProductoCreateDto
    {
        public bool Activo { get; set; } = true;
    }

    /// <summary>Resultado de similarity() contra lb_producto.nombre — CONTEXT_LOGISTICA.md sección 6.</summary>
    public class SugerenciaProductoDto
    {
        public long Id { get; set; }
        public string Nombre { get; set; } = null!;
        public double Score { get; set; }
    }
}

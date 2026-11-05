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
        public string? Marca { get; set; }
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
        public string? Marca { get; set; }
        public string? Descripcion { get; set; }
        public int CategoriaId { get; set; }
        public string CategoriaNombre { get; set; } = null!;
        public string UnidadMedida { get; set; } = null!;
        public bool RequiereTalla { get; set; }
        public string? TipoTalla { get; set; }
        public bool RequiereColor { get; set; }
        public bool EsRetornable { get; set; }
        public bool Activo { get; set; }
        /// <summary>Otras formas de escribir el nombre (el sistema las aprende; también se pueden editar).</summary>
        public List<string> Alias { get; set; } = new();
    }

    public class ProductoCreateDto
    {
        public string? Codigo { get; set; }
        public string Nombre { get; set; } = null!;
        /// <summary>Opcional. Si viene y el nombre no la contiene, se agrega al final del nombre.</summary>
        public string? Marca { get; set; }
        public string? Descripcion { get; set; }
        public int CategoriaId { get; set; }
        public string UnidadMedida { get; set; } = null!;
        public bool RequiereTalla { get; set; }
        public string? TipoTalla { get; set; }
        public bool RequiereColor { get; set; }
        public bool EsRetornable { get; set; }
        /// <summary>En una edición reemplaza la lista de alias; null = dejarla como está.</summary>
        public List<string>? Alias { get; set; }
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

    public class ProductoAliasDto
    {
        public long ProductoId { get; set; }
        public string Alias { get; set; } = null!;
    }

    public class ResolverDuplicadosDto
    {
        /// <summary>Producto que se conserva.</summary>
        public long PrincipalId { get; set; }
        /// <summary>Productos repetidos que se desactivan; su nombre pasa a ser alias del principal.</summary>
        public List<long> DuplicadoIds { get; set; } = new();
    }

    public class ResolverDuplicadosResultDto
    {
        public int Desactivados { get; set; }
        public List<string> Omitidos { get; set; } = new();
    }
}

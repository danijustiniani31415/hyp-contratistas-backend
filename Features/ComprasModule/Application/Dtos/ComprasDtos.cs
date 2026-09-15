namespace Abril_Backend.Features.ComprasModule.Application.Dtos
{
    public class ProveedorDto
    {
        public int Id { get; set; }
        public string RazonSocial { get; set; } = null!;
        public string? Ruc { get; set; }
        public string? Contacto { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
    }

    public class ProveedorCreateDto
    {
        public string RazonSocial { get; set; } = null!;
        public string? Ruc { get; set; }
        public string? Contacto { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
    }

    public class OrdenCompraItemCreateDto
    {
        public long ProductoId { get; set; }
        public string Talla { get; set; } = "";
        public decimal CantidadSolicitada { get; set; }
        public decimal CostoUnitario { get; set; }
    }

    public class OrdenCompraCreateDto
    {
        public int ProveedorId { get; set; }
        public int AlmacenId { get; set; }
        public string? Observacion { get; set; }
        public List<OrdenCompraItemCreateDto> Items { get; set; } = new();
    }

    public class RecibirItemDto
    {
        public decimal Cantidad { get; set; }
    }

    public class OrdenCompraItemDetailDto
    {
        public long Id { get; set; }
        public string ProductoNombre { get; set; } = null!;
        public string? ProductoCodigo { get; set; }
        public string Talla { get; set; } = "";
        public decimal CantidadSolicitada { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal CantidadRecibida { get; set; }
        public decimal CantidadPendiente { get; set; }
    }

    public class OrdenCompraDetailDto
    {
        public long Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string ProveedorNombre { get; set; } = null!;
        public string AlmacenNombre { get; set; } = null!;
        public string SolicitadoPorNombre { get; set; } = null!;
        public string Estado { get; set; } = null!;
        public string? Observacion { get; set; }
        public DateTimeOffset CreadoEn { get; set; }
        public List<OrdenCompraItemDetailDto> Items { get; set; } = new();
    }

    public class OrdenCompraListItemDto
    {
        public long Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string ProveedorNombre { get; set; } = null!;
        public string AlmacenNombre { get; set; } = null!;
        public string Estado { get; set; } = null!;
        public int CantidadItems { get; set; }
        public DateTimeOffset CreadoEn { get; set; }
    }

    public class OrdenCompraListResponseDto
    {
        public List<OrdenCompraListItemDto> Data { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
    }
}

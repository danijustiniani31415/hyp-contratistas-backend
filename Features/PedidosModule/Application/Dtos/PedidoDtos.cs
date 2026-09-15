namespace Abril_Backend.Features.PedidosModule.Application.Dtos
{
    public class PedidoItemCreateDto
    {
        public long ProductoId { get; set; }
        public string Talla { get; set; } = "";
        public decimal CantidadSolicitada { get; set; }
    }

    public class PedidoCreateDto
    {
        public int ProyectoId { get; set; }
        public int AlmacenId { get; set; }
        public string? Observacion { get; set; }
        public List<PedidoItemCreateDto> Items { get; set; } = new();
    }

    public class PedidoListItemDto
    {
        public long Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string ProyectoNombre { get; set; } = null!;
        public string AlmacenNombre { get; set; } = null!;
        public string SolicitanteNombre { get; set; } = null!;
        public string Estado { get; set; } = null!;
        public int CantidadItems { get; set; }
        public DateTimeOffset CreadoEn { get; set; }
    }

    public class PedidoListResponseDto
    {
        public List<PedidoListItemDto> Data { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
    }

    public class PedidoItemDetailDto
    {
        public long Id { get; set; }
        public string ProductoNombre { get; set; } = null!;
        public string? ProductoCodigo { get; set; }
        public string UnidadMedida { get; set; } = null!;
        public string Talla { get; set; } = "";
        public decimal CantidadSolicitada { get; set; }
        public decimal? CantidadEntregada { get; set; }
    }

    public class PedidoDetailDto
    {
        public long Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string ProyectoNombre { get; set; } = null!;
        public string AlmacenNombre { get; set; } = null!;
        public long SolicitanteUsuarioSistemaId { get; set; }
        public string SolicitanteNombre { get; set; } = null!;
        public string Estado { get; set; } = null!;
        public string? Observacion { get; set; }
        public string? MotivoRechazo { get; set; }
        public string? AprobadoPorNombre { get; set; }
        public DateTimeOffset? AprobadoEn { get; set; }
        public string? EntregadoPorNombre { get; set; }
        public DateTimeOffset? EntregadoEn { get; set; }
        public DateTimeOffset CreadoEn { get; set; }
        public List<PedidoItemDetailDto> Items { get; set; } = new();
    }

    public class RechazarPedidoDto
    {
        public string MotivoRechazo { get; set; } = null!;
    }
}

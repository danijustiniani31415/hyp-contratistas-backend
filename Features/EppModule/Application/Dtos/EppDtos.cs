namespace Abril_Backend.Features.EppModule.Application.Dtos
{
    public class EntregaEppItemCreateDto
    {
        public long ProductoId { get; set; }
        public string Talla { get; set; } = "";
        public string Color { get; set; } = "";
        public decimal Cantidad { get; set; }
    }

    public class EntregaEppCreateDto
    {
        public int PersonaId { get; set; }
        public int AlmacenId { get; set; }
        public string? Observacion { get; set; }
        public List<EntregaEppItemCreateDto> Items { get; set; } = new();
    }

    public class EntregaEppItemDetailDto
    {
        public long Id { get; set; }
        public string ProductoNombre { get; set; } = null!;
        public string? ProductoCodigo { get; set; }
        public string Talla { get; set; } = "";
        public string Color { get; set; } = "";
        public decimal Cantidad { get; set; }
    }

    public class EntregaEppDetailDto
    {
        public long Id { get; set; }
        public int PersonaId { get; set; }
        public string PersonaNombre { get; set; } = null!;
        public string AlmacenNombre { get; set; } = null!;
        public string EntregadoPorNombre { get; set; } = null!;
        public string? Observacion { get; set; }
        public DateTimeOffset CreadoEn { get; set; }
        public List<EntregaEppItemDetailDto> Items { get; set; } = new();
    }

    public class EntregaEppListItemDto
    {
        public long Id { get; set; }
        public string PersonaNombre { get; set; } = null!;
        public string AlmacenNombre { get; set; } = null!;
        public string EntregadoPorNombre { get; set; } = null!;
        public int CantidadItems { get; set; }
        public DateTimeOffset CreadoEn { get; set; }
    }

    public class EntregaEppListResponseDto
    {
        public List<EntregaEppListItemDto> Data { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
    }
}

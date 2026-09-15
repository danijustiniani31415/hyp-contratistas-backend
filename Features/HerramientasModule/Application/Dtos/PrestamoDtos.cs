namespace Abril_Backend.Features.HerramientasModule.Application.Dtos
{
    public class PrestamoItemCreateDto
    {
        public long ProductoId { get; set; }
        public string Talla { get; set; } = "";
        public decimal Cantidad { get; set; }
    }

    public class PrestamoCreateDto
    {
        public int AlmacenId { get; set; }
        public int PersonaId { get; set; }
        public int? ProyectoId { get; set; }
        public DateOnly? FechaDevolucionEstimada { get; set; }
        public string? Observacion { get; set; }
        public List<PrestamoItemCreateDto> Items { get; set; } = new();
    }

    public class DevolverItemDto
    {
        /// <summary>DEVUELTO, PERDIDO o DANADO.</summary>
        public string Estado { get; set; } = "DEVUELTO";
        public string? Observacion { get; set; }
    }

    public class PrestamoItemDetailDto
    {
        public long Id { get; set; }
        public string ProductoNombre { get; set; } = null!;
        public string? ProductoCodigo { get; set; }
        public string Talla { get; set; } = "";
        public decimal Cantidad { get; set; }
        public string Estado { get; set; } = null!;
        public string? DevueltoPorNombre { get; set; }
        public DateTimeOffset? FechaDevolucion { get; set; }
        public string? ObservacionDevolucion { get; set; }
    }

    public class PrestamoDetailDto
    {
        public long Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string AlmacenNombre { get; set; } = null!;
        public int PersonaId { get; set; }
        public string PersonaNombre { get; set; } = null!;
        public string? ProyectoNombre { get; set; }
        public string PrestadoPorNombre { get; set; } = null!;
        public DateOnly? FechaDevolucionEstimada { get; set; }
        public string? Observacion { get; set; }
        public DateTimeOffset CreadoEn { get; set; }
        /// <summary>ABIERTO si algún ítem sigue PRESTADO, CERRADO si todos ya se devolvieron/perdieron/dañaron. Calculado, no se guarda.</summary>
        public string Estado { get; set; } = null!;
        public List<PrestamoItemDetailDto> Items { get; set; } = new();
    }

    public class PrestamoListItemDto
    {
        public long Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string PersonaNombre { get; set; } = null!;
        public string AlmacenNombre { get; set; } = null!;
        public string Estado { get; set; } = null!;
        public int CantidadItems { get; set; }
        public int CantidadPendientes { get; set; }
        public DateTimeOffset CreadoEn { get; set; }
    }

    public class PrestamoListResponseDto
    {
        public List<PrestamoListItemDto> Data { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
    }
}

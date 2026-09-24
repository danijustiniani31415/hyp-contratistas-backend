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
        public string Color { get; set; } = "";
        public decimal CantidadSolicitada { get; set; }
        public decimal CostoUnitario { get; set; }
        /// <summary>Null = compra libre. Con valor: esta fila cubre ese ítem de pedido puntual.</summary>
        public long? PedidoItemId { get; set; }
    }

    public class OrdenCompraCreateDto
    {
        public int ProveedorId { get; set; }
        public int AlmacenId { get; set; }
        public string? Observacion { get; set; }
        public List<OrdenCompraItemCreateDto> Items { get; set; } = new();
    }

    /// <summary>Genera una OC a partir de ítems pendientes de uno o varios pedidos, y/o de
    /// reposición sugerida por stock bajo mínimo. Con PedidoItemId: la cantidad es siempre la
    /// pendiente de compra de ese ítem al momento de generar (no se pide, para no permitir comprar
    /// de más o de menos por error). Sin PedidoItemId: es una reposición libre — ProductoId y
    /// Cantidad sí los define quien genera la orden.</summary>
    public class GenerarDesdePedidosItemDto
    {
        public long? PedidoItemId { get; set; }
        public long? ProductoId { get; set; }
        public string Talla { get; set; } = "";
        public string Color { get; set; } = "";
        public decimal? Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
    }

    public class GenerarOrdenCompraDesdePedidosDto
    {
        public int ProveedorId { get; set; }
        public int AlmacenId { get; set; }
        public string? Observacion { get; set; }
        public List<GenerarDesdePedidosItemDto> Items { get; set; } = new();
    }

    public class RecibirItemDto
    {
        public decimal Cantidad { get; set; }
        public string? FacturaNumero { get; set; }
        public decimal? FacturaMonto { get; set; }
    }

    public class OrdenCompraRecepcionDetailDto
    {
        public long Id { get; set; }
        public decimal Cantidad { get; set; }
        public string? FacturaNumero { get; set; }
        public decimal? FacturaMonto { get; set; }
        public string RecibidoPorNombre { get; set; } = null!;
        public DateTimeOffset CreadoEn { get; set; }
    }

    public class OrdenCompraItemDetailDto
    {
        public long Id { get; set; }
        public string ProductoNombre { get; set; } = null!;
        public string? ProductoCodigo { get; set; }
        public string Talla { get; set; } = "";
        public string Color { get; set; } = "";
        public decimal CantidadSolicitada { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal CantidadRecibida { get; set; }
        public decimal CantidadPendiente { get; set; }
        public long? PedidoItemId { get; set; }
        public string? PedidoCodigo { get; set; }
        public List<OrdenCompraRecepcionDetailDto> Recepciones { get; set; } = new();
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

    /// <summary>Devuelve mercadería ya recibida (defectuosa/incorrecta) al proveedor — genera una
    /// Guía de Remisión real (motivo SUNAT "02 Compra: devolución") porque el material sale
    /// físicamente del almacén, no es un simple ajuste interno [DECIDIDO 2026-09-24].</summary>
    public class DevolverItemDto
    {
        public decimal Cantidad { get; set; }
        public string ModalidadTraslado { get; set; } = "02";
        public DateOnly FechaTraslado { get; set; }
        public decimal PesoBrutoTotal { get; set; }
        public string PesoBrutoUnidad { get; set; } = "KGM";
        public int? NumBultos { get; set; }
        public string? TransportistaRuc { get; set; }
        public string? TransportistaRazonSocial { get; set; }
        public string? VehiculoPlaca { get; set; }
        public string? ConductorNombres { get; set; }
        public string? ConductorLicencia { get; set; }
        public string? Observacion { get; set; }
    }

    public class DevolverItemResultDto
    {
        public OrdenCompraDetailDto Orden { get; set; } = null!;
        public string GuiaCodigo { get; set; } = null!;
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

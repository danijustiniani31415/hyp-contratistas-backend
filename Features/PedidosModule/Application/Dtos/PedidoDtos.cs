namespace Abril_Backend.Features.PedidosModule.Application.Dtos
{
    public class PedidoItemCreateDto
    {
        public long ProductoId { get; set; }
        public string Talla { get; set; } = "";
        public string Color { get; set; } = "";
        public string? Observacion { get; set; }
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
        public string Color { get; set; } = "";
        public string? Observacion { get; set; }
        public decimal CantidadSolicitada { get; set; }
        public decimal? CantidadEntregada { get; set; }
        public decimal CantidadEnCompra { get; set; }
        public decimal CantidadRecibidaAlmacen { get; set; }
        public decimal CantidadDespachada { get; set; }
        public decimal CantidadConfirmadaMina { get; set; }
    }

    /// <summary>Ítem de un pedido APROBADO que todavía no está cubierto por una compra en curso —
    /// alimenta el panel de Logística de "qué falta comprar" cruzando todos los pedidos activos.</summary>
    public class PendienteCompraDto
    {
        public long PedidoItemId { get; set; }
        public long PedidoId { get; set; }
        public string PedidoCodigo { get; set; } = null!;
        public string ProyectoNombre { get; set; } = null!;
        public long ProductoId { get; set; }
        public string ProductoNombre { get; set; } = null!;
        public string Talla { get; set; } = "";
        public string Color { get; set; } = "";
        public decimal CantidadSolicitada { get; set; }
        public decimal CantidadEnCompra { get; set; }
        public decimal CantidadPendienteDeCompra { get; set; }
        public DateTimeOffset PedidoCreadoEn { get; set; }
    }

    /// <summary>Ítem de un pedido APROBADO ya recibido en el almacén de Lima pero todavía no
    /// despachado con guía — alimenta el selector "Despachar pedido" al crear una Guía de Remisión.</summary>
    public class PendienteDespachoDto
    {
        public long PedidoItemId { get; set; }
        public long PedidoId { get; set; }
        public string PedidoCodigo { get; set; } = null!;
        public string ProyectoNombre { get; set; } = null!;
        public int AlmacenId { get; set; }
        public long ProductoId { get; set; }
        public string ProductoNombre { get; set; } = null!;
        public string Talla { get; set; } = "";
        public string Color { get; set; } = "";
        public string UnidadMedida { get; set; } = null!;
        public decimal CantidadPendienteDeDespacho { get; set; }
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
        public string? VisadoPorNombre { get; set; }
        public DateTimeOffset? VisadoEn { get; set; }
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

    /// <summary>Preview de destinatarios por etapa del flujo, para un proyecto dado — no envía nada,
    /// solo muestra a qué correos llegaría cada notificación (verificación antes de confiar en el flujo).</summary>
    public class PedidoDestinatariosDto
    {
        public List<string> Visadores { get; set; } = new();
        public List<string> Aprobadores { get; set; } = new();
        public List<string> Entregadores { get; set; } = new();
    }
}

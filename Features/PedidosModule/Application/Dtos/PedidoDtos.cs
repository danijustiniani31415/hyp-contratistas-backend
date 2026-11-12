namespace Abril_Backend.Features.PedidosModule.Application.Dtos
{
    public class PedidoItemCreateDto
    {
        public long ProductoId { get; set; }
        public string Talla { get; set; } = "";
        public string Color { get; set; } = "";
        public string? Observacion { get; set; }
        public decimal CantidadSolicitada { get; set; }
        public int? PartidaId { get; set; }
    }

    public class PedidoCreateDto
    {
        public int ProyectoId { get; set; }
        public int AlmacenId { get; set; }
        public int? ContratoId { get; set; }
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
        public decimal? CantidadAprobada { get; set; }
        public string Estado { get; set; } = null!;
        public string? MotivoRechazo { get; set; }
        public int? PartidaId { get; set; }
        public string? PartidaNombre { get; set; }
        public decimal? CantidadEntregada { get; set; }
        public decimal CantidadEnCompra { get; set; }
        public decimal CantidadRecibidaAlmacen { get; set; }
        public decimal CantidadDespachada { get; set; }
        public decimal CantidadConfirmadaMina { get; set; }
        /// <summary>Lo que hay libre en el almacén del pedido: stock actual menos lo ya comprometido por otros pedidos aprobados. Solo se llena mientras el pedido sigue abierto.</summary>
        public decimal? StockLibre { get; set; }
    }

    /// <summary>Decisión del Residente (Visar) o Gerencia (Aprobar) sobre un ítem puntual del
    /// pedido: lo aprueba (completo o con cantidad reducida) o lo rechaza — el resto de ítems del
    /// pedido sigue su flujo normal.</summary>
    public class PedidoItemDecisionDto
    {
        public long PedidoItemId { get; set; }
        public bool Rechazado { get; set; }
        /// <summary>Obligatorio si Rechazado = false. Debe ser mayor a 0 y no mayor a
        /// CantidadSolicitada — igual a CantidadSolicitada si se aprueba completo, menor si se
        /// reduce.</summary>
        public decimal? CantidadAprobada { get; set; }
        /// <summary>Obligatorio si Rechazado = true.</summary>
        public string? MotivoRechazo { get; set; }
    }

    /// <summary>Body de Visar/Aprobar: una decisión por cada ítem que siga pendiente de esa etapa
    /// (PENDIENTE para Visar, VISADO para Aprobar) — los ítems que un rechazo ya dejó fuera del
    /// flujo (RECHAZADO) no se vuelven a incluir.</summary>
    public class PedidoDecisionDto
    {
        public List<PedidoItemDecisionDto> Items { get; set; } = new();
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
        public int? ContratoId { get; set; }
        public string? ContratoCodigo { get; set; }
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

    public class ContratoDto
    {
        public int Id { get; set; }
        public int ProyectoId { get; set; }
        public string Codigo { get; set; } = null!;
        public string? Descripcion { get; set; }
        public DateOnly VigenteDesde { get; set; }
        public DateOnly? VigenteHasta { get; set; }
        public bool Activo { get; set; }
    }

    public class ContratoCreateDto
    {
        public int ProyectoId { get; set; }
        public string Codigo { get; set; } = null!;
        public string? Descripcion { get; set; }
        public DateOnly VigenteDesde { get; set; }
        public DateOnly? VigenteHasta { get; set; }
    }

    public class PartidaDto
    {
        public int Id { get; set; }
        public int ProyectoId { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public bool Activo { get; set; }
        public short? MonedaId { get; set; }
        public string? MonedaCodigo { get; set; }
        public decimal? MontoPresupuestado { get; set; }
    }

    public class PartidaCreateDto
    {
        public int ProyectoId { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public short? MonedaId { get; set; }
        public decimal? MontoPresupuestado { get; set; }
    }

    /// <summary>Una fila de la bitácora de decisiones de un ítem — para responder "quién redujo
    /// o rechazó esta línea, y cuándo" sin depender solo del estado final.</summary>
    public class PedidoItemHistorialDto
    {
        public long Id { get; set; }
        public long PedidoItemId { get; set; }
        public string ProductoNombre { get; set; } = null!;
        public string Etapa { get; set; } = null!;
        public string UsuarioNombre { get; set; } = null!;
        public string EstadoResultante { get; set; } = null!;
        public decimal CantidadSolicitada { get; set; }
        public decimal? CantidadAprobada { get; set; }
        public string? MotivoRechazo { get; set; }
        public DateTimeOffset CreadoEn { get; set; }
    }

    public class MonedaDto
    {
        public short Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
    }

    /// <summary>Presupuestado vs consumido de una partida — el consumido sale del Kardex
    /// (lb_movimiento SALIDA con esta partida), no de los pedidos directamente: así incluye
    /// cualquier salida futura que se impute a la partida sin pasar por un Pedido.</summary>
    public class ConsumoPartidaDto
    {
        public int PartidaId { get; set; }
        public string PartidaCodigo { get; set; } = null!;
        public string PartidaNombre { get; set; } = null!;
        public string? MonedaCodigo { get; set; }
        public decimal? MontoPresupuestado { get; set; }
        public decimal Consumido { get; set; }
        public decimal? Saldo { get; set; }
        public decimal? PorcentajeConsumido { get; set; }
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

namespace Abril_Backend.Features.PedidosModule.Application.Dtos
{
    public class EntregarItemDto
    {
        public long ItemId { get; set; }
        public decimal Cantidad { get; set; }
    }

    /// <summary>Entrega (total o parcial) de un pedido aprobado. Sin cuerpo = entrega todo lo pendiente, como siempre.</summary>
    public class EntregarPedidoDto
    {
        /// <summary>Cantidad a entregar ahora por ítem. Lo no listado (o con 0) queda pendiente.</summary>
        public List<EntregarItemDto>? Items { get; set; }
        /// <summary>true = lo que falta ya no se entregará: el pedido se cierra aunque tenga saldo.</summary>
        public bool CerrarConSaldo { get; set; }
    }
}

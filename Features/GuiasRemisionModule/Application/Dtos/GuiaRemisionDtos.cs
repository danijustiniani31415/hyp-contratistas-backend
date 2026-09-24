namespace Abril_Backend.Features.GuiasRemisionModule.Application.Dtos
{
    public class GuiaRemisionItemCreateDto
    {
        public long ProductoId { get; set; }
        public string Talla { get; set; } = "";
        public string Color { get; set; } = "";
        public decimal Cantidad { get; set; }
        public string UnidadMedida { get; set; } = "NIU";
        /// <summary>Null = despacho suelto. Con valor: este ítem despacha ese ítem de pedido puntual.</summary>
        public long? PedidoItemId { get; set; }
    }

    public class ConfirmarRecepcionItemDto
    {
        public long ItemId { get; set; }
        public decimal CantidadConfirmada { get; set; }
    }

    public class ConfirmarRecepcionDto
    {
        public List<ConfirmarRecepcionItemDto> Items { get; set; } = new();
    }

    public class GuiaRemisionCreateDto
    {
        public string MotivoTraslado { get; set; } = "04";
        public string ModalidadTraslado { get; set; } = "02";
        public DateOnly FechaTraslado { get; set; }
        public decimal PesoBrutoTotal { get; set; }
        public string PesoBrutoUnidad { get; set; } = "KGM";
        public int? NumBultos { get; set; }

        public int AlmacenOrigenId { get; set; }
        public int? AlmacenDestinoId { get; set; }
        public string? DestinatarioRuc { get; set; }
        public string? DestinatarioRazonSocial { get; set; }

        public string? TransportistaRuc { get; set; }
        public string? TransportistaRazonSocial { get; set; }
        public string? VehiculoPlaca { get; set; }
        public string? ConductorNombres { get; set; }
        public string? ConductorLicencia { get; set; }

        public string? Observacion { get; set; }
        public string? ReferenciaTipo { get; set; }
        public long? ReferenciaId { get; set; }

        public List<GuiaRemisionItemCreateDto> Items { get; set; } = new();
    }

    public class GuiaRemisionItemDetailDto
    {
        public long Id { get; set; }
        public string ProductoNombre { get; set; } = null!;
        public string? ProductoCodigo { get; set; }
        public string Talla { get; set; } = "";
        public string Color { get; set; } = "";
        public decimal Cantidad { get; set; }
        public string UnidadMedida { get; set; } = "NIU";
        public long? PedidoItemId { get; set; }
        public string? PedidoCodigo { get; set; }
        public decimal? CantidadConfirmada { get; set; }
        public DateTimeOffset? ConfirmadoEn { get; set; }
        public string? ConfirmadoPorNombre { get; set; }
    }

    public class GuiaRemisionDetailDto
    {
        public long Id { get; set; }
        public string Serie { get; set; } = null!;
        public long Numero { get; set; }
        public string Codigo => $"{Serie}-{Numero:D8}";
        public string Estado { get; set; } = null!;

        public string MotivoTraslado { get; set; } = null!;
        public string ModalidadTraslado { get; set; } = null!;
        public DateOnly FechaTraslado { get; set; }
        public decimal PesoBrutoTotal { get; set; }
        public string PesoBrutoUnidad { get; set; } = null!;
        public int? NumBultos { get; set; }

        public string AlmacenOrigenNombre { get; set; } = null!;
        public string? AlmacenDestinoNombre { get; set; }
        public string? DestinatarioRuc { get; set; }
        public string? DestinatarioRazonSocial { get; set; }

        public string? TransportistaRuc { get; set; }
        public string? TransportistaRazonSocial { get; set; }
        public string? VehiculoPlaca { get; set; }
        public string? ConductorNombres { get; set; }
        public string? ConductorLicencia { get; set; }

        public string? Observacion { get; set; }
        public string? ReferenciaTipo { get; set; }
        public long? ReferenciaId { get; set; }
        public string CreadoPorNombre { get; set; } = null!;
        public DateTimeOffset CreadoEn { get; set; }

        public string? Ticket { get; set; }
        public string? CdrCodigoRespuesta { get; set; }
        public string? CdrDescripcion { get; set; }
        public DateTimeOffset? EnviadoEn { get; set; }
        public DateTimeOffset? RespondidoEn { get; set; }

        /// <summary>True si tiene almacén de destino, ya se transmitió a SUNAT y nadie confirmó
        /// recepción todavía — gatilla el botón "Confirmar recepción" en el frontend.</summary>
        public bool ConfirmacionPendiente { get; set; }

        public List<GuiaRemisionItemDetailDto> Items { get; set; } = new();
    }

    public class GuiaRemisionListItemDto
    {
        public long Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string Estado { get; set; } = null!;
        public string AlmacenOrigenNombre { get; set; } = null!;
        public string? AlmacenDestinoNombre { get; set; }
        public DateOnly FechaTraslado { get; set; }
        public int CantidadItems { get; set; }
        public DateTimeOffset CreadoEn { get; set; }
    }

    public class GuiaRemisionListResponseDto
    {
        public List<GuiaRemisionListItemDto> Data { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
    }
}

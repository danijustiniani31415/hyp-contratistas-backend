namespace Abril_Backend.Features.FacturacionElectronicaModule.Application.Dtos
{
    public class CpeItemDto
    {
        public string? Codigo { get; set; }
        public string Descripcion { get; set; } = "";
        public string UnidadMedida { get; set; } = "NIU";
        public decimal Cantidad { get; set; }
        /// <summary>Precio unitario tal como lo digita el usuario: sin IGV, o con IGV si el documento marca PreciosIncluyenIgv.</summary>
        public decimal PrecioUnitario { get; set; }
        /// <summary>Descuento de la línea, en monto (no porcentaje), sobre el valor de venta.</summary>
        public decimal Descuento { get; set; }
        /// <summary>10 gravado, 20 exonerado, 30 inafecto, 40 exportación.</summary>
        public string AfectacionIgv { get; set; } = "10";
        public int? ProductoId { get; set; }
        public int? PartidaId { get; set; }
    }

    public class CpeCuotaDto
    {
        public decimal Monto { get; set; }
        public DateOnly Fecha { get; set; }
    }

    /// <summary>Crea (o reemplaza, si es un borrador existente) un borrador de factura o nota.</summary>
    public class CpeCreateDto
    {
        /// <summary>01 factura, 07 nota de crédito, 08 nota de débito.</summary>
        public string TipoDocumento { get; set; } = "01";
        public string? Serie { get; set; }
        public DateOnly? FechaEmision { get; set; }
        public DateOnly? FechaVencimiento { get; set; }
        public string Moneda { get; set; } = "PEN";
        public decimal? TipoCambio { get; set; }
        /// <summary>0101 venta interna o 0200 exportación (1001, sujeta a detracción, la pone el sistema si hay detracción).</summary>
        public string TipoOperacion { get; set; } = "0101";

        public string ClienteTipoDoc { get; set; } = "6";
        public string ClienteNumeroDoc { get; set; } = "";
        public string ClienteRazon { get; set; } = "";
        public string? ClienteDireccion { get; set; }
        /// <summary>País del cliente (2 letras ISO): solo exportación.</summary>
        public string? ClientePais { get; set; }

        public bool PreciosIncluyenIgv { get; set; }
        public string FormaPago { get; set; } = "CONTADO";
        public List<CpeCuotaDto>? Cuotas { get; set; }

        public string? DetraccionCodigo { get; set; }
        public decimal? DetraccionPorcentaje { get; set; }
        public string? DetraccionCuenta { get; set; }

        /// <summary>Solo notas: comprobante que modifican (debe estar aceptado) y motivo.</summary>
        public long? RefCpeId { get; set; }
        public string? MotivoCodigo { get; set; }
        public string? MotivoDescripcion { get; set; }

        public string? Observacion { get; set; }
        public string? OrdenCompraRef { get; set; }
        public string? GuiaRef { get; set; }
        public int? ContratoId { get; set; }
        public int? ProyectoId { get; set; }

        public List<CpeItemDto> Items { get; set; } = new();
    }

    public class CpeAvisoDto
    {
        /// <summary>ERROR (bloquea) o AVISO.</summary>
        public string Nivel { get; set; } = "AVISO";
        public string Mensaje { get; set; } = "";
    }

    public class CpeItemDetalleDto : CpeItemDto
    {
        public decimal ValorUnitario { get; set; }
        public decimal ValorVenta { get; set; }
        public decimal Igv { get; set; }
    }

    public class CpeDetailDto
    {
        public long Id { get; set; }
        public string TipoDocumento { get; set; } = "";
        public string Serie { get; set; } = "";
        public long? Numero { get; set; }
        public string Identificador { get; set; } = "";
        public string Estado { get; set; } = "";
        public DateOnly FechaEmision { get; set; }
        public DateOnly? FechaVencimiento { get; set; }
        public string Moneda { get; set; } = "PEN";
        public decimal? TipoCambio { get; set; }
        public string TipoOperacion { get; set; } = "0101";
        public string ClienteTipoDoc { get; set; } = "6";
        public string ClienteNumeroDoc { get; set; } = "";
        public string ClienteRazon { get; set; } = "";
        public string? ClienteDireccion { get; set; }
        public string? ClientePais { get; set; }
        public decimal TotalGravado { get; set; }
        public decimal TotalExonerado { get; set; }
        public decimal TotalInafecto { get; set; }
        public decimal TotalExportacion { get; set; }
        public decimal TotalDescuentos { get; set; }
        public decimal Igv { get; set; }
        public decimal Total { get; set; }
        public bool PreciosIncluyenIgv { get; set; }
        public string FormaPago { get; set; } = "CONTADO";
        public List<CpeCuotaDto> Cuotas { get; set; } = new();
        public string? DetraccionCodigo { get; set; }
        public decimal? DetraccionPorcentaje { get; set; }
        public decimal? DetraccionMonto { get; set; }
        public string? DetraccionCuenta { get; set; }
        public long? RefCpeId { get; set; }
        public string? RefIdentificador { get; set; }
        public string? MotivoCodigo { get; set; }
        public string? MotivoDescripcion { get; set; }
        public string? Observacion { get; set; }
        public string? OrdenCompraRef { get; set; }
        public string? GuiaRef { get; set; }
        public int? ContratoId { get; set; }
        public int? ProyectoId { get; set; }
        public string? SunatCodigo { get; set; }
        public string? SunatDescripcion { get; set; }
        public DateTimeOffset? EnviadoEn { get; set; }
        public DateTimeOffset? RespondidoEn { get; set; }
        public string? BajaMotivo { get; set; }
        public string? BajaTicket { get; set; }
        public bool TieneXml { get; set; }
        public bool TieneCdr { get; set; }
        /// <summary>Beta o Produccion: a qué SUNAT apunta este servidor ahora mismo.</summary>
        public string Ambiente { get; set; } = "";
        /// <summary>Datos del emisor para la representación impresa.</summary>
        public string EmisorRuc { get; set; } = "";
        public string EmisorRazonSocial { get; set; } = "";
        public string? EmisorDireccion { get; set; }
        public string? XmlHashFirma { get; set; }
        public string TotalEnLetras { get; set; } = "";
        public string? ContratoCodigo { get; set; }
        public List<CpeItemDetalleDto> Items { get; set; } = new();
        public List<CpeAvisoDto> Avisos { get; set; } = new();
    }

    public class CpeListItemDto
    {
        public long Id { get; set; }
        public string TipoDocumento { get; set; } = "";
        public string Identificador { get; set; } = "";
        public string Estado { get; set; } = "";
        public DateOnly FechaEmision { get; set; }
        public string ClienteRazon { get; set; } = "";
        public string ClienteNumeroDoc { get; set; } = "";
        public string Moneda { get; set; } = "PEN";
        public decimal Total { get; set; }
    }

    public class CpeListResponseDto
    {
        public List<CpeListItemDto> Data { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
    }

    public class CpeBajaDto
    {
        public string Motivo { get; set; } = "";
    }

    public class CpeResumenDto
    {
        public long Id { get; set; }
        public string Identificador { get; set; } = "";
        public DateOnly FechaReferencia { get; set; }
        public string Estado { get; set; } = "";
        public string? Ticket { get; set; }
        public int Cantidad { get; set; }
        public string? SunatCodigo { get; set; }
        public string? SunatDescripcion { get; set; }
        public DateTimeOffset CreadoEn { get; set; }
    }

    /// <summary>Boletas emitidas de una fecha que todavía no se informaron a SUNAT.</summary>
    public class CpePendienteResumenDto
    {
        public DateOnly Fecha { get; set; }
        public int Cantidad { get; set; }
        public int Bajas { get; set; }
        public decimal Total { get; set; }
    }

    public class CpeSerieDto
    {
        public int Id { get; set; }
        public string TipoDocumento { get; set; } = "";
        public string Serie { get; set; } = "";
        public long UltimoNumero { get; set; }
        public bool Activa { get; set; }
    }

    public class CpeSerieUpsertDto
    {
        public string TipoDocumento { get; set; } = "01";
        public string Serie { get; set; } = "";
        /// <summary>Último número ya emitido por otros medios; el sistema continúa desde el siguiente.</summary>
        public long UltimoNumero { get; set; }
        public bool Activa { get; set; } = true;
    }
}

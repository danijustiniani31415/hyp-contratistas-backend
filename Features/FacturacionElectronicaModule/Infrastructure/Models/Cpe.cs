using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.PedidosModule.Infrastructure.Models;

namespace Abril_Backend.Features.FacturacionElectronicaModule.Infrastructure.Models
{
    /// <summary>Talonario de comprobantes electrónicos: el correlativo se toma con bloqueo de fila al emitir.</summary>
    [Table("lb_cpe_serie")]
    public class CpeSerie
    {
        public int Id { get; set; }
        /// <summary>01 factura, 07 nota de crédito, 08 nota de débito.</summary>
        public string TipoDocumento { get; set; } = null!;
        public string Serie { get; set; } = null!;
        /// <summary>Último número ya usado; el siguiente es +1.</summary>
        public long UltimoNumero { get; set; }
        public bool Activa { get; set; } = true;
        public DateTimeOffset CreadoEn { get; set; }
    }

    /// <summary>
    /// Comprobante electrónico emitido por H&amp;P (factura, nota de crédito o de débito). Ciclo de vida ante SUNAT:
    /// BORRADOR → ACEPTADO / ACEPTADO_OBS / RECHAZADO → BAJA_PENDIENTE → ANULADO.
    /// </summary>
    [Table("lb_cpe")]
    public class Cpe
    {
        public long Id { get; set; }
        public string TipoDocumento { get; set; } = "01";
        public string Serie { get; set; } = null!;
        public long? Numero { get; set; }
        public string Estado { get; set; } = "BORRADOR";

        public DateOnly FechaEmision { get; set; }
        public TimeOnly? HoraEmision { get; set; }
        public DateOnly? FechaVencimiento { get; set; }
        public string Moneda { get; set; } = "PEN";
        public decimal? TipoCambio { get; set; }
        public string TipoOperacion { get; set; } = "0101";

        public string ClienteTipoDoc { get; set; } = "6";
        public string ClienteNumeroDoc { get; set; } = null!;
        public string ClienteRazon { get; set; } = null!;
        public string? ClienteDireccion { get; set; }
        /// <summary>País del cliente (ISO 3166, 2 letras): solo exportación.</summary>
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
        public string? CuotasJson { get; set; }

        public string? DetraccionCodigo { get; set; }
        public decimal? DetraccionPorcentaje { get; set; }
        public decimal? DetraccionMonto { get; set; }
        public string? DetraccionCuenta { get; set; }

        public long? RefCpeId { get; set; }
        public string? RefTipoDocumento { get; set; }
        public string? RefSerie { get; set; }
        public long? RefNumero { get; set; }
        public string? MotivoCodigo { get; set; }
        public string? MotivoDescripcion { get; set; }

        public string? Observacion { get; set; }
        public string? OrdenCompraRef { get; set; }
        public string? GuiaRef { get; set; }
        public int? ContratoId { get; set; }
        public int? ProyectoId { get; set; }

        public string? XmlNombreArchivo { get; set; }
        public string? XmlHashFirma { get; set; }
        public string? XmlFirmado { get; set; }
        public string? CdrXml { get; set; }
        public string? SunatCodigo { get; set; }
        public string? SunatDescripcion { get; set; }
        public DateTimeOffset? EnviadoEn { get; set; }
        public DateTimeOffset? RespondidoEn { get; set; }

        public string? BajaMotivo { get; set; }
        public string? BajaTicket { get; set; }
        public string? BajaIdentificador { get; set; }
        public string? BajaCdrXml { get; set; }
        public DateTimeOffset? BajaEn { get; set; }

        public long CreadoPorUsuarioSistemaId { get; set; }
        public DateTimeOffset CreadoEn { get; set; }
        public long? EmitidoPorUsuarioSistemaId { get; set; }

        [ForeignKey(nameof(ContratoId))]
        public Contrato? Contrato { get; set; }

        public List<CpeItem> Items { get; set; } = new();

        /// <summary>F001-123, o F001-borrador mientras no tiene número.</summary>
        [NotMapped]
        public string Identificador => $"{Serie}-{(Numero.HasValue ? Numero.Value.ToString() : "borrador")}";
    }

    [Table("lb_cpe_item")]
    public class CpeItem
    {
        public long Id { get; set; }
        public long CpeId { get; set; }
        public int Orden { get; set; }
        public string? Codigo { get; set; }
        public string Descripcion { get; set; } = null!;
        public string UnidadMedida { get; set; } = "NIU";
        public decimal Cantidad { get; set; }
        public decimal ValorUnitario { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
        public decimal ValorVenta { get; set; }
        /// <summary>Catálogo 07: 10 gravado, 20 exonerado, 30 inafecto, 40 exportación.</summary>
        public string AfectacionIgv { get; set; } = "10";
        public decimal Igv { get; set; }
        public int? ProductoId { get; set; }
        public int? PartidaId { get; set; }

        [ForeignKey(nameof(CpeId))]
        public Cpe? Cpe { get; set; }
    }

    [Table("lb_cpe_envio")]
    public class CpeEnvio
    {
        public long Id { get; set; }
        public long CpeId { get; set; }
        public string Operacion { get; set; } = null!;
        public string Ambiente { get; set; } = null!;
        public bool Ok { get; set; }
        public string? Codigo { get; set; }
        public string? Mensaje { get; set; }
        public DateTimeOffset CreadoEn { get; set; }
    }
}

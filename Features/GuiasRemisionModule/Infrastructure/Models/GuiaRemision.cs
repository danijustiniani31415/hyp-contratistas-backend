using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.PersonasModule.Infrastructure.Models;

namespace Abril_Backend.Features.GuiasRemisionModule.Infrastructure.Models
{
    /// <summary>
    /// Guía de Remisión Electrónica - Remitente (Fase 3, punto 8). Emisión real ante el SEE de
    /// SUNAT (sin OSE) — el estado de este header refleja el ciclo de vida ante SUNAT, no solo
    /// un registro interno: BORRADOR -> ENVIADA -> ACEPTADA/RECHAZADA -> ANULADA.
    /// </summary>
    [Table("lb_guia_remision")]
    public class GuiaRemision
    {
        public long Id { get; set; }
        public string Serie { get; set; } = null!;
        /// <summary>Correlativo SUNAT: null mientras es BORRADOR, se asigna recién al transmitir.</summary>
        public long? Numero { get; set; }
        public string Estado { get; set; } = "BORRADOR";

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
        /// <summary>DNI del conductor: SUNAT lo exige como documento de identidad (la licencia va aparte).</summary>
        public string? ConductorDni { get; set; }
        /// <summary>Apellidos del conductor (los nombres van en ConductorNombres). Null en guías antiguas: ahí
        /// ConductorNombres trae "Apellidos Nombres" juntos y el XML los separa por posición.</summary>
        public string? ConductorApellidos { get; set; }
        /// <summary>Tarjeta de circulación / certificado de habilitación del vehículo principal.</summary>
        public string? VehiculoNroCirculacion { get; set; }
        /// <summary>JSON de <see cref="ConductorSecundario"/> — ver GuiaSecundarios.</summary>
        public string? ConductoresSecundariosJson { get; set; }
        /// <summary>JSON de <see cref="VehiculoSecundario"/> — ver GuiaSecundarios.</summary>
        public string? VehiculosSecundariosJson { get; set; }

        // Anulación: la baja ante SUNAT se hace en SOL (no hay API); acá queda el rastro de quién la
        // registró, por qué y con qué constancia de baja, para poder auditarla.
        public string? AnulacionMotivo { get; set; }
        public string? AnulacionConstancia { get; set; }
        public long? AnuladaPorUsuarioSistemaId { get; set; }
        public DateTimeOffset? AnuladaEn { get; set; }

        public string? Observacion { get; set; }
        public string? ReferenciaTipo { get; set; }
        public long? ReferenciaId { get; set; }

        public long CreadoPorUsuarioSistemaId { get; set; }
        public DateTimeOffset CreadoEn { get; set; }

        /// <summary>true cuando ya se movió el stock y se avanzó el pedido (recién al ser ACEPTADA por SUNAT).</summary>
        public bool DespachoAplicado { get; set; }

        public string? XmlNombreArchivo { get; set; }
        public string? XmlHashFirma { get; set; }
        /// <summary>XML firmado tal como se envió a SUNAT: respaldo legal, no se pierde aunque cambien los datos.</summary>
        public string? XmlFirmado { get; set; }
        /// <summary>XML del CDR que entregó SUNAT al aceptar o rechazar la guía.</summary>
        public string? CdrXml { get; set; }
        /// <summary>Enlace del QR oficial de SUNAT (descargaqr?hashqr=…), si lo entregó.</summary>
        public string? QrUrl { get; set; }
        public string? Ticket { get; set; }
        public string? CdrCodigoRespuesta { get; set; }
        public string? CdrDescripcion { get; set; }
        public DateTimeOffset? EnviadoEn { get; set; }
        public DateTimeOffset? RespondidoEn { get; set; }

        [ForeignKey(nameof(AlmacenOrigenId))]
        public Almacen? AlmacenOrigen { get; set; }
        [ForeignKey(nameof(AlmacenDestinoId))]
        public Almacen? AlmacenDestino { get; set; }
        [ForeignKey(nameof(CreadoPorUsuarioSistemaId))]
        public UsuarioSistema? CreadoPor { get; set; }

        public ICollection<GuiaRemisionItem> Items { get; set; } = new List<GuiaRemisionItem>();
    }
}

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
        public long Numero { get; set; }
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

        public string? Observacion { get; set; }
        public string? ReferenciaTipo { get; set; }
        public long? ReferenciaId { get; set; }

        public long CreadoPorUsuarioSistemaId { get; set; }
        public DateTimeOffset CreadoEn { get; set; }

        public string? XmlNombreArchivo { get; set; }
        public string? XmlHashFirma { get; set; }
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

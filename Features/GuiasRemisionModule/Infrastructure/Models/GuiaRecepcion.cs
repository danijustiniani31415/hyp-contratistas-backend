using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.GuiasRemisionModule.Infrastructure.Models
{
    /// <summary>Un viaje de recepción en el almacén de destino: qué llegó bien, dañado y (al cerrar) lo que no llegó.</summary>
    [Table("lb_guia_recepcion")]
    public class GuiaRecepcion
    {
        public long Id { get; set; }
        public long GuiaRemisionId { get; set; }
        public long RecibidoPor { get; set; }
        public DateTimeOffset RecibidoEn { get; set; }
        /// <summary>true = el almacenero declaró que ya no llegará más: lo pendiente pasa a faltante.</summary>
        public bool Cierra { get; set; }
        public string? Observacion { get; set; }

        [ForeignKey("RecepcionId")]
        public List<GuiaRecepcionItem> Items { get; set; } = new();
    }

    [Table("lb_guia_recepcion_item")]
    public class GuiaRecepcionItem
    {
        public long Id { get; set; }
        public long RecepcionId { get; set; }
        public long GuiaItemId { get; set; }
        public decimal CantidadOk { get; set; }
        public decimal CantidadDanada { get; set; }
        public decimal CantidadFaltante { get; set; }
        public string? Motivo { get; set; }
        public string? Observacion { get; set; }
    }

    [Table("lb_guia_recepcion_foto")]
    public class GuiaRecepcionFoto
    {
        public long Id { get; set; }
        public long RecepcionId { get; set; }
        public string Nombre { get; set; } = null!;
        public string Mime { get; set; } = null!;
        public byte[] Contenido { get; set; } = null!;
        public DateTimeOffset CreadoEn { get; set; }
    }

    /// <summary>Lo faltante o dañado de una recepción. Queda ABIERTA hasta que logística la resuelve.</summary>
    [Table("lb_guia_diferencia")]
    public class GuiaDiferencia
    {
        public long Id { get; set; }
        public long GuiaRemisionId { get; set; }
        public long GuiaItemId { get; set; }
        public long? RecepcionId { get; set; }
        /// <summary>FALTANTE o DANADO.</summary>
        public string Tipo { get; set; } = null!;
        public decimal Cantidad { get; set; }
        public string? Motivo { get; set; }
        public string? Observacion { get; set; }
        /// <summary>ABIERTA o RESUELTA.</summary>
        public string Estado { get; set; } = "ABIERTA";
        /// <summary>REPOSICION, RECLAMO, MERMA o LLEGO.</summary>
        public string? Resolucion { get; set; }
        public string? ResolucionNota { get; set; }
        public long? ResueltaPor { get; set; }
        public DateTimeOffset? ResueltaEn { get; set; }
        public long CreadoPor { get; set; }
        public DateTimeOffset CreadoEn { get; set; }
    }
}

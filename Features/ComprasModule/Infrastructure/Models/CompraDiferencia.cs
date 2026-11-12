using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.ComprasModule.Infrastructure.Models
{
    /// <summary>Lo faltante, dañado o sobrante de una compra. Queda ABIERTA hasta que logística la resuelve.</summary>
    [Table("lb_compra_diferencia")]
    public class CompraDiferencia
    {
        public long Id { get; set; }
        public long OrdenCompraId { get; set; }
        public long OrdenCompraItemId { get; set; }
        public long? RecepcionId { get; set; }
        /// <summary>FALTANTE, DANADO o SOBRANTE.</summary>
        public string Tipo { get; set; } = null!;
        public decimal Cantidad { get; set; }
        public string? Motivo { get; set; }
        public string? Observacion { get; set; }
        public string Estado { get; set; } = "ABIERTA";
        /// <summary>REPOSICION, RECLAMO, NOTA_CREDITO o ACEPTADO.</summary>
        public string? Resolucion { get; set; }
        public string? ResolucionNota { get; set; }
        public long? ResueltaPor { get; set; }
        public DateTimeOffset? ResueltaEn { get; set; }
        public long CreadoPor { get; set; }
        public DateTimeOffset CreadoEn { get; set; }
    }
}

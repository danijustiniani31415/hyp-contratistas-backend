using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Infrastructure.Models;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_interconsultas")]
    public class SsInterconsulta
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("emo_id")]
        public int? EmoId { get; set; }

        [Column("worker_id")]
        public int WorkerId { get; set; }

        [Column("especialidad")]
        public string Especialidad { get; set; } = string.Empty;

        [Column("medico_deriva_id")]
        public int? MedicoDerivaId { get; set; }

        [Column("fecha_derivacion")]
        public DateOnly FechaDerivacion { get; set; }

        [Column("fecha_atencion")]
        public DateOnly? FechaAtencion { get; set; }

        [Column("centro_atencion")]
        public string? CentroAtencion { get; set; }

        [Column("diagnostico")]
        public string? Diagnostico { get; set; }

        [Column("cie10")]
        public string? Cie10 { get; set; }

        [Column("resultado")]
        public string? Resultado { get; set; }

        [Column("url_informe")]
        public string? UrlInforme { get; set; }

        [Column("estado")]
        public string Estado { get; set; } = "Pendiente";

        [Column("requiere_seguimiento")]
        public bool RequiereSeguimiento { get; set; }

        [Column("registrado_por_id")]
        public int? RegistradoPorId { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTimeOffset? UpdatedAt { get; set; }

        [ForeignKey(nameof(EmoId))]
        public WorkerEmo? Emo { get; set; }

        [ForeignKey(nameof(WorkerId))]
        public Worker? Worker { get; set; }

        [ForeignKey(nameof(MedicoDerivaId))]
        public SsMedicoOcupacional? MedicoDeriva { get; set; }
    }
}

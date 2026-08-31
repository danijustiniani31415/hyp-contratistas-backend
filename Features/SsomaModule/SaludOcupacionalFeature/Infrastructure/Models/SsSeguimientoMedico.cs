using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Infrastructure.Models;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_seguimientos_medicos")]
    public class SsSeguimientoMedico
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("worker_id")]
        public int WorkerId { get; set; }

        [Column("tipo")]
        public string Tipo { get; set; } = string.Empty;

        [Column("fecha_inicio")]
        public DateOnly FechaInicio { get; set; }

        [Column("fecha_cierre")]
        public DateOnly? FechaCierre { get; set; }

        [Column("descripcion")]
        public string Descripcion { get; set; } = string.Empty;

        [Column("diagnostico_cie10")]
        public string? DiagnosticoCie10 { get; set; }

        [Column("medico_id")]
        public int? MedicoId { get; set; }

        [Column("estado")]
        public string Estado { get; set; } = "Activo";

        [Column("dias_perdidos")]
        public int? DiasPerdidos { get; set; }

        [Column("requiere_descanso")]
        public bool RequiereDescanso { get; set; }

        [Column("url_documentos")]
        public string? UrlDocumentos { get; set; }

        [Column("registrado_por_id")]
        public int? RegistradoPorId { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTimeOffset? UpdatedAt { get; set; }

        [ForeignKey(nameof(WorkerId))]
        public Worker? Worker { get; set; }

        [ForeignKey(nameof(MedicoId))]
        public SsMedicoOcupacional? Medico { get; set; }
    }
}

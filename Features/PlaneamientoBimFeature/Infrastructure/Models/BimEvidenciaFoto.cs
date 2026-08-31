using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Models
{
    [Table("bim_evidencia_foto")]
    public class BimEvidenciaFoto
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("project_id")]
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        [Column("fecha")]
        public DateOnly Fecha { get; set; }

        /// <summary>"GENERAL" (Evidencia Fotográfica de Carga Diaria) | "PROCURA".</summary>
        [Column("categoria")]
        public string Categoria { get; set; } = "GENERAL";

        [Column("url")]
        public string Url { get; set; } = string.Empty;

        [Column("created_user_id")]
        public int CreatedUserId { get; set; }

        [Column("created_date_time")]
        public DateTimeOffset CreatedDateTime { get; set; }
    }
}

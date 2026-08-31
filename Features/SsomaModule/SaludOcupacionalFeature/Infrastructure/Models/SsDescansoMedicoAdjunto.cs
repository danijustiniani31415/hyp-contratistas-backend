using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_descanso_medico_adjunto")]
    public class SsDescansoMedicoAdjunto
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("descanso_id")]
        public int DescansoId { get; set; }

        [Column("url")]
        public string Url { get; set; } = string.Empty;

        [Column("nombre_archivo")]
        public string? NombreArchivo { get; set; }

        /// <summary>
        /// Ubicación estable en SharePoint del archivo (drive + item). Con esto el backend
        /// lo descarga vía Graph con su propio token, sin depender de que el navegador del
        /// usuario tenga sesión de Microsoft 365. Null en los adjuntos anteriores a la
        /// carpeta configurable (Azure Blob o ruta relativa del sitio SSOMAApps), que se
        /// siguen sirviendo a partir de <see cref="Url"/>.
        /// </summary>
        [Column("drive_id")]
        public string? DriveId { get; set; }

        [Column("item_id")]
        public string? ItemId { get; set; }

        [Column("state")]
        public bool State { get; set; } = true;

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [Column("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        [ForeignKey(nameof(DescansoId))]
        public SsDescansoMedico? Descanso { get; set; }
    }
}

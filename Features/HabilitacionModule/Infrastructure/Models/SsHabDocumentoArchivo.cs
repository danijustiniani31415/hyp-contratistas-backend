using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("ss_hab_documento_archivo")]
    public class SsHabDocumentoArchivo
    {
        public int Id { get; set; }
        public int VersionId { get; set; }
        public string ArchivoUrl { get; set; } = string.Empty;
        public string? NombreArchivo { get; set; }
        public bool EsZip { get; set; } = false;
        public string? ZipContenido { get; set; }
        public int Orden { get; set; } = 0;
        public DateTime? CreatedAt { get; set; }

        [ForeignKey(nameof(VersionId))]
        public SsHabDocumentoVersion? Version { get; set; }
    }
}

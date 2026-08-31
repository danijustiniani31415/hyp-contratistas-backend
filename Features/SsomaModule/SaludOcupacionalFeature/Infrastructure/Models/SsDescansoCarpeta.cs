using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    /// <summary>
    /// Carpeta única (singleton) de SharePoint donde se guardan los certificados de los
    /// descansos médicos (tabla <c>ss_descanso_carpeta</c>). Se define por base de datos —no
    /// por appsettings ni hardcodeada— para poder cambiarla sin redeploy y para que cada
    /// entorno apunte a la biblioteca que quiera. Existe a lo sumo una fila vigente
    /// (<c>state = true</c>): se guarda el link tal cual y el servicio lo resuelve a
    /// driveId/folderId vía Graph al subir. Mismo patrón que <c>ga_captura_folder</c>.
    /// </summary>
    [Table("ss_descanso_carpeta")]
    public class SsDescansoCarpeta
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Link de la carpeta/biblioteca de SharePoint pegado tal cual.</summary>
        [Column("link_url")]
        public string LinkUrl { get; set; } = string.Empty;

        /// <summary>Nombre legible de la carpeta (solo referencia para quien lee la tabla).</summary>
        [Column("folder_name")]
        public string? FolderName { get; set; }

        [Column("active")]
        public bool Active { get; set; } = true;

        [Column("state")]
        public bool State { get; set; } = true;

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [Column("created_user_id")]
        public int? CreatedUserId { get; set; }

        [Column("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        [Column("updated_user_id")]
        public int? UpdatedUserId { get; set; }
    }
}

namespace Abril_Backend.Features.GestionAdministrativa.Shared.Models
{
    /// <summary>
    /// Carpeta única (singleton) de SharePoint donde se guardan los PDF de planillas de rendición
    /// de salidas (tabla <c>ga_rendicion_folder</c>). Se define por base de datos —no por appsettings
    /// ni hardcodeado— para que dev y producción apunten a bibliotecas distintas y se pueda cambiar
    /// por BD sin redeploy. Existe a lo sumo una fila vigente (state = true); se guarda el link tal
    /// cual (<c>link_url</c>) y el servicio lo resuelve vía Graph al subir. Mismo patrón que
    /// <c>ga_captura_folder</c> (capturas de Salidas) y <c>gth_sustento_folder</c> (Reclutamiento).
    /// </summary>
    public class GaRendicionFolder
    {
        public int GaRendicionFolderId { get; set; }

        /// <summary>Link de la carpeta de SharePoint (se resuelve a driveId/folderId al subir).</summary>
        public string LinkUrl { get; set; } = null!;

        /// <summary>Nombre legible de la carpeta (opcional, solo referencia).</summary>
        public string? FolderName { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; } = true;
        public bool State { get; set; } = true;
    }
}

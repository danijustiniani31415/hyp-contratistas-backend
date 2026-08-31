namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Carpeta única (singleton) de SharePoint donde se guardan los sustentos de las
    /// solicitudes de personal (tabla <c>gth_sustento_folder</c>). Se define por base de
    /// datos —no por appsettings— para que dev y producción apunten a bibliotecas distintas
    /// y se pueda cambiar por BD sin redeploy. Existe a lo sumo una fila vigente (state = true);
    /// se guarda el link tal cual (<c>link_url</c>) y el servicio lo resuelve vía Graph al subir.
    /// Mismo espíritu que <c>ga_adjunto_folder</c> (Salidas).
    /// </summary>
    public class GthSustentoFolder
    {
        public int GthSustentoFolderId { get; set; }

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

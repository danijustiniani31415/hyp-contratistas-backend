namespace Abril_Backend.Features.GestionAdministrativa.Shared.Models
{
    /// <summary>
    /// Carpeta única (singleton) de OneDrive/SharePoint donde se guardan los documentos
    /// adjuntos de las solicitudes de salida (motivos con requiere_adjunto = true).
    /// Existe a lo sumo una fila vigente (state = true); al editarla se pega un link, el
    /// sistema lo resuelve a su ubicación estable (driveId + folderId) y a partir de ahí
    /// se sube todo ahí. Mismo patrón que invoice_folder (Contabilidad).
    /// </summary>
    public class GaAdjuntoFolder
    {
        public int GaAdjuntoFolderId { get; set; }
        /// <summary>Link original pegado por el usuario (para mostrar/reabrir).</summary>
        public string LinkUrl { get; set; } = null!;
        /// <summary>Ubicación estable resuelta vía Graph Shares API.</summary>
        public string DriveId { get; set; } = null!;
        public string FolderId { get; set; } = null!;
        public string? FolderName { get; set; }
        public string? WebUrl { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}

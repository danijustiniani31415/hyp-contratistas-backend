namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Infrastructure.Models
{
    /// <summary>
    /// Carpeta única (singleton) de SharePoint/OneDrive donde se guardan los archivos adjuntos
    /// de las actas de reunión. Existe a lo sumo una fila vigente (state = true): el usuario pega
    /// un link, el sistema lo resuelve a su ubicación estable (driveId + folderId) vía Graph y a
    /// partir de ahí todos los adjuntos se suben ahí. Si no hay fila, se usa el storage por defecto.
    /// </summary>
    public class ReunionFolder
    {
        public int ReunionFolderId { get; set; }
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

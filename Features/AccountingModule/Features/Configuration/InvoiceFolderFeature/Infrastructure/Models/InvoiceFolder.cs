namespace Abril_Backend.Features.AccountingModule.Features.Configuration.InvoiceFolderFeature.Infrastructure.Models
{
    /// <summary>
    /// Carpeta única (singleton) de OneDrive/SharePoint donde se guardan TODAS las facturas.
    /// Existe a lo sumo una fila vigente (state = true); al editarla se pega un link, el sistema
    /// lo resuelve a su ubicación estable (driveId + folderId) y a partir de ahí se sube todo ahí.
    /// </summary>
    public class InvoiceFolder
    {
        public int InvoiceFolderId { get; set; }
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

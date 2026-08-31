namespace Abril_Backend.Shared.Services.SharePoint.Dtos
{
    /// <summary>
    /// Resultado de resolver un link de SharePoint/OneDrive vía Graph Shares API.
    /// Contiene la ubicación estable (driveId + itemId) de la carpeta/archivo.
    /// </summary>
    public class ShareLinkResolveDto
    {
        public string DriveId { get; set; } = null!;
        public string ItemId { get; set; } = null!;
        public string? Name { get; set; }
        public string? WebUrl { get; set; }
        /// <summary>True si el item resuelto es una carpeta.</summary>
        public bool IsFolder { get; set; }
    }
}

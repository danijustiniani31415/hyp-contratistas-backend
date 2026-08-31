namespace Abril_Backend.Infrastructure.Models
{
    public class CatalogItem
    {
        public int CatalogItemId { get; set; }
        public int CatalogTypeId { get; set; }
        public string CatalogItemDescription { get; set; } = string.Empty;
        public bool Active { get; set; }

        public CatalogType CatalogType { get; set; } = null!;
    }
}

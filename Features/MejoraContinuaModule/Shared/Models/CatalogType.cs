namespace Abril_Backend.Infrastructure.Models
{
    public class CatalogType
    {
        public int CatalogTypeId { get; set; }
        public string CatalogTypeName { get; set; } = string.Empty;
        public bool Active { get; set; }
    }
}

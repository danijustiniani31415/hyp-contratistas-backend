namespace Abril_Backend.Infrastructure.Models
{
    public class ScopeTemplateItem
    {
        public int ScopeTemplateItemId { get; set; }
        public int ScopeTemplateId { get; set; }
        public int CatalogItemId { get; set; }
        public int? ScopeTemplateItemParentId { get; set; }
        public int DisplayOrder { get; set; }
        public bool Active { get; set; }

        public ScopeTemplate ScopeTemplate { get; set; } = null!;
        public CatalogItem CatalogItem { get; set; } = null!;
        public ScopeTemplateItem? Parent { get; set; }
        public ICollection<ScopeTemplateItem> Children { get; set; } = new List<ScopeTemplateItem>();
    }
}

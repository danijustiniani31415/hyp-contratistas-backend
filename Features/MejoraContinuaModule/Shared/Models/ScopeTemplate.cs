namespace Abril_Backend.Infrastructure.Models
{
    public class ScopeTemplate
    {
        public int ScopeTemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public bool Active { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }

        public ICollection<ScopeTemplateItem> Items { get; set; } = new List<ScopeTemplateItem>();
    }
}

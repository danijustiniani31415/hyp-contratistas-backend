using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonAreasFeature.Infrastructure.Models;

namespace Abril_Backend.Infrastructure.Models
{
    public class ScopeItem
    {
        public int ScopeItemId { get; set; }
        public int LessonAreaId { get; set; }
        public int CatalogItemId { get; set; }
        public int? ScopeItemParentId { get; set; }
        public int DisplayOrder { get; set; }
        public bool Active { get; set; }

        public LessonArea LessonArea { get; set; } = null!;
        public CatalogItem CatalogItem { get; set; } = null!;
        public ScopeItem? Parent { get; set; }
        public ICollection<ScopeItem> Children { get; set; } = new List<ScopeItem>();
    }
}

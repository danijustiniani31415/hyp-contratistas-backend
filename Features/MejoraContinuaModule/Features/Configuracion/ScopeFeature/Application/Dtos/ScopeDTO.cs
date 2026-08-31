namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.ScopeFeature.Application.Dtos
{
    // ── ScopeItem ────────────────────────────────────────────────
    public class ScopeItemDTO
    {
        public int ScopeItemId { get; set; }
        public int LessonAreaId { get; set; }
        public int CatalogItemId { get; set; }
        public string CatalogItemDescription { get; set; } = string.Empty;
        public string CatalogTypeName { get; set; } = string.Empty;
        public int? ScopeItemParentId { get; set; }
        public int DisplayOrder { get; set; }
        public bool Active { get; set; }
        public List<ScopeItemDTO> Children { get; set; } = new();
    }

    public class ScopeItemUpsertDTO
    {
        public int LessonAreaId { get; set; }
        public List<ScopeItemNodeDTO> Items { get; set; } = new();
    }

    // Igual semántica que ScopeTemplateItemNodeDTO (ver más abajo):
    //   • En el request (PUT /scope/tree): NodeId es un id de scope_item real o
    //     un id temporal negativo (-1, -2, ...) generado por el cliente para
    //     nodos nuevos. ParentNodeId apunta a otro NodeId del mismo payload.
    //   • Un mismo catalog_item puede aparecer varias veces bajo padres
    //     distintos — por eso CatalogItemId NO sirve como clave de árbol.
    public class ScopeItemNodeDTO
    {
        public int NodeId { get; set; }
        public int? ParentNodeId { get; set; }
        public int CatalogItemId { get; set; }
        public int DisplayOrder { get; set; }
    }

    // ── ScopeTemplate ────────────────────────────────────────────
    // NodeId/ParentNodeId identifican un nodo único dentro del árbol de la
    // plantilla (un mismo catalog_item puede aparecer varias veces bajo padres
    // distintos, por eso CatalogItemId NO sirve como clave de árbol).
    //   • En la respuesta (GET): NodeId = scope_template_item_id real.
    //   • En el request (POST/PUT): NodeId puede ser un id real (nodo existente
    //     reutilizable) o un id temporal negativo (-1, -2, ...) generado por el
    //     cliente para nodos nuevos. El backend solo usa NodeId/ParentNodeId
    //     para resolver el árbol durante la inserción.
    public class ScopeTemplateItemNodeDTO
    {
        public int NodeId { get; set; }
        public int? ParentNodeId { get; set; }
        public int CatalogItemId { get; set; }
        public string CatalogItemDescription { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    public class ScopeTemplateDTO
    {
        public int ScopeTemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public bool Active { get; set; }
        public List<ScopeTemplateItemNodeDTO> Items { get; set; } = new();
    }

    public class ScopeTemplateCreateDTO
    {
        public string TemplateName { get; set; } = string.Empty;
        public List<ScopeTemplateItemNodeDTO> Items { get; set; } = new();
    }

    public class ScopeTemplateUpdateDTO
    {
        public int ScopeTemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public List<ScopeTemplateItemNodeDTO> Items { get; set; } = new();
    }
}

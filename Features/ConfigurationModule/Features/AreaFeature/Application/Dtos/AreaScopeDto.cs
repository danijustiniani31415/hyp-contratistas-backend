namespace Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Application.Dtos
{
    public class AreaScopeTreeDto
    {
        public int AreaScopeId { get; set; }
        public int AreaItemId { get; set; }
        public string AreaItemName { get; set; } = string.Empty;
        public int AreaTypeId { get; set; }
        public string AreaTypeName { get; set; } = string.Empty;
        public int? AreaScopeParentId { get; set; }
        public int DisplayOrder { get; set; }
        public bool Active { get; set; }
        /// <summary>Cantidad de trabajadores activos asignados directamente a este nodo (workers.area_scope_id).</summary>
        public int WorkersCount { get; set; }
        public List<AreaScopeTreeDto> Children { get; set; } = new();
    }

    /// <summary>Trabajador asignado a un nodo del árbol (workers.area_scope_id).</summary>
    public class AreaScopeWorkerDto
    {
        public int WorkerId { get; set; }
        public string? FullName { get; set; }
        public string? EmailCorporativo { get; set; }
        /// <summary>Nombre de la categoría (workers.worker_category_id → workers_category.name).</summary>
        public string? CategoryName { get; set; }
    }

    /// <summary>Reasignación del padre de un nodo. Null = mover a la raíz.</summary>
    public class AreaScopeUpdateParentDto
    {
        public int? NewParentAreaScopeId { get; set; }
    }

    /// <summary>Nodo dentro de una nueva rama. tempId es un identificador local del cliente.</summary>
    public class AreaScopeBranchNodeDto
    {
        public int TempId { get; set; }
        public int AreaItemId { get; set; }
        public int? ParentTempId { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class AreaScopeBranchDto
    {
        public List<AreaScopeBranchNodeDto> Nodes { get; set; } = new();
    }
}

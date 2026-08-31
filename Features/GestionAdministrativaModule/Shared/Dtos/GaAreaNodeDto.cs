namespace Abril_Backend.Features.GestionAdministrativa.Shared.Dtos
{
    /// <summary>
    /// Nodo del árbol area_scope para las pantallas de configuración del módulo
    /// (visibilidad de salidas, revisor de salidas). Lista plana; el frontend arma
    /// la jerarquía con area_scope_parent_id.
    /// </summary>
    public class GaAreaNodeDto
    {
        public int AreaScopeId { get; set; }
        public int AreaItemId { get; set; }
        public string AreaItemName { get; set; } = string.Empty;
        public int AreaTypeId { get; set; }
        public string AreaTypeName { get; set; } = string.Empty;
        public int? AreaScopeParentId { get; set; }
        public int DisplayOrder { get; set; }
    }
}

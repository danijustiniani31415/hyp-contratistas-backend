namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.CatalogFeature.Application.Dtos
{
    public class CatalogItemDTO
    {
        public int CatalogItemId { get; set; }
        public int CatalogTypeId { get; set; }
        public string CatalogTypeName { get; set; } = string.Empty;
        public string CatalogItemDescription { get; set; } = string.Empty;
        public bool Active { get; set; }
    }

    public class CatalogItemCreateDTO
    {
        public int CatalogTypeId { get; set; }
        public string CatalogItemDescription { get; set; } = string.Empty;
        public bool Active { get; set; } = true;
    }

    public class CatalogItemEditDTO
    {
        public int CatalogItemId { get; set; }
        public int CatalogTypeId { get; set; }
        public string CatalogItemDescription { get; set; } = string.Empty;
        public bool Active { get; set; }
    }
}

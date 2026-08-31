namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.CatalogFeature.Application.Dtos
{
    public class CatalogTypeDTO
    {
        public int CatalogTypeId { get; set; }
        public string CatalogTypeName { get; set; } = string.Empty;
        public bool Active { get; set; }
    }

    public class CatalogTypeCreateDTO
    {
        public string CatalogTypeName { get; set; } = string.Empty;
        public bool Active { get; set; } = true;
    }

    public class CatalogTypeEditDTO
    {
        public int CatalogTypeId { get; set; }
        public string CatalogTypeName { get; set; } = string.Empty;
        public bool Active { get; set; }
    }
}

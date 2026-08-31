namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Application.Dtos
{
    public class SubAreaDTO
    {
        public int SubAreaId { get; set; }
        public int AreaId { get; set; }
        public string AreaDescription { get; set; }
        public string SubAreaDescription { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
    }
}

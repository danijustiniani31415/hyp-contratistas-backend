namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Application.Dtos
{
    public class SubAreaEditDTO
    {
        public int SubAreaId { get; set; }
        public int AreaId { get; set; }
        public string SubAreaDescription { get; set; }
        public bool Active { get; set; }
    }
}

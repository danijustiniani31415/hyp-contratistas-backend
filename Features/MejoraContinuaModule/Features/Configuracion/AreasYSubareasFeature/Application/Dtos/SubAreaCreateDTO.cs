namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Application.Dtos
{
    public class SubAreaCreateDTO
    {
        public int AreaId { get; set; }
        public string SubAreaDescription { get; set; }
        public bool Active { get; set; }
    }
}

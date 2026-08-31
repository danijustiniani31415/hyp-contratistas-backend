namespace Abril_Backend.Features.Habilitacion.Application.Dtos.SctrVidaley
{
    public class SctrVidaLeyAprobarDto
    {
        public List<int> WorkerIdsAprobados { get; set; } = [];
        public List<int> WorkerIdsRechazados { get; set; } = [];
        public string? ObsAbril { get; set; }
        public DateTime? Vigencia { get; set; }
    }
}

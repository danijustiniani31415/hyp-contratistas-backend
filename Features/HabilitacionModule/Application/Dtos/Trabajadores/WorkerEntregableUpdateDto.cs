namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Trabajadores
{
    public class WorkerEntregableUpdateDto
    {
        public string Estado { get; set; } = string.Empty;
        public DateTime? Vigencia { get; set; }
        public string? ArchivoUrl { get; set; }
        public string? ObsAbril { get; set; }
        public string? ObsContratista { get; set; }
    }
}

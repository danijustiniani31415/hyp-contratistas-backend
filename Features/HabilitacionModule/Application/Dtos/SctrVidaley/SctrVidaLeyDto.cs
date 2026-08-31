namespace Abril_Backend.Features.Habilitacion.Application.Dtos.SctrVidaley
{
    public class SctrVidaLeyDto
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string EmpresaNombre { get; set; } = string.Empty;
        public int? ProyectoId { get; set; }
        public string ProyectoNombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string TipoPoliza { get; set; } = string.Empty;
        public DateTime? FechaInicio { get; set; }
        public int Mes { get; set; }
        public int Anio { get; set; }
        public string? ArchivoUrl { get; set; }
        public string? ArchivoUrl2 { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? Vigencia { get; set; }
        public string? ObsAbril { get; set; }
        public List<SctrWorkerDto> Workers { get; set; } = [];
    }
}

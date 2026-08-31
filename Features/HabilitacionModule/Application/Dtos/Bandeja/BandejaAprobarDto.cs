namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Bandeja
{
    public class BandejaAprobarDto
    {
        public string Estado { get; set; } = string.Empty;
        public string? ObsAbril { get; set; }
        public DateTime? Vigencia { get; set; }
    }

    public class BandejaBulkAprobarDto
    {
        public List<int> Ids { get; set; } = [];
        public string Tipo { get; set; } = string.Empty;
    }

    public class BandejaBulkResultDto
    {
        public int Procesados { get; set; }
        public List<int> NoEncontrados { get; set; } = [];
    }
}

namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Equipos
{
    public class EquipoEntregableDto
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public string NombreItem { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime? Vigencia { get; set; }
        public string? ArchivoUrl { get; set; }
        public string? ObsAbril { get; set; }
        public string? ObsContratista { get; set; }
        public bool RequiereVigencia { get; set; }
    }
}

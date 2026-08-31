namespace Abril_Backend.Features.Habilitacion.Application.Dtos.ControlAcceso
{
    public class TareoDto
    {
        public int Id { get; set; }
        public int ProyectoId { get; set; }
        public string ProyectoNombre { get; set; } = string.Empty;
        public DateOnly Fecha { get; set; }
        public string? Observaciones { get; set; }
        public int? CreadoPor { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<TareoDetalleCasaDto> DetallesCasa { get; set; } = [];
        public List<TareoDetalleContratistaDto> DetallesContratista { get; set; } = [];
    }
}

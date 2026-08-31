namespace Abril_Backend.Features.Habilitacion.Application.Dtos.ControlAcceso
{
    public class TareoCreateDto
    {
        public int ProyectoId { get; set; }
        public DateOnly Fecha { get; set; }
        public string? Observaciones { get; set; }
        public List<TareoDetalleCasaDto> DetallesCasa { get; set; } = [];
        public List<TareoDetalleContratistaDto> DetallesContratista { get; set; } = [];
    }
}

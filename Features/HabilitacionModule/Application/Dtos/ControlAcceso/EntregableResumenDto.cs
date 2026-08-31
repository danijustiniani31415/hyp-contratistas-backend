namespace Abril_Backend.Features.Habilitacion.Application.Dtos.ControlAcceso
{
    public class EntregableResumenDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime? Vigencia { get; set; }
    }
}

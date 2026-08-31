namespace Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Infrastructure.Models
{
    public class VecinoSolicitudEstado
    {
        public int VecinoSolicitudEstadoId { get; set; }
        public string Descripcion { get; set; } = null!;
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}

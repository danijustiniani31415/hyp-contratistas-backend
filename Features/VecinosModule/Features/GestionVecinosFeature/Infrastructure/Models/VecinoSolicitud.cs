namespace Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Infrastructure.Models
{
    public class VecinoSolicitud
    {
        public int VecinoSolicitudId { get; set; }

        public int VecinoId { get; set; }
        public Vecino? Vecino { get; set; }

        public string Descripcion { get; set; } = null!;
        public bool EsCritica { get; set; }

        public int VecinoSolicitudEstadoId { get; set; }
        public VecinoSolicitudEstado? Estado { get; set; }

        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}

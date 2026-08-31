namespace Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Infrastructure.Models
{
    public class VecinoCompromisoEstado
    {
        public int VecinoCompromisoEstadoId { get; set; }
        public string Descripcion { get; set; } = null!;
        public bool Active { get; set; }
        public bool State { get; set; }
    }

    public class VecinoEntregableTipo
    {
        public int VecinoEntregableTipoId { get; set; }
        public string Descripcion { get; set; } = null!;
        public int Orden { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }

    public class VecinoEntregableEstado
    {
        public int VecinoEntregableEstadoId { get; set; }
        public string Descripcion { get; set; } = null!;
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}

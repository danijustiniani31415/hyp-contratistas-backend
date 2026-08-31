namespace Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Infrastructure.Models
{
    /// <summary>Catálogo del uso del predio (Vivienda, Comercio, Oficinas, etc.).</summary>
    public class VecinoUso
    {
        public int VecinoUsoId { get; set; }
        public string Descripcion { get; set; } = null!;
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}

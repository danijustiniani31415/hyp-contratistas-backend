namespace Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Infrastructure.Models
{
    /// <summary>Una imagen del "estado de la propiedad" asociada a una casa/lote (Vecino).</summary>
    public class VecinoImagen
    {
        public int VecinoImagenId { get; set; }

        public int VecinoId { get; set; }
        public Vecino? Vecino { get; set; }

        public string ArchivoUrl { get; set; } = null!;
        public string? OriginalFileName { get; set; }

        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}

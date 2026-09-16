using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.PersonasModule.Infrastructure.Models
{
    [Table("lb_proyecto")]
    public class Proyecto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string? Ubicacion { get; set; }
        public string Estado { get; set; } = "ACTIVO";
        public DateTimeOffset CreadoEn { get; set; }
        /// <summary>Dirección + ubigeo (catálogo SUNAT) — punto de llegada de la GRE cuando el destino es la obra.</summary>
        public string? Direccion { get; set; }
        public string? Ubigeo { get; set; }

        public ICollection<Almacen> Almacenes { get; set; } = new List<Almacen>();
    }
}

using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.ComprasModule.Infrastructure.Models
{
    [Table("lb_proveedor")]
    public class Proveedor
    {
        public int Id { get; set; }
        public string RazonSocial { get; set; } = null!;
        public string? Ruc { get; set; }
        public string? Contacto { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public bool Activo { get; set; } = true;
    }
}

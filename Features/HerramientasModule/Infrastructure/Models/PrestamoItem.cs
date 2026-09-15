using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.CatalogoModule.Infrastructure.Models;
using Abril_Backend.Features.PersonasModule.Infrastructure.Models;

namespace Abril_Backend.Features.HerramientasModule.Infrastructure.Models
{
    [Table("lb_prestamo_item")]
    public class PrestamoItem
    {
        public long Id { get; set; }
        public long PrestamoId { get; set; }
        public long ProductoId { get; set; }
        public string Talla { get; set; } = "";
        public decimal Cantidad { get; set; }
        /// <summary>PRESTADO, DEVUELTO, PERDIDO, DANADO.</summary>
        public string Estado { get; set; } = "PRESTADO";
        public long? DevueltoPorUsuarioSistemaId { get; set; }
        public DateTimeOffset? FechaDevolucion { get; set; }
        public string? ObservacionDevolucion { get; set; }

        [ForeignKey(nameof(PrestamoId))]
        public Prestamo? Prestamo { get; set; }
        [ForeignKey(nameof(ProductoId))]
        public Producto? Producto { get; set; }
        [ForeignKey(nameof(DevueltoPorUsuarioSistemaId))]
        public UsuarioSistema? DevueltoPor { get; set; }
    }
}

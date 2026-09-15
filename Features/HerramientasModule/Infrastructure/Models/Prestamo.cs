using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.PersonasModule.Infrastructure.Models;

namespace Abril_Backend.Features.HerramientasModule.Infrastructure.Models
{
    /// <summary>
    /// Fase 2, punto 6 — préstamo de herramientas/equipos. A diferencia de Pedidos/EPP, el
    /// producto vuelve: el estado vive por PrestamoItem (PRESTADO/DEVUELTO/PERDIDO/DANADO), no
    /// en este header, para que cada ítem se pueda devolver en su propia fecha.
    /// </summary>
    [Table("lb_prestamo")]
    public class Prestamo
    {
        public long Id { get; set; }
        public string Codigo { get; set; } = null!;
        public int AlmacenId { get; set; }
        public int PersonaId { get; set; }
        public int? ProyectoId { get; set; }
        public long PrestadoPorUsuarioSistemaId { get; set; }
        public DateOnly? FechaDevolucionEstimada { get; set; }
        public string? Observacion { get; set; }
        public DateTimeOffset CreadoEn { get; set; }

        [ForeignKey(nameof(AlmacenId))]
        public Almacen? Almacen { get; set; }
        [ForeignKey(nameof(PersonaId))]
        public Persona? Persona { get; set; }
        [ForeignKey(nameof(ProyectoId))]
        public Proyecto? Proyecto { get; set; }
        [ForeignKey(nameof(PrestadoPorUsuarioSistemaId))]
        public UsuarioSistema? PrestadoPor { get; set; }

        public ICollection<PrestamoItem> Items { get; set; } = new List<PrestamoItem>();
    }
}

using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("ss_tareo")]
    public class SsTareo
    {
        public int Id { get; set; }
        public int ProyectoId { get; set; }
        public DateOnly Fecha { get; set; }
        public string? Observaciones { get; set; }
        public int? CreadoPor { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(ProyectoId))]
        public Project? Proyecto { get; set; }
    }
}

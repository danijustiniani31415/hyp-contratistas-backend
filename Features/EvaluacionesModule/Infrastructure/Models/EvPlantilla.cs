using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Models
{
    [Table("ev_plantilla")]
    public class EvPlantilla
    {
        public int Id { get; set; }
        public string AreaNombre { get; set; } = string.Empty;
        public string Criterio { get; set; } = string.Empty;
        public int Orden { get; set; } = 0;
        public bool Activo { get; set; } = true;
        public int Version { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }
}

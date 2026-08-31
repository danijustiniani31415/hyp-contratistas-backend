using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Models
{
    [Table("ev_jefe_ssoma_plantilla")]
    public class EvJefeSsomaPlantilla
    {
        public int Id { get; set; }
        public string Criterio { get; set; } = string.Empty;
        public int Orden { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

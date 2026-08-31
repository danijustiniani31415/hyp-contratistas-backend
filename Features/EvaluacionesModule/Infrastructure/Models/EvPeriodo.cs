using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Models
{
    [Table("ev_periodo")]
    public class EvPeriodo
    {
        public int Id { get; set; }
        public int Mes { get; set; }
        public int Anio { get; set; }
        public DateOnly FechaApertura { get; set; }
        public DateOnly FechaCierre { get; set; }
        public bool Activo { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

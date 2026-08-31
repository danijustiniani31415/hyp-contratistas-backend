using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Models
{
    [Table("ev_contratista_plantilla")]
    public class EvContratistaPlantilla
    {
        public int Id { get; set; }
        public string AreaNombre { get; set; } = string.Empty;
        public string PuestoEvaluador { get; set; } = string.Empty;
        public string Criterio { get; set; } = string.Empty;
        public int Orden { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

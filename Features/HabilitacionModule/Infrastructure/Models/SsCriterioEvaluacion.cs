using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models
{
    [Table("ss_criterio_evaluacion")]
    public class SsCriterioEvaluacion
    {
        public int Id { get; set; }
        public string Criterio { get; set; } = string.Empty;
        public int Orden { get; set; }
        public bool Activo { get; set; } = true;
    }
}

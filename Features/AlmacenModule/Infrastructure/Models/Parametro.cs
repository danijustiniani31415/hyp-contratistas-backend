using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.AlmacenModule.Infrastructure.Models
{
    /// <summary>Valores de configuración editables desde el sistema (por ejemplo, el monto desde el que un ajuste de inventario exige aprobación).</summary>
    [Table("lb_parametro")]
    public class Parametro
    {
        [System.ComponentModel.DataAnnotations.Key]
        public string Clave { get; set; } = null!;
        public string Valor { get; set; } = null!;
        public long? ActualizadoPor { get; set; }
        public DateTimeOffset ActualizadoEn { get; set; }
    }
}

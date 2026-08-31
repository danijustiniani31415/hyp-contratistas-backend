using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.SsomaModule.InduccionProgramacionFeature.Infrastructure.Models
{
    /// <summary>
    /// Fila única (Id = 1) que recuerda hasta dónde avanzó la generación automática de la
    /// rotación: el último proyecto asignado y la última fecha ya generada. Así, agregar un
    /// proyecto nuevo a la cola no reinicia el orden de los demás — la próxima fecha a generar
    /// simplemente continúa desde acá.
    /// </summary>
    [Table("ss_induccion_rotacion_cursor")]
    public class SsInduccionRotacionCursor
    {
        public int Id { get; set; }
        public int? UltimoProyectoRotacionId { get; set; }
        public DateOnly? UltimaFechaGenerada { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

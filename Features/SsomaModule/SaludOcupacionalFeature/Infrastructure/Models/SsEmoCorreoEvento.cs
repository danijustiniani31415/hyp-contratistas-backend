using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    /// <summary>
    /// Catálogo de los correos de EMO cuyos destinatarios se configuran en
    /// /ssoma/salud-ocupacional/emos/configuracion (una sección por fila):
    /// programación automática, programación manual, aceptada y rechazada.
    ///
    /// Es el eje "correo" de la matriz <see cref="SsEmoCorreoRegla"/>.
    /// </summary>
    [Table("ss_emo_correo_evento")]
    public class SsEmoCorreoEvento
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Clave estable: PROGRAMACION_AUTOMATICA, PROGRAMACION_MANUAL, ACEPTADA, RECHAZADA.</summary>
        [Column("codigo")]
        public string Codigo { get; set; } = string.Empty;

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("orden")]
        public int Orden { get; set; }

        [Column("active")]
        public bool Active { get; set; } = true;

        [Column("state")]
        public bool State { get; set; } = true;

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [Column("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}

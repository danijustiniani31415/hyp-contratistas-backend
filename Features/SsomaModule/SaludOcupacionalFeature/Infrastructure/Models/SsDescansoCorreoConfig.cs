using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    /// <summary>
    /// Configuración de destinatarios del correo que se envía al registrar un
    /// descanso médico desde Mi Salud. Cada fila representa un destinatario
    /// (trabajador solicitante, asistenta social, GTH, médico ocupacional) y su
    /// flag <see cref="Active"/> prende/apaga el envío a ese destinatario.
    /// Sirve para pruebas tanto en desarrollo como en producción.
    /// </summary>
    [Table("ss_descanso_correo_config")]
    public class SsDescansoCorreoConfig
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Clave estable del destinatario (TRABAJADOR, ASISTENTA_SOCIAL, GTH, MEDICO_OCUPACIONAL).</summary>
        [Column("codigo")]
        public string Codigo { get; set; } = string.Empty;

        /// <summary>Nombre para mostrar en la pantalla de configuración.</summary>
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>Descripción de a dónde llega el correo (informativa para la UI).</summary>
        [Column("descripcion")]
        public string? Descripcion { get; set; }

        /// <summary>Orden de visualización.</summary>
        [Column("orden")]
        public int Orden { get; set; }

        /// <summary>true = se envía el correo a este destinatario; false = no se envía.</summary>
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

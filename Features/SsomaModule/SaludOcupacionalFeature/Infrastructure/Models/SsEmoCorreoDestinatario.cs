using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    /// <summary>
    /// El "quién" de los correos de EMO: es el eje "fila" de la matriz
    /// <see cref="SsEmoCorreoRegla"/>. El <see cref="TipoId"/> decide si va en
    /// "Para" (PRINCIPAL) o en "CC" (COPIA).
    ///
    /// Hay tres clases de fila, distinguidas por <see cref="Codigo"/> y <see cref="Email"/>:
    ///  • <b>Dinámica</b> (código + sin correo): el correo NO vive acá, se resuelve al
    ///    enviar según el trabajador — <c>CLINICA</c>, <c>JEFE</c>, <c>TRABAJADOR</c>,
    ///    <c>RESIDENTE</c>, <c>COORD_ADMIN</c>, <c>COORD_SSOMA</c>,
    ///    <c>ADMIN_RAZON_SOCIAL</c>, <c>GTH</c>. No se editan ni se eliminan.
    ///  • <b>Buzón de área</b> (código + correo): el correo vive acá y se edita desde la
    ///    pantalla, pero la fila no se puede eliminar — <c>MEDICINA_OCUPACIONAL</c>,
    ///    <c>ARQCOM_*</c>, <c>POSTVENTA_*</c>.
    ///  • <b>Correo adicional</b> (sin código): agregado a mano, con alta/edición/baja
    ///    completa desde la pantalla.
    ///
    /// <see cref="Active"/> quedó SIN USO cuando la configuración pasó de ser una lista
    /// plana a la matriz: el interruptor real es <see cref="SsEmoCorreoRegla.Active"/>,
    /// una celda por correo y perfil de trabajador.
    /// </summary>
    [Table("ss_emo_correo_destinatario")]
    public class SsEmoCorreoDestinatario
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("tipo_id")]
        public int TipoId { get; set; }

        /// <summary>Clave estable de los destinatarios fijos/dinámicos (hoy solo CLINICA). Null en los editables.</summary>
        [Column("codigo")]
        public string? Codigo { get; set; }

        /// <summary>Correo destino. Null solo en los destinatarios fijos/dinámicos.</summary>
        [Column("email")]
        public string? Email { get; set; }

        /// <summary>Nombre/etiqueta del destinatario, para mostrarlo en la pantalla.</summary>
        [Column("nombre")]
        public string? Nombre { get; set; }

        /// <summary>Texto informativo bajo el nombre (usado por los destinatarios fijos).</summary>
        [Column("descripcion")]
        public string? Descripcion { get; set; }

        /// <summary>false = fila fija: solo se puede prender/apagar, no editar ni eliminar.</summary>
        [Column("editable")]
        public bool Editable { get; set; } = true;

        [Column("orden")]
        public int Orden { get; set; }

        /// <summary>true = se le envía el correo; false = no se le envía.</summary>
        [Column("active")]
        public bool Active { get; set; } = true;

        [Column("state")]
        public bool State { get; set; } = true;

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [Column("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        [ForeignKey(nameof(TipoId))]
        public SsEmoCorreoTipo? Tipo { get; set; }
    }
}

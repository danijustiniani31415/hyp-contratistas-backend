using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    /// <summary>
    /// Catálogo del tipo de destinatario del correo de programación de EMO:
    /// PRINCIPAL (va en "Para") y COPIA (va en "CC"). No se usan copias ocultas.
    /// Existe como tabla para no guardar el tipo como texto plano en
    /// <see cref="SsEmoCorreoDestinatario"/>.
    /// </summary>
    [Table("ss_emo_correo_tipo")]
    public class SsEmoCorreoTipo
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Clave estable del tipo (PRINCIPAL, COPIA).</summary>
        [Column("codigo")]
        public string Codigo { get; set; } = string.Empty;

        /// <summary>Nombre para mostrar en la pantalla de configuración.</summary>
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>Orden de visualización de las secciones.</summary>
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

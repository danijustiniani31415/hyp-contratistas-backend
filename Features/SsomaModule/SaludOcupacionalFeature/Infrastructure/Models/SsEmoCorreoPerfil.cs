using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    /// <summary>
    /// Perfil del trabajador para el que se configura cada correo de EMO: es el
    /// eje "columna" de la matriz <see cref="SsEmoCorreoRegla"/>, porque a un
    /// trabajador de Oficina Central no le escribe la misma gente que a uno de obra.
    ///
    /// Se deriva de <c>workers.contrata_casa</c> + <c>workers.obra_oficina_staff_id</c>
    /// (ver <c>EmoCorreoPerfilCodigo.Resolver</c>). Solo cubre al personal de casa: por
    /// negocio Abril controla únicamente el EMO de sus propios trabajadores, así que un
    /// trabajador de contratista no cae en ningún perfil y no recibe estos correos.
    ///
    /// Tiene los mismos 3 valores que <c>workers_obra_oficina_staff</c> pero no lo
    /// reutiliza: este es un catálogo de configuración de correos y aquel es de datos
    /// maestros de RR.HH.; atarlos obligaría a que agregar una modalidad de trabajador
    /// cambiara en silencio la matriz de destinatarios.
    /// </summary>
    [Table("ss_emo_correo_perfil")]
    public class SsEmoCorreoPerfil
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Clave estable: OFICINA_CENTRAL, STAFF, OBRA, CONTRATISTA.</summary>
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

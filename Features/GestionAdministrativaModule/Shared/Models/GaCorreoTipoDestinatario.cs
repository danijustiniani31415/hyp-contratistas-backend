using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.GestionAdministrativa.Shared.Models
{
    /// <summary>
    /// Catálogo de tipos de destinatario para las reglas de correos (<see cref="GaCorreoRegla"/>):
    /// TRABAJADOR (un worker concreto), AREA (todos los miembros de un nodo area_scope) y
    /// CORREO (una dirección escrita a mano, ej. un grupo de correos como gthnm@abril.pe).
    /// </summary>
    [Table("ga_correo_tipo_destinatario")]
    public class GaCorreoTipoDestinatario
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Clave estable del tipo (TRABAJADOR, AREA, CORREO).</summary>
        [Column("codigo")]
        public string Codigo { get; set; } = string.Empty;

        /// <summary>Nombre para mostrar.</summary>
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>Orden de visualización.</summary>
        [Column("orden")]
        public int Orden { get; set; }

        [Column("active")]
        public bool Active { get; set; } = true;

        /// <summary>Soft delete: false = eliminado (se conserva para auditoría).</summary>
        [Column("state")]
        public bool State { get; set; } = true;

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [Column("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
